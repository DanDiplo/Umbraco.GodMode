using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Options;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    public class GodModeHealthRiskService : IGodModeHealthRiskService
    {
        private readonly IUmbracoDataService dataService;
        private readonly IUmbracoDatabaseService dataBaseService;
        private readonly IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService;
        private readonly IOptions<GodModeConfig> godModeConfig;

        public GodModeHealthRiskService(
            IUmbracoDataService dataService,
            IUmbracoDatabaseService dataBaseService,
            IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService,
            IOptions<GodModeConfig> godModeConfig)
        {
            this.dataService = dataService;
            this.dataBaseService = dataBaseService;
            this.deliveryApiDiagnosticsService = deliveryApiDiagnosticsService;
            this.godModeConfig = godModeConfig;
        }

        public async Task<IEnumerable<HealthRiskFinding>> BuildHealthRiskFindingsAsync(CancellationToken cancellationToken = default)
        {
            const int logRowWarningThreshold = 100000;
            const int contentVersionWarningThreshold = 50;

            cancellationToken.ThrowIfCancellationRequested();

            var findings = new List<HealthRiskFinding>();
            var contentTypes = dataService.GetContentTypeMap().ToList();
            var dataTypes = (await dataService.GetDataTypesStatus()).ToList();
            var templates = (await dataService.GetTemplates()).ToList();
            var referenceEdges = (await dataService.GetSchemaReferenceGraph()).ToList();
            var driftFindings = (await dataService.GetConfigurationDriftFindings()).ToList();
            var deliveryApiDiagnostics = await deliveryApiDiagnosticsService.GetDiagnosticsAsync(cancellationToken);
            var usage = dataBaseService.GetContentUsageData().ToList();
            var orphanedTags = dataBaseService.GetOrphanedTags();
            var orphanedMediaCount = dataBaseService.GetOrphanedMediaCount();
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

            foreach (var drift in driftFindings.Where(x => x.Score >= 50).Take(25))
            {
                findings.Add(CreateFinding(
                    drift.Severity,
                    "Configuration Drift",
                    drift.Category,
                    drift.Summary,
                    drift.EntityType,
                    drift.EntityName,
                    drift.EntityAlias,
                    drift.EntityKey,
                    drift.Recommendation));
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

            if (logRowCount > logRowWarningThreshold)
            {
                findings.Add(CreateFinding(
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

            return findings
                .Where(x => !IsIgnoredAlias(x.EntityAlias))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Category)
                .ThenBy(x => x.Title)
                .ThenBy(x => x.EntityName);
        }

        private bool IsIgnoredAlias(string alias)
        {
            return !string.IsNullOrWhiteSpace(alias)
                && godModeConfig.Value.AliasesToIgnore.Any(x => alias.InvariantEquals(x));
        }

        private static HealthRiskFinding CreateFinding(string severity, string category, string title, string detail, string entityType, string entityName, string entityAlias, string entityKey, string recommendation)
            => new()
            {
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
