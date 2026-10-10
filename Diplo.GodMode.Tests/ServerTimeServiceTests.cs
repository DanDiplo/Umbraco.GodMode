using System.Reflection;
using System.Text.Json;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class ServerTimeServiceTests
{
    [TestMethod]
    public void EqualWinterOffsets_DoNotEstablishMatchingHistoricalRules()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "Africa/Abidjan", "Europe/London");
        var migration = service.GetServerTime().SystemDateMigration;
        Assert.IsTrue(migration.ConfiguredTimeZoneValid);
        Assert.IsFalse(migration.EffectiveTimeZoneMatchesServer);
        Assert.IsFalse(migration.EffectiveTimeZoneSupportsDaylightSaving);
    }

    [TestMethod]
    public void SqliteDstWarning_UsesMigrationZoneOnUtcServer()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var time = CreateService(cache, "Europe/London", "UTC").GetServerTime();
        Assert.IsFalse(time.SupportsDaylightSavingTime);
        var finding = GodModeHealthRiskService.BuildDateTimeFindings(time, CompleteEvidence())
            .Single(x => x.CheckId == "time-migration-sqlite-dst");
        Assert.AreEqual("Info", finding.Severity);
    }

    [TestMethod]
    public void SqlServerValidity_ComesFromDatabaseRatherThanLocalResolution()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(cache, "Europe/London", "UTC", database: new SqlServerDatabaseType(),
            sqlValidation: name => { Assert.AreEqual("Europe/London", name); return false; });
        var migration = service.GetServerTime().SystemDateMigration;
        Assert.IsTrue(migration.EffectiveTimeZoneResolved);
        Assert.IsFalse(migration.ConfiguredTimeZoneValid);
    }

    [TestMethod]
    public void SqlServerZoneUnavailableLocally_CanStillBeValidForDatabase()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var migration = CreateService(cache, "sql-only-zone", "UTC", database: new SqlServerDatabaseType(),
            sqlValidation: _ => true).GetServerTime().SystemDateMigration;
        Assert.IsTrue(migration.ConfiguredTimeZoneValid);
        Assert.IsFalse(migration.EffectiveTimeZoneResolved);
        Assert.IsNull(migration.EffectiveTimeZoneSupportsDaylightSaving);
    }

    [TestMethod]
    public void FailedSqlServerValidation_RemainsUnknown()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var migration = CreateService(cache, "Europe/London", "UTC", database: new SqlServerDatabaseType(),
            sqlValidation: _ => throw new InvalidOperationException("unavailable")).GetServerTime().SystemDateMigration;
        Assert.IsNull(migration.ConfiguredTimeZoneValid);
        StringAssert.Contains(migration.TimeZoneValidationMessage, "unavailable");
    }

    [TestMethod]
    public void EvidenceCache_SharedAcrossScopedServices_ThenExpiresByEviction()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var calls = 0;
        Func<IEnumerable<FutureDatedRows>> read = () => { calls++; return SuccessfulRows(); };
        var first = CreateService(cache, readRows: read).GetSystemDateEvidence();
        var second = CreateService(cache, readRows: read).GetSystemDateEvidence();
        Assert.AreSame(first, second);
        Assert.AreEqual(1, calls);
        Assert.IsTrue(first.DatabaseCheckSucceeded);
        cache.Remove("godmode:system-date-evidence:v2");
        CreateService(cache, readRows: read).GetSystemDateEvidence();
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void PartialDatabaseEvidence_IsNotReportedAsComplete()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var evidence = CreateService(cache, readRows: () =>
            SuccessfulRows().Select((row, i) => { row.CheckSucceeded = i != 2; return row; })).GetSystemDateEvidence();
        Assert.IsFalse(evidence.DatabaseCheckSucceeded);
        Assert.AreEqual(4, evidence.FutureDatedRows.Count());
        Assert.IsTrue(GodModeHealthRiskService.BuildDateTimeFindings(new ServerTimeInfo(), evidence)
            .Any(x => x.CheckId == "time-evidence-incomplete"));
    }

    [TestMethod]
    public void FailedDatabaseEvidence_DoesNotBecomeSuccessfulEmptyEvidence()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var evidence = CreateService(cache, readRows: () => throw new InvalidOperationException("database unavailable")).GetSystemDateEvidence();
        Assert.IsFalse(evidence.DatabaseCheckSucceeded);
        StringAssert.Contains(evidence.DatabaseCheckMessage, "could not be checked");
    }

    [TestMethod]
    public void CurrentDisabledMigration_IsInformationalEvenAfterCompletedUpgrade()
    {
        var time = new ServerTimeInfo { SystemDateMigration = new() { Enabled = false, UpgradeComplete = true } };
        var finding = GodModeHealthRiskService.BuildDateTimeFindings(time, CompleteEvidence())
            .Single(x => x.CheckId == "time-migration-disabled");
        Assert.AreEqual("Info", finding.Severity);
        StringAssert.Contains(finding.Detail, "does not establish");
    }

    [TestMethod]
    public void FixedRole_HistoricalActiveRowsDoNotCreateServerDownFinding()
    {
        var overview = new ContentScheduleOverview
        {
            JobsCheckSucceeded = true, ServersCheckSucceeded = true,
            AutomaticServerRegistration = false,
            Servers = [new RegisteredServerInfo { IsActive = true, IsStale = true }]
        };
        Assert.IsFalse(GodModeHealthRiskService.BuildScheduledPublishingFindings(overview).Any(x => x.CheckId == "server-stale"));
        overview.AutomaticServerRegistration = true;
        Assert.IsTrue(GodModeHealthRiskService.BuildScheduledPublishingFindings(overview).Any(x => x.CheckId == "server-stale"));
    }

    [TestMethod]
    public void PublishingSuspension_IsReportedDespiteFreshJobTimestamp()
    {
        var overview = new ContentScheduleOverview
        {
            JobsCheckSucceeded = true, ServersCheckSucceeded = true,
            ScheduledPublishingSuspended = true,
            Items = [new ContentScheduleItem { IsOverdue = true }],
            Jobs = [new DistributedJobInfo { Name = "ScheduledPublishingJob", LastRun = DateTime.UtcNow }]
        };
        var finding = GodModeHealthRiskService.BuildScheduledPublishingFindings(overview).Single(x => x.CheckId == "schedule-suspended");
        Assert.AreEqual("High", finding.Severity);
    }

    [TestMethod]
    public void UtcConverter_NormalizesNullableUnspecifiedDates_AndPreservesOffsetValues()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UtcDateTimeJsonConverter());
        var json = JsonSerializer.Serialize(new
        {
            Date = (DateTime?)new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Unspecified),
            Local = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.FromHours(2))
        }, options);
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual("2026-01-10T12:00:00Z", document.RootElement.GetProperty("Date").GetString());
        StringAssert.EndsWith(document.RootElement.GetProperty("Local").GetString()!, "+02:00");
    }

    private static SystemDateEvidence CompleteEvidence() => new() { LogCheckSucceeded = true, DatabaseCheckSucceeded = true };

    private static IEnumerable<FutureDatedRows> SuccessfulRows() => Enumerable.Range(0, 4)
        .Select(i => new FutureDatedRows { Table = "table" + i, CheckSucceeded = true });

    private static ServerTimeService CreateService(IMemoryCache cache, string? configured = null, string zone = "UTC",
        DatabaseType? database = null, Func<string, bool>? sqlValidation = null,
        Func<IEnumerable<FutureDatedRows>>? readRows = null)
    {
        var db = Stub<IUmbracoDatabaseService>(method => method.Name switch
        {
            "GetDatabaseType" => database ?? DatabaseType.SQLite,
            "IsSqlServerTimeZoneValid" => (sqlValidation ?? (_ => false))((string)method.Arguments![0]!),
            "GetFutureDatedRows" => (readRows ?? SuccessfulRows)(),
            _ => throw new NotSupportedException(method.Name)
        });
        return new ServerTimeService(new FixedTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(zone)),
            Options.Create(new SystemDateMigrationSettings { LocalServerTimeZone = configured }),
            Stub<IRuntimeState>(method => method.Name switch
            {
                "get_Level" => RuntimeLevel.Run,
                "get_CurrentMigrationState" or "get_FinalMigrationState" => "complete",
                _ => throw new NotSupportedException(method.Name)
            }), Stub<IServerRoleAccessor>(_ => throw new NotSupportedException()), db,
            Stub<IGodModeLogService>(_ => new SystemDateLogEvidence { CheckSucceeded = true }),
            NullLogger<ServerTimeService>.Instance, cache);
    }

    // Provider-name stub: these tests exercise validation policy without loading a SQL driver.
    private sealed class SqlServerDatabaseType : DatabaseType;

    private sealed class FixedTimeProvider(TimeZoneInfo zone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => zone;
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
    }

    private static T Stub<T>(Func<(string Name, object?[]? Arguments), object?> call) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Call = call;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<(string Name, object?[]? Arguments), object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call((targetMethod!.Name, args));
    }
}
