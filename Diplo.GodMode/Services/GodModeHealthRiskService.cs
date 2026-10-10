using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    public class GodModeHealthRiskService : IGodModeHealthRiskService
    {
        private readonly IUmbracoDataService dataService;
        private readonly IUmbracoDatabaseService dataBaseService;
        private readonly IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService;
        private readonly IGodModeLogService logService;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IOptions<GlobalSettings> globalSettings;
        private readonly IOptions<ModelsBuilderSettings> modelsBuilderSettings;
        private readonly IOptions<GodModeConfig> godModeConfig;
        private readonly IServerTimeService serverTimeService;

        public GodModeHealthRiskService(
            IUmbracoDataService dataService,
            IUmbracoDatabaseService dataBaseService,
            IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService,
            IGodModeLogService logService,
            IHostEnvironment hostEnvironment,
            IOptions<GlobalSettings> globalSettings,
            IOptions<ModelsBuilderSettings> modelsBuilderSettings,
            IOptions<GodModeConfig> godModeConfig,
            IServerTimeService serverTimeService)
        {
            this.dataService = dataService;
            this.dataBaseService = dataBaseService;
            this.deliveryApiDiagnosticsService = deliveryApiDiagnosticsService;
            this.logService = logService;
            this.hostEnvironment = hostEnvironment;
            this.globalSettings = globalSettings;
            this.modelsBuilderSettings = modelsBuilderSettings;
            this.godModeConfig = godModeConfig;
            this.serverTimeService = serverTimeService;
        }

        public async Task<IEnumerable<HealthRiskFinding>> BuildHealthRiskFindingsAsync(CancellationToken cancellationToken = default)
        {
            const int logRowWarningThreshold = 100000;
            const int contentVersionWarningThreshold = 50;
            const int propertyCountWarningThreshold = 50;
            const int recurringErrorWarningThreshold = 25;

            cancellationToken.ThrowIfCancellationRequested();

            var findings = new List<HealthRiskFinding>();
            var contentTypes = dataService.GetContentTypeMap().ToList();
            var dataTypes = (await dataService.GetDataTypesStatus()).ToList();
            var templates = (await dataService.GetTemplates()).ToList();
            var referenceEdges = (await dataService.GetSchemaReferenceGraph()).ToList();
            var deliveryApiDiagnostics = await deliveryApiDiagnosticsService.GetDiagnosticsAsync(cancellationToken);
            var usage = dataBaseService.GetContentUsageData().ToList();
            var orphanedTags = dataBaseService.GetOrphanedTags();
            var orphanedMediaCount = dataBaseService.GetOrphanedMediaCount();
            var languagesWithoutAssignedDomains = dataBaseService.GetLanguagesWithoutAssignedDomains().ToList();
            var logRowCount = dataBaseService.GetLogRowCount();
            var contentVersionCount = dataBaseService.GetContentVersionCount();
            var contentWithExcessiveVersionsCount = dataBaseService.GetContentWithExcessiveVersionsCount(contentVersionWarningThreshold);
            var usageByAlias = usage
                .Where(x => !string.IsNullOrWhiteSpace(x.Alias))
                .GroupBy(x => x.Alias)
                .ToDictionary(x => x.Key, x => x.Sum(y => y.NodeCount));

            foreach (var edge in referenceEdges.Where(x => x.Relation == "configures missing block type"))
            {
                findings.Add(CreateFinding(
                    "broken-block-reference",
                    "High",
                    "Broken References",
                    "Block editor references a missing element type",
                    $"{edge.SourceName} references a block element type that was not found. Context: {edge.Context}.",
                    "Data Type",
                    edge.SourceName,
                    edge.SourceAlias,
                    edge.SourceKey,
                    "Open the data type configuration and remove or replace the missing block type."));
            }

            foreach (var contentType in contentTypes.Where(x => !x.IsElement && !x.HasTemplates))
            {
                findings.Add(CreateFinding(
                    "doctype-no-templates",
                    "Low",
                    "Content Model",
                    "Document type has no allowed templates",
                    $"{contentType.Name} is a document type, but it has no allowed templates. It may be intentional for headless/API-only content.",
                    "Document Type",
                    contentType.Name,
                    contentType.Alias,
                    contentType.Udi.ToString(),
                    "Add an allowed template, mark it as an element type if it should never render, or keep it documented as API-only."));
            }

            foreach (var contentType in contentTypes.Where(x => !x.AllProperties.Any()))
            {
                findings.Add(CreateFinding(
                    "contenttype-no-properties",
                    contentType.IsElement ? "Low" : "Medium",
                    "Content Model",
                    contentType.IsElement ? "Element type has no properties" : "Document type has no properties",
                    $"{contentType.Name} has no own or inherited properties.",
                    contentType.IsElement ? "Element Type" : "Document Type",
                    contentType.Name,
                    contentType.Alias,
                    contentType.Udi.ToString(),
                    "Confirm this is a deliberate structural type; otherwise add properties or remove the type."));
            }

            foreach (var contentType in contentTypes.Where(x => !x.IsElement && usageByAlias.TryGetValue(x.Alias, out var count) && count == 0))
            {
                findings.Add(CreateFinding(
                    "doctype-no-instances",
                    "Low",
                    "Content Usage",
                    "Document type has no content instances",
                    $"{contentType.Name} exists in the schema but has no content items.",
                    "Document Type",
                    contentType.Name,
                    contentType.Alias,
                    contentType.Udi.ToString(),
                    "Review whether this type is still needed or whether it is waiting for future content."));
            }

            foreach (var dataType in dataTypes.Where(x => !x.IsUsed && !x.IsNestedUsed))
            {
                findings.Add(CreateFinding(
                    "datatype-unused",
                    "Low",
                    "Data Types",
                    "Data type appears unused",
                    $"{dataType.Name} is not used directly by document/media types and was not found in supported block editor configurations.",
                    "Data Type",
                    dataType.Name,
                    dataType.Alias,
                    dataType.Udi.ToString(),
                    "Delete it if obsolete, or keep it if it is intentionally reserved for future schema work."));
            }

            foreach (var duplicateGroup in dataTypes.Where(x => !string.IsNullOrWhiteSpace(x.Name)).GroupBy(x => x.Name).Where(x => x.Count() > 1))
            {
                var aliases = string.Join(", ", duplicateGroup.Select(x => x.Alias).Distinct().OrderBy(x => x));
                findings.Add(CreateFinding(
                    "datatype-duplicate-name",
                    "Low",
                    "Data Types",
                    "Multiple data types share the same name",
                    $"{duplicateGroup.Key} appears {duplicateGroup.Count()} times. Editor aliases: {aliases}.",
                    "Data Type",
                    duplicateGroup.Key,
                    aliases,
                    string.Empty,
                    "Rename duplicates so future schema changes are easier to reason about."));
            }

            findings.AddRange(deliveryApiDiagnostics.Findings);

            var referencedTemplateKeys = referenceEdges
                .Where(x => x.TargetType == "Template")
                .Select(x => x.TargetKey)
                .ToHashSet();
            var templatesReferencedByTemplates = templates
                .SelectMany(template => template.Parents
                    .Where(parent => parent.Id != template.Id)
                    .Select(parent => parent.Udi.ToString()))
                .ToHashSet();

            foreach (var template in templates.Where(x => !referencedTemplateKeys.Contains(x.Udi.ToString()) && !templatesReferencedByTemplates.Contains(x.Udi.ToString())))
            {
                findings.Add(CreateFinding(
                    "template-unreferenced",
                    "Low",
                    "Templates",
                    "Template is not allowed by any document type",
                    $"{template.Name} exists but is not referenced by any document type allowed-template relationship or another template.",
                    "Template",
                    template.Name,
                    template.Alias,
                    template.Udi.ToString(),
                    "Remove it if obsolete, or assign it to the document type that should render with it."));
            }

            if (orphanedTags.Any())
            {
                findings.Add(CreateFinding(
                    "orphaned-tags",
                    "Low",
                    "Content Cleanup",
                    "Orphaned tags exist",
                    $"{orphanedTags.Count} tags exist in the database but are not associated with any content or media.",
                    "Tags",
                    "Orphaned tags",
                    string.Empty,
                    string.Empty,
                    "Use the Tag Browser to review and delete orphaned tags if they are no longer needed."));
            }

            if (orphanedMediaCount > 0)
            {
                findings.Add(CreateFinding(
                    "orphaned-media",
                    "Low",
                    "Content Cleanup",
                    "Orphaned media exists",
                    $"{orphanedMediaCount} media items have no incoming Umbraco relation and may be unused.",
                    "Media",
                    "Orphaned media",
                    string.Empty,
                    string.Empty,
                    "Review media usage before deleting; not every custom picker or rich text reference may create an Umbraco relation."));
            }

            foreach (var language in languagesWithoutAssignedDomains)
            {
                findings.Add(CreateFinding(
                    "language-no-hostname",
                    "Medium",
                    "Culture & Hostnames",
                    "Language has no assigned hostname",
                    $"{language.Name} ({language.Culture}) exists as a language, but no Culture and Hostnames entry points to it.",
                    "Language",
                    language.Name,
                    language.Culture,
                    language.Id.ToString(),
                    "Open Settings > Languages > Culture and Hostnames and assign at least one hostname to this language if it should resolve site URLs."));
            }

            if (logRowCount > logRowWarningThreshold)
            {
                findings.Add(CreateFinding(
                    "log-table-large",
                    "Medium",
                    "Database",
                    "umbracoLog table is large",
                    $"The log table contains {logRowCount:n0} rows, which can slow diagnostics and database maintenance.",
                    "Database Table",
                    "umbracoLog",
                    string.Empty,
                    string.Empty,
                    "Review log retention, archive old rows, and investigate noisy recurring errors."));
            }

            if (contentWithExcessiveVersionsCount > 0)
            {
                findings.Add(CreateFinding(
                    "content-excessive-versions",
                    "Medium",
                    "Database",
                    "Content has many previous versions",
                    $"{contentWithExcessiveVersionsCount} content items have more than {contentVersionWarningThreshold} versions. The version table contains {contentVersionCount:n0} rows in total.",
                    "Database Table",
                    "umbracoContentVersion",
                    string.Empty,
                    string.Empty,
                    "Review content version cleanup settings and prune old versions where appropriate."));
            }

            foreach (var contentType in contentTypes)
            {
                var allProperties = contentType.AllProperties?.ToList() ?? [];

                if (allProperties.Count > propertyCountWarningThreshold)
                {
                    findings.Add(CreateFinding(
                        "contenttype-excessive-properties",
                        "Low",
                        "Content Model",
                        contentType.IsElement ? "Element type has a very high property count" : "Document type has a very high property count",
                        $"{contentType.Name} has {allProperties.Count} properties (own and inherited). Very large types are slower to edit and often indicate responsibilities that could be split into compositions.",
                        contentType.IsElement ? "Element Type" : "Document Type",
                        contentType.Name,
                        contentType.Alias,
                        contentType.Udi.ToString(),
                        "Consider splitting shared properties into compositions, or confirm the size is intentional."));
                }

                var duplicateAliases = allProperties
                    .Where(x => !string.IsNullOrWhiteSpace(x.Alias))
                    .GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .OrderBy(x => x)
                    .ToList();

                if (duplicateAliases.Any())
                {
                    findings.Add(CreateFinding(
                        "contenttype-duplicate-property-alias",
                        "Medium",
                        "Content Model",
                        "Composition produces a duplicate property alias",
                        $"{contentType.Name} resolves more than one property to the same alias ({string.Join(", ", duplicateAliases)}). This usually comes from overlapping compositions and can cause unexpected editing or rendering behaviour.",
                        contentType.IsElement ? "Element Type" : "Document Type",
                        contentType.Name,
                        contentType.Alias,
                        contentType.Udi.ToString(),
                        "Review the compositions on this type and remove or rename the clashing property so each alias is defined once."));
                }
            }

            var lockedOutMemberCount = dataBaseService
                .GetMembers(1, 1, isLockedOut: true)
                .TotalItems;

            if (lockedOutMemberCount > 0)
            {
                findings.Add(CreateFinding(
                    "members-locked-out",
                    "Low",
                    "Members",
                    "Members are locked out",
                    $"{lockedOutMemberCount:n0} members are currently locked out. A cluster of lock-outs can indicate brute-force attempts or a broken login flow.",
                    "Members",
                    "Locked out members",
                    string.Empty,
                    string.Empty,
                    "Review the Member Browser, unlock legitimate accounts, and investigate repeated failed logins if the count is unexpectedly high."));
            }

            var unapprovedMemberCount = dataBaseService
                .GetMembers(1, 1, isApproved: false)
                .TotalItems;

            if (unapprovedMemberCount > 0)
            {
                findings.Add(CreateFinding(
                    "members-unapproved",
                    "Low",
                    "Members",
                    "Members are not approved",
                    $"{unapprovedMemberCount:n0} members exist but are not approved, so they cannot log in. These may be stale sign-ups or an incomplete approval workflow.",
                    "Members",
                    "Unapproved members",
                    string.Empty,
                    string.Empty,
                    "Approve the members who should have access, or remove abandoned sign-ups to keep the member list clean."));
            }

            foreach (var insight in logService
                .GetInsights(from: null, to: null, take: 5)
                .Where(x => x.Count >= recurringErrorWarningThreshold && (x.Level == "Error" || x.Level == "Fatal")))
            {
                var lastSeen = insight.LastSeen.HasValue
                    ? $" Last seen {insight.LastSeen.Value.UtcDateTime:yyyy-MM-dd HH:mm} UTC."
                    : string.Empty;

                findings.Add(CreateFinding(
                    "log-recurring-error",
                    insight.Level == "Fatal" ? "High" : "Medium",
                    "Logs",
                    $"Recurring {insight.Level.ToLowerInvariant()} in the logs",
                    $"\"{insight.Title}\" has been logged {insight.Count:n0} times.{lastSeen} Repeated errors of this kind are usually worth fixing at the source rather than ignoring.",
                    "Log Entry",
                    string.IsNullOrWhiteSpace(insight.SourceContext) ? insight.Title : insight.SourceContext,
                    string.Empty,
                    insight.Id,
                    "Open the Log Browser, filter to this error, and address the underlying cause."));
            }

            foreach (var status in dataBaseService
                .GetDictionaryTranslationStatus()
                .Where(x => x.TotalItems > 0 && x.UntranslatedItems > 0))
            {
                findings.Add(CreateFinding(
                    "dictionary-untranslated",
                    "Low",
                    "Dictionary",
                    "Dictionary items are untranslated",
                    $"{status.UntranslatedItems:n0} of {status.TotalItems:n0} dictionary items have no value (or a blank value) for {status.Name} ({status.Culture}).",
                    "Language",
                    status.Name,
                    status.Culture,
                    status.LanguageId.ToString(),
                    "Open the Dictionary in Settings and provide translations for this language, or remove unused dictionary items."));
            }

            var isProduction = hostEnvironment.IsProduction();
            var global = globalSettings.Value;

            if (!global.UseHttps)
            {
                findings.Add(CreateFinding(
                    "config-usehttps-disabled",
                    isProduction ? "Medium" : "Low",
                    "Configuration",
                    "HTTPS is not enforced",
                    $"Umbraco:CMS:Global:UseHttps is false{(isProduction ? " in a Production environment" : string.Empty)}, so authentication and session cookies are not flagged as secure-only.",
                    "Configuration",
                    "Umbraco:CMS:Global:UseHttps",
                    string.Empty,
                    string.Empty,
                    "Serve the site over HTTPS and set Umbraco:CMS:Global:UseHttps to true so cookies are only sent over secure connections."));
            }

            if (string.IsNullOrWhiteSpace(global.Smtp?.Host))
            {
                findings.Add(CreateFinding(
                    "config-smtp-missing",
                    "Medium",
                    "Configuration",
                    "SMTP is not configured",
                    "No Umbraco:CMS:Global:Smtp host is configured. Password reset emails, invitations and health-check notifications will silently fail to send.",
                    "Configuration",
                    "Umbraco:CMS:Global:Smtp",
                    string.Empty,
                    string.Empty,
                    "Configure an SMTP host (and From address) so the backoffice can send mail, or confirm that mail is handled by a custom IEmailSender."));
            }

            if (isProduction && string.Equals(modelsBuilderSettings.Value.ModelsMode.ToString(), "InMemoryAuto", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(CreateFinding(
                    "config-modelsbuilder-inmemory-production",
                    "Medium",
                    "Configuration",
                    "Models Builder is in InMemoryAuto mode in Production",
                    "ModelsBuilder is set to InMemoryAuto in a Production environment. Models are recompiled in memory at runtime, which adds overhead and can cause app-pool restarts under load.",
                    "Configuration",
                    "Umbraco:CMS:ModelsBuilder:ModelsMode",
                    string.Empty,
                    string.Empty,
                    "Switch to SourceCodeManual (or Nothing) for production and generate models at build/deploy time."));
            }

            AddDateTimeFindings(findings);
            AddScheduledPublishingFindings(findings);

            return findings
                .Where(x => !IsIgnoredAlias(x.EntityAlias))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Category)
                .ThenBy(x => x.Title)
                .ThenBy(x => x.EntityName);
        }

        private void AddDateTimeFindings(List<HealthRiskFinding> findings)
            => findings.AddRange(BuildDateTimeFindings(serverTimeService.GetServerTime(), serverTimeService.GetSystemDateEvidence()));

        internal static IEnumerable<HealthRiskFinding> BuildDateTimeFindings(ServerTimeInfo time, SystemDateEvidence evidence)
        {
            var findings = new List<HealthRiskFinding>();
            var migration = time.SystemDateMigration;

            if (migration.ConfiguredTimeZoneValid == false)
            {
                findings.Add(CreateFinding(
                    "time-migration-zone-invalid",
                    "Info",
                    "Dates & Time",
                    "Current migration time zone is not valid for this database",
                    $"LocalServerTimeZone is currently '{migration.ConfiguredTimeZone}'. {migration.TimeZoneValidationMessage} Current settings do not establish what a completed upgrade used; fresh v17 installs have no historical dates to convert.",
                    "Configuration",
                    "Umbraco:CMS:SystemDateMigration:LocalServerTimeZone",
                    string.Empty,
                    string.Empty,
                    "Review the actual upgrade logs before changing historical dates. SQL Server requires an ID listed in sys.time_zone_info; SQLite requires an ID that resolves on the web server."));
            }
            else if (migration.ConfiguredTimeZoneValid == true && migration.EffectiveTimeZoneResolved && !migration.EffectiveTimeZoneMatchesServer)
            {
                findings.Add(CreateFinding(
                    "time-migration-zone-differs",
                    "Info",
                    "Dates & Time",
                    "SystemDateMigration time zone differs from this server",
                    $"LocalServerTimeZone is currently '{migration.ConfiguredTimeZone}', with different adjustment rules from this server's '{time.TimeZoneId}'. This can be intentional after moving servers and does not establish what a completed upgrade used.",
                    "Configuration",
                    "Umbraco:CMS:SystemDateMigration:LocalServerTimeZone",
                    string.Empty,
                    string.Empty,
                    "Confirm the configured zone matches the server that wrote the dates before the upgrade. Once the migration has run the setting can be removed."));
            }

            if (!migration.Enabled && !migration.EffectiveTimeZoneIsUtc)
            {
                findings.Add(CreateFinding(
                    "time-migration-disabled",
                    "Info",
                    "Dates & Time",
                    "UTC system date migration is disabled",
                    "SystemDateMigration:Enabled is currently false. This does not establish whether an earlier upgrade skipped conversion; fresh v17 installations do not need historical conversion.",
                    "Configuration",
                    "Umbraco:CMS:SystemDateMigration:Enabled",
                    string.Empty,
                    string.Empty,
                    "Review upgrade logs and known historical dates. Changing this setting after a completed upgrade does not rerun the migration. Do not change stored dates based on this setting alone."));
            }

            if (migration.Enabled && migration.UsesBaseOffsetOnly && !migration.EffectiveTimeZoneIsUtc && migration.EffectiveTimeZoneSupportsDaylightSaving == true)
            {
                findings.Add(CreateFinding(
                    "time-migration-sqlite-dst",
                    "Info",
                    "Dates & Time",
                    "SQLite UTC migration ignores daylight saving",
                    $"SQLite converts using the base offset of '{migration.EffectiveTimeZone}' only. If that zone was used for a pre-v17 upgrade, dates written during daylight saving may be shifted. The current setting does not prove which zone the upgrade used; fresh v17 installs are unaffected.",
                    "Database",
                    "SQLite",
                    string.Empty,
                    string.Empty,
                    "Only relevant to sites upgraded from Umbraco 16 or earlier. Spot check summer dates in the content and audit history."));
            }

            foreach (var entry in evidence.MigrationLogEntries.Where(x => x.Level is "Error" or "Fatal"))
            {
                findings.Add(CreateFinding(
                    "time-migration-log-error",
                    "Medium",
                    "Dates & Time",
                    "UTC system date migration logged an error",
                    $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss zzz}: {entry.Message}",
                    "Migration",
                    "MigrateSystemDatesToUtc",
                    string.Empty,
                    string.Empty,
                    "Check the full entry and subsequent upgrade results. A historical error does not establish an unresolved failure or justify changing stored dates."));
            }

            if (!evidence.LogCheckSucceeded || !evidence.DatabaseCheckSucceeded)
            {
                findings.Add(CreateFinding("time-evidence-incomplete", "Low", "Dates & Time", "Date evidence is incomplete",
                    $"Logs: {evidence.LogCheckMessage} Database: {evidence.DatabaseCheckMessage}", "Diagnostics", "Date evidence", string.Empty, string.Empty,
                    "Review the failed checks before drawing conclusions. Evidence is cached for up to two minutes."));
            }

            foreach (var row in evidence.FutureDatedRows.Where(x => x.CheckSucceeded && x.Count > 0))
            {
                findings.Add(CreateFinding(
                    "time-future-dated-rows",
                    "Medium",
                    "Dates & Time",
                    "System dates are in the future",
                    $"{row.Count:n0} row(s) in {row.Table}.{row.Column} are later than the check's UTC cutoff {evidence.FutureDateCutoffUtc:yyyy-MM-dd HH:mm} (latest {row.Latest:yyyy-MM-dd HH:mm} UTC). Possible causes include clock differences, imported data or shifted local dates. This check cannot detect older shifts or dates shifted into the past.",
                    "Database",
                    $"{row.Table}.{row.Column}",
                    string.Empty,
                    string.Empty,
                    "Compare with known event times and upgrade logs. No matches is not proof of correct migration; do not rewrite dates based on this heuristic alone."));
            }
            return findings;
        }

        private void AddScheduledPublishingFindings(List<HealthRiskFinding> findings)
            => findings.AddRange(BuildScheduledPublishingFindings(serverTimeService.GetContentSchedules()));

        internal static IEnumerable<HealthRiskFinding> BuildScheduledPublishingFindings(ContentScheduleOverview schedules)
        {
            var findings = new List<HealthRiskFinding>();
            const int maxOverdueFindings = 25;
            var overdue = schedules.Items.Where(x => x.IsOverdue && !x.Trashed).ToList();

            if (schedules.ScheduledPublishingSuspended)
            {
                findings.Add(CreateFinding("schedule-suspended", overdue.Count > 0 ? "High" : "Info", "Scheduled Publishing",
                    "Scheduled publishing is suspended", "The publishing job can update its last-run timestamp without processing schedules while publishing is suspended.",
                    "Background Job", "ScheduledPublishingJob", string.Empty, string.Empty, "Check why scheduled publishing was suspended before resuming it."));
            }

            if (!schedules.JobsCheckSucceeded || !schedules.ServersCheckSucceeded)
            {
                findings.Add(CreateFinding("schedule-evidence-incomplete", "Low", "Scheduled Publishing", "Scheduling evidence is incomplete",
                    "Job or server registrations could not be read. An unavailable check does not mean there are no jobs or servers.",
                    "Diagnostics", "Scheduled Publishing", string.Empty, string.Empty, "Check the server logs for database read errors."));
            }

            foreach (var item in overdue.Take(maxOverdueFindings))
            {
                var action = item.Action == "Expire" ? "unpublish" : "publish";
                var culture = string.IsNullOrEmpty(item.Culture) ? string.Empty : $" ({item.Culture})";

                findings.Add(CreateFinding(
                    "schedule-overdue",
                    "High",
                    "Scheduled Publishing",
                    $"Scheduled {action} did not run",
                    $"{item.Name}{culture} was scheduled to {action} at {item.Date:yyyy-MM-dd HH:mm} UTC, more than {schedules.OverdueAfterMinutes} minutes ago, and the schedule is still pending.",
                    "Content",
                    item.Name,
                    item.ContentTypeAlias,
                    item.NodeKey.ToString(),
                    "Check the ScheduledPublishingJob in the Scheduled Publishing view and the logs for publishing errors (for example validation failures or a missing parent)."));
            }

            if (overdue.Count > maxOverdueFindings)
            {
                findings.Add(CreateFinding(
                    "schedule-overdue-more",
                    "High",
                    "Scheduled Publishing",
                    "More scheduled publishing did not run",
                    $"A further {overdue.Count - maxOverdueFindings:n0} overdue schedule(s) are not listed individually.",
                    "Content",
                    "Scheduled Publishing",
                    string.Empty,
                    string.Empty,
                    "Open the Scheduled Publishing view to see every overdue entry."));
            }

            var publishingJob = schedules.Jobs.FirstOrDefault(x => x.Name == "ScheduledPublishingJob");
            foreach (var job in schedules.Jobs.Where(x => x.IsStale))
            {
                var isPublishing = job == publishingJob;

                findings.Add(CreateFinding(
                    "background-job-stale",
                    isPublishing && schedules.Items.Any() ? "High" : "Medium",
                    "Scheduled Publishing",
                    "Background job last-run timestamp is old",
                    $"The distributed job {job.Name} runs every {TimeSpan.FromSeconds(job.PeriodSeconds)} but last ran at {job.LastRun:yyyy-MM-dd HH:mm} UTC (last attempt {job.LastAttemptedRun:yyyy-MM-dd HH:mm} UTC){(job.IsRunning ? " and is still marked as running" : string.Empty)}.",
                    "Background Job",
                    job.Name,
                    string.Empty,
                    string.Empty,
                    job.IsRunning
                        ? "The job may still be executing or may have been interrupted. Compare the last attempt with its expected execution time and logs before considering a restart."
                        : "Check that background jobs are enabled and that at least one server is running, then check the logs for errors."));
            }

            foreach (var server in schedules.Servers.Where(x => schedules.AutomaticServerRegistration && x.IsActive && x.IsStale))
            {
                findings.Add(CreateFinding(
                    "server-stale",
                    "Low",
                    "Scheduled Publishing",
                    "Registered server has stopped checking in",
                    $"{server.ComputerName} ({server.Address}) is marked active but last checked in at {server.LastNotifiedDate:yyyy-MM-dd HH:mm} UTC.",
                    "Server",
                    server.ComputerName,
                    string.Empty,
                    string.Empty,
                    "Check whether the server is still deployed and can reach the database. Automatic election normally deactivates stale registrations; switching to fixed roles bypasses those updates and can leave historical rows."));
            }
            return findings;
        }

        private bool IsIgnoredAlias(string alias)
        {
            return !string.IsNullOrWhiteSpace(alias)
                && godModeConfig.Value.AliasesToIgnore.Any(x => alias.InvariantEquals(x));
        }

        private static HealthRiskFinding CreateFinding(string checkId, string severity, string category, string title, string detail, string entityType, string entityName, string entityAlias, string entityKey, string recommendation)
            => new()
            {
                CheckId = checkId,
                Severity = severity,
                Score = severity switch
                {
                    "High" => 80,
                    "Medium" => 50,
                    "Low" => 20,
                    _ => 10
                },
                Category = category,
                Title = title,
                Detail = detail,
                EntityType = entityType,
                EntityName = entityName,
                EntityAlias = entityAlias,
                EntityKey = entityKey,
                Recommendation = recommendation
            };
    }
}
