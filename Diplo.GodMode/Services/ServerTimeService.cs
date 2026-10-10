using System.Globalization;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Reports the server clock and time zone, and checks how Umbraco 17's switch to UTC system dates has landed
    /// </summary>
    public class ServerTimeService : IServerTimeService
    {
        /// <summary>
        /// Scheduled publishing runs every minute, so anything this late has been missed.
        /// </summary>
        private const int OverdueAfterMinutes = 5;

        /// <summary>
        /// Rows dated further ahead than this are treated as suspicious rather than clock skew.
        /// </summary>
        private static readonly TimeSpan FutureDateTolerance = TimeSpan.FromMinutes(10);

        private static readonly DateTime FallbackStartedUtc = DateTime.UtcNow;

        private static readonly string[] MigrationUtcIdentifiers = ["Coordinated Universal Time", "UTC"];
        private static readonly object EvidenceLock = new();

        private readonly TimeProvider timeProvider;
        private readonly IOptions<SystemDateMigrationSettings> migrationSettings;
        private readonly IRuntimeState runtimeState;
        private readonly IServerRoleAccessor serverRoleAccessor;
        private readonly IUmbracoDatabaseService databaseService;
        private readonly IGodModeLogService logService;
        private readonly ILogger<ServerTimeService> logger;
        private readonly IMemoryCache memoryCache;

        public ServerTimeService(
            TimeProvider timeProvider,
            IOptions<SystemDateMigrationSettings> migrationSettings,
            IRuntimeState runtimeState,
            IServerRoleAccessor serverRoleAccessor,
            IUmbracoDatabaseService databaseService,
            IGodModeLogService logService,
            ILogger<ServerTimeService> logger,
            IMemoryCache memoryCache)
        {
            this.timeProvider = timeProvider;
            this.migrationSettings = migrationSettings;
            this.runtimeState = runtimeState;
            this.serverRoleAccessor = serverRoleAccessor;
            this.databaseService = databaseService;
            this.logService = logService;
            this.logger = logger;
            this.memoryCache = memoryCache;
        }

        public bool AutomaticServerRegistration => serverRoleAccessor is ElectedServerRoleAccessor;

        public ServerTimeInfo GetServerTime()
        {
            var utcNow = timeProvider.GetUtcNow();
            var zone = timeProvider.LocalTimeZone;
            var offset = zone.GetUtcOffset(utcNow);
            var (ianaId, windowsId) = GetZoneIds(zone);
            var nextTransition = GetNextTransition(zone, utcNow);
            var culture = CultureInfo.CurrentCulture;

            return new ServerTimeInfo
            {
                ServerUtcNow = utcNow.UtcDateTime,
                ServerLocalNow = TimeZoneInfo.ConvertTime(utcNow, zone),
                TimeZoneId = zone.Id,
                IanaId = ianaId,
                WindowsId = windowsId,
                DisplayName = zone.DisplayName,
                StandardName = zone.StandardName,
                DaylightName = zone.DaylightName,
                UtcOffset = FormatOffset(offset),
                UtcOffsetMinutes = (int)offset.TotalMinutes,
                BaseUtcOffsetMinutes = (int)zone.BaseUtcOffset.TotalMinutes,
                IsUtc = zone.BaseUtcOffset == TimeSpan.Zero && !zone.SupportsDaylightSavingTime,
                SupportsDaylightSavingTime = zone.SupportsDaylightSavingTime,
                IsDaylightSavingTime = zone.IsDaylightSavingTime(utcNow),
                NextTransition = nextTransition.HasValue ? TimeZoneInfo.ConvertTime(nextTransition.Value, zone) : null,
                NextTransitionUtcOffset = nextTransition.HasValue ? FormatOffset(zone.GetUtcOffset(nextTransition.Value)) : string.Empty,
                TzEnvironmentVariable = Environment.GetEnvironmentVariable("TZ") ?? string.Empty,
                TimeProviderType = timeProvider.GetType().FullName ?? timeProvider.GetType().Name,
                CultureName = culture.Name,
                ShortDatePattern = culture.DateTimeFormat.ShortDatePattern,
                LongTimePattern = culture.DateTimeFormat.LongTimePattern,
                ProcessStartedUtc = ProcessTimeHelper.GetStartedUtc(FallbackStartedUtc),
                SystemDateMigration = GetMigrationInfo(zone, utcNow)
            };
        }

        public SystemDateEvidence GetSystemDateEvidence()
        {
            lock (EvidenceLock)
            {
                return memoryCache.GetOrCreate("godmode:system-date-evidence:v2", entry =>
                {
                    // Independent of changing file signatures; concurrent requests share the same scan.
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                    return ReadSystemDateEvidence();
                })!;
            }
        }

        private SystemDateEvidence ReadSystemDateEvidence()
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var evidence = new SystemDateEvidence
            {
                CheckedAtUtc = utcNow,
                FutureDateCutoffUtc = utcNow.Add(FutureDateTolerance)
            };

            try
            {
                var logs = logService.GetSystemDateMigrationLogs();
                evidence.MigrationLogEntries = logs.Entries;
                evidence.LogCheckSucceeded = logs.CheckSucceeded;
                evidence.LogCheckMessage = logs.CheckMessage;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not search the logs for the UTC system date migration.");
                evidence.LogCheckMessage = "The local migration logs could not be checked.";
            }

            try
            {
                evidence.FutureDatedRows = databaseService
                    .GetFutureDatedRows(evidence.FutureDateCutoffUtc)
                    .ToList();
                evidence.DatabaseCheckSucceeded = evidence.FutureDatedRows.Count() == 4
                    && evidence.FutureDatedRows.All(x => x.CheckSucceeded);
                evidence.DatabaseCheckMessage = evidence.DatabaseCheckSucceeded
                    ? "Checked four system date columns. No matches does not establish that historical dates are correct."
                    : "One or more system date columns could not be checked; results are partial.";
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not check for future dated rows.");
                evidence.DatabaseCheckMessage = "System date columns could not be checked.";
            }

            return evidence;
        }

        public ContentScheduleOverview GetContentSchedules()
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var overdueCutoff = utcNow.AddMinutes(-OverdueAfterMinutes);

            var items = databaseService.GetContentSchedules().ToList();
            foreach (var item in items)
            {
                item.IsOverdue = DateTime.SpecifyKind(item.Date, DateTimeKind.Utc) < overdueCutoff;
            }

            var jobsCheckSucceeded = true;
            List<DistributedJobInfo> jobs;
            try
            {
                jobs = databaseService.GetDistributedJobs().ToList();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read distributed jobs.");
                jobs = [];
                jobsCheckSucceeded = false;
            }
            foreach (var job in jobs)
            {
                // Allow a few missed periods (and at least five minutes) before calling a job stale.
                var allowance = TimeSpan.FromSeconds(Math.Max(job.PeriodSeconds * 3, 300));
                job.IsStale = DateTime.SpecifyKind(job.LastRun, DateTimeKind.Utc) < utcNow - allowance;
            }

            var servers = GetRegisteredServers(utcNow, out var serversCheckSucceeded);
            return new ContentScheduleOverview
            {
                ServerUtcNow = utcNow,
                OverdueAfterMinutes = OverdueAfterMinutes,
                Items = items,
                Jobs = jobs,
                Servers = servers,
                CurrentServerRole = GetCurrentServerRole(),
                AutomaticServerRegistration = AutomaticServerRegistration,
                ScheduledPublishingSuspended = !Suspendable.ScheduledPublishing.CanRun,
                JobsCheckSucceeded = jobsCheckSucceeded,
                ServersCheckSucceeded = serversCheckSucceeded
            };
        }

        private IEnumerable<RegisteredServerInfo> GetRegisteredServers(DateTime utcNow, out bool checkSucceeded)
        {
            checkSucceeded = true;
            try
            {
                return databaseService.GetRegistredServers()
                    .Select(x => new RegisteredServerInfo
                    {
                        Id = x.Id,
                        Address = x.Address,
                        ComputerName = x.ComputerName,
                        RegisteredDate = x.RegisteredDate,
                        LastNotifiedDate = x.LastNotifiedDate,
                        IsActive = x.IsActive,
                        IsSchedulingPublisher = x.IsSchedulingPublisher,
                        IsStale = ServerModel.IsStaleAt(x.LastNotifiedDate, utcNow)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read registered servers.");
                checkSucceeded = false;
                return [];
            }
        }

        private string GetCurrentServerRole()
        {
            try
            {
                return serverRoleAccessor.CurrentServerRole.ToString();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not determine the current server role.");
                return "Unknown";
            }
        }

        private SystemDateMigrationInfo GetMigrationInfo(TimeZoneInfo serverZone, DateTimeOffset utcNow)
        {
            var settings = migrationSettings.Value;
            // Umbraco passes an explicit value through unchanged; do not silently validate a trimmed ID.
            var configured = string.IsNullOrWhiteSpace(settings.LocalServerTimeZone) ? string.Empty : settings.LocalServerTimeZone;
            TimeZoneInfo? configuredZone = null;

            if (configured.Length > 0)
            {
                try
                {
                    configuredZone = TimeZoneInfo.FindSystemTimeZoneById(configured);
                }
                catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
                {
                    configuredZone = null;
                }
            }

            var effectiveZone = configured.Length > 0 ? configuredZone : serverZone;
            var effectiveName = configured.Length > 0 ? configured : serverZone.Id;
            var databaseType = GetDatabaseType();
            var isSqlite = databaseType.StartsWith("SQLite", StringComparison.OrdinalIgnoreCase);

            bool? valid = null;
            var validationMessage = "No explicit time zone configured; uses the detected server zone.";
            if (configured.Length > 0)
            {
                if (isSqlite)
                {
                    valid = configuredZone is not null;
                    validationMessage = valid.Value ? "Resolved locally for SQLite's base-offset conversion." : "This ID does not resolve locally for SQLite.";
                }
                else if (databaseType.StartsWith("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        valid = databaseService.IsSqlServerTimeZoneValid(configured);
                        validationMessage = valid.Value
                            ? "Recognised by this SQL Server in sys.time_zone_info."
                            : "Not recognised by this SQL Server. Explicit IDs must be SQL Server time zone names (for example GMT Standard Time), not IANA IDs.";
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Could not validate the migration time zone on SQL Server.");
                        validationMessage = "SQL Server time zone validation is unavailable; local resolution alone does not establish validity.";
                    }
                }
                else
                {
                    validationMessage = "Time zone validation is unavailable for this database provider.";
                }
            }

            return new SystemDateMigrationInfo
            {
                Enabled = settings.Enabled,
                ConfiguredTimeZone = configured,
                ConfiguredTimeZoneValid = valid,
                TimeZoneValidationMessage = validationMessage,
                EffectiveTimeZoneResolved = effectiveZone is not null,
                EffectiveTimeZoneSupportsDaylightSaving = effectiveZone?.SupportsDaylightSavingTime,
                EffectiveTimeZone = effectiveName,
                EffectiveTimeZoneMatchesServer = effectiveZone is not null
                    && effectiveZone.HasSameRules(serverZone),
                EffectiveTimeZoneIsUtc = MigrationUtcIdentifiers.Contains(effectiveName, StringComparer.OrdinalIgnoreCase)
                    || (effectiveZone is not null && effectiveZone.BaseUtcOffset == TimeSpan.Zero && !effectiveZone.SupportsDaylightSavingTime),
                DatabaseType = databaseType,
                UsesBaseOffsetOnly = isSqlite,
                UpgradeComplete = runtimeState.Level == Umbraco.Cms.Core.RuntimeLevel.Run
                    && string.Equals(runtimeState.CurrentMigrationState, runtimeState.FinalMigrationState, StringComparison.OrdinalIgnoreCase),
                CurrentMigrationState = runtimeState.CurrentMigrationState ?? string.Empty,
                FinalMigrationState = runtimeState.FinalMigrationState ?? string.Empty
            };
        }

        private string GetDatabaseType()
        {
            try
            {
                // NPoco types are named e.g. SQLiteDatabaseType or SqlServer2012DatabaseType.
                var name = databaseService.GetDatabaseType().GetType().Name;
                return name.EndsWith("DatabaseType", StringComparison.Ordinal) ? name[..^"DatabaseType".Length] : name;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not determine the database type.");
                return string.Empty;
            }
        }

        private static (string IanaId, string WindowsId) GetZoneIds(TimeZoneInfo zone)
        {
            if (zone.HasIanaId)
            {
                return (zone.Id, TimeZoneInfo.TryConvertIanaIdToWindowsId(zone.Id, out var windowsId) ? windowsId : string.Empty);
            }

            return (TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : string.Empty, zone.Id);
        }

        /// <summary>
        /// Finds the next point within roughly a year at which the zone's UTC offset changes (e.g. a daylight saving switch).
        /// </summary>
        private static DateTimeOffset? GetNextTransition(TimeZoneInfo zone, DateTimeOffset utcNow)
        {
            if (!zone.SupportsDaylightSavingTime)
            {
                return null;
            }

            var current = zone.GetUtcOffset(utcNow);
            var start = new DateTimeOffset(utcNow.UtcDateTime.Date, TimeSpan.Zero);

            for (var day = 1; day <= 400; day++)
            {
                var candidate = start.AddDays(day);
                if (zone.GetUtcOffset(candidate) == current)
                {
                    continue;
                }

                // The change happened in the preceding 24 hours: narrow it down to the minute.
                var low = candidate.AddDays(-1);
                var high = candidate;
                while (high - low > TimeSpan.FromMinutes(1))
                {
                    var mid = low + TimeSpan.FromTicks((high - low).Ticks / 2);
                    if (zone.GetUtcOffset(mid) == current)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid;
                    }
                }

                var transition = new DateTimeOffset(high.UtcTicks - (high.UtcTicks % TimeSpan.TicksPerMinute), TimeSpan.Zero);
                return transition > utcNow ? transition : null;
            }

            return null;
        }

        internal static string FormatOffset(TimeSpan offset)
            => $"UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration():hh\\:mm}";
    }
}
