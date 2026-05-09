using System.Text.Json;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;

namespace Diplo.GodMode.AI.Services
{
    public class FixPlanAiAnalysisProvider : IGodModeAnalysisProvider
    {
        private const int MaxItemsPerIssue = 20;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false
        };

        private readonly IAIChatService chatService;
        private readonly GodModeAiConfig config;
        private readonly ILogger<FixPlanAiAnalysisProvider> logger;

        public FixPlanAiAnalysisProvider(
            IAIChatService chatService,
            IOptions<GodModeAiConfig> config,
            ILogger<FixPlanAiAnalysisProvider> logger)
        {
            this.chatService = chatService;
            this.config = config.Value;
            this.logger = logger;
        }

        public string Alias => GodModeAiAnalysisService.FixPlanProviderAlias;

        public string Name => "AI Fix Plan";

        public async Task<GodModeAnalysisResult> AnalyzeAsync(GodModeAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, """
                        You are an expert Umbraco CMS technical lead creating a practical remediation plan.
                        Return only data that conforms to the supplied structured output schema.
                        Each finding is one fix-plan item, ordered by priority.
                        Use title as the action title, score as priority score from 1-100, detail as impact plus reasoning, recommendation as the exact next action.
                        Include effort in the category field using one of: Quick Win, Moderate, Larger Refactor, Review First.
                        Include affectedEntities whenever the evidence contains names, aliases or keys.
                        Return at most 6 plan items and at most 8 affectedEntities per item.
                        Prefer high-confidence fixes over generic advice. Do not invent entities.
                        """),
                    new(ChatRole.User, BuildPrompt(request))
                };

                var response = await chatService.GetChatResponseAsync(
                    builder =>
                    {
                        builder.WithAlias("diplo-godmode-ai-fix-plan");
                        builder.WithName("Diplo GodMode AI Fix Plan");

                        var options = BuildChatOptions();
                        if (options is not null)
                        {
                            builder.WithChatOptions(options);
                        }

                        builder.WithOutputSchema(AIOutputSchema.FromType<AiFixPlanPayload>());

                        if (!string.IsNullOrWhiteSpace(config.ChatProfileAlias))
                        {
                            builder.WithProfile(config.ChatProfileAlias);
                        }
                    },
                    messages,
                    cancellationToken);

                return ParseResponse(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI fix plan failed.");
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = "AI fix plan could not be created.",
                    Findings =
                    [
                        new GodModeAnalysisFinding
                        {
                            Severity = "High",
                            Score = 80,
                            Category = "AI Configuration",
                            Title = "Fix plan could not be created",
                            Detail = ex.Message,
                            EntityType = "AI Provider",
                            EntityName = "Umbraco.AI",
                            EntityAlias = string.Empty,
                            Recommendation = "Check the Umbraco.AI profile and retry."
                        }
                    ],
                    SuggestedNextSteps = ["Review the Umbraco.AI provider configuration and retry."]
                };
            }
        }

        private ChatOptions BuildChatOptions()
        {
            if (config.MaxOutputTokens is null && config.Temperature is null)
            {
                return null;
            }

            return new ChatOptions
            {
                MaxOutputTokens = config.MaxOutputTokens,
                Temperature = config.Temperature
            };
        }

        private static string BuildPrompt(GodModeAnalysisRequest request)
        {
            var evidence = ToFixPlanEvidence(request.Snapshot);
            return $"""
                Task:
                {request.Prompt}

                Evidence JSON:
                {JsonSerializer.Serialize(evidence, JsonOptions)}
                """;
        }

        private static object ToFixPlanEvidence(GodModeSnapshot snapshot)
        {
            var contentTypes = snapshot.ContentTypes.ToList();
            var dataTypes = snapshot.DataTypes.ToList();
            var references = snapshot.ReferenceGraph.ToList();
            var templates = snapshot.Templates.ToList();
            var healthFindings = snapshot.HealthRiskFindings.ToList();
            var driftFindings = snapshot.ConfigurationDriftFindings.ToList();
            var referencedTemplateKeys = references
                .Where(x => x.TargetType == "Template")
                .Select(x => x.TargetKey)
                .ToHashSet();
            var templatesReferencedByTemplates = templates
                .SelectMany(template => (template.Parents ?? [])
                    .Where(parent => parent.Id != template.Id)
                    .Select(parent => parent.Udi.ToString()))
                .ToHashSet();

            var noPropertyTypes = contentTypes.Where(x => !x.AllProperties.Any()).ToList();
            var noTemplateTypes = contentTypes.Where(x => !x.IsElement && !x.HasTemplates).ToList();
            var unusedDataTypes = dataTypes.Where(x => !x.IsUsed && !x.IsNestedUsed).ToList();
            var unassignedTemplates = templates
                .Where(x => !referencedTemplateKeys.Contains(x.Udi.ToString()) && !templatesReferencedByTemplates.Contains(x.Udi.ToString()))
                .ToList();

            return new
            {
                totals = new
                {
                    documentTypes = contentTypes.Count(x => !x.IsElement),
                    elementTypes = contentTypes.Count(x => x.IsElement),
                    dataTypes = dataTypes.Count,
                    templates = templates.Count,
                    references = references.Count,
                    healthFindings = healthFindings.Count,
                    driftFindings = driftFindings.Count
                },
                fixInputs = new
                {
                    noProperties = ToCandidateSet(noPropertyTypes, ToAffectedEntity),
                    noAllowedTemplates = ToCandidateSet(noTemplateTypes, ToAffectedEntity),
                    unusedDataTypes = ToCandidateSet(unusedDataTypes, ToAffectedEntity),
                    unassignedTemplates = ToCandidateSet(unassignedTemplates, ToAffectedEntity),
                    orphanedMedia = ToCandidateSet(snapshot.OrphanedMedia.ToList(), x => new
                    {
                        entityType = "Media",
                        name = x.Name,
                        alias = x.Path,
                        key = x.Udi.ToString(),
                        context = $"Media id {x.Id}"
                    }),
                    orphanedTags = ToCandidateSet(snapshot.OrphanedTags.ToList(), x => new
                    {
                        entityType = "Tag",
                        name = x.Text,
                        alias = x.Group,
                        key = x.Id.ToString(),
                        context = x.Culture
                    }),
                    healthFindings = healthFindings.Take(MaxItemsPerIssue).Select(x => new
                    {
                        x.Severity,
                        x.Score,
                        x.Category,
                        x.Title,
                        x.EntityType,
                        x.EntityName,
                        x.EntityAlias,
                        x.Recommendation
                    }),
                    driftFindings = driftFindings.Take(MaxItemsPerIssue).Select(x => new
                    {
                        x.Severity,
                        x.Score,
                        x.Category,
                        x.EntityType,
                        x.EntityName,
                        x.EntityAlias,
                        x.Summary,
                        x.Recommendation
                    })
                }
            };
        }

        private static object ToCandidateSet<T>(IReadOnlyCollection<T> items, Func<T, object> map)
            => new
            {
                count = items.Count,
                items = items.Take(MaxItemsPerIssue).Select(map)
            };

        private static object ToAffectedEntity(ContentTypeMap item)
            => ToAffectedEntity(item.IsElement ? "Element Type" : "Document Type", item.Name, item.Alias, item.Udi.ToString(), null);

        private static object ToAffectedEntity(DataTypeMap item)
            => ToAffectedEntity("Data Type", item.Name, item.Alias, item.Udi.ToString(), null);

        private static object ToAffectedEntity(TemplateModel item)
            => ToAffectedEntity("Template", item.Name, item.Alias, item.Udi.ToString(), null);

        private static object ToAffectedEntity(string entityType, string name, string alias, string key, string context)
            => new
            {
                entityType,
                name,
                alias,
                key,
                context
            };

        private GodModeAnalysisResult ParseResponse(ChatResponse response)
        {
            if (response.FinishReason == ChatFinishReason.Length)
            {
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = "The AI fix plan response was cut off before the structured JSON was complete.",
                    Findings =
                    [
                        new GodModeAnalysisFinding
                        {
                            Severity = "Medium",
                            Score = 50,
                            Category = "AI Configuration",
                            Title = "Fix plan response was truncated",
                            Detail = UsageText(response),
                            EntityType = "AI Provider",
                            EntityName = response.ModelId ?? "Configured chat model",
                            EntityAlias = config.ChatProfileAlias ?? string.Empty,
                            Recommendation = "Reduce the number of plan items or increase the Umbraco.AI profile max tokens."
                        }
                    ],
                    SuggestedNextSteps = ["Reduce the requested fix-plan size or increase the Umbraco.AI profile max tokens."]
                };
            }

            try
            {
                var json = ExtractJson(response.Text);
                var payload = JsonSerializer.Deserialize<AiFixPlanPayload>(json, JsonOptions);
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = payload?.Summary ?? "AI fix plan created.",
                    Findings = payload?.Findings.Select(ToFinding).ToList() ?? [],
                    SuggestedNextSteps = payload?.SuggestedNextSteps ?? []
                };
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "AI fix plan returned non-JSON content.");
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = "The AI provider did not return parseable structured JSON for the fix plan.",
                    SuggestedNextSteps = ["Check that the selected model supports structured JSON schema responses."]
                };
            }
        }

        private static string ExtractJson(string text)
        {
            var trimmed = text.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
        }

        private static string UsageText(ChatResponse response)
        {
            var usage = response.Usage;
            return usage is null
                ? "The provider stopped because it reached the output token limit."
                : $"The provider stopped because it reached the output token limit. Token usage: input {usage.InputTokenCount?.ToString() ?? "unknown"}, output {usage.OutputTokenCount?.ToString() ?? "unknown"}, total {usage.TotalTokenCount?.ToString() ?? "unknown"}.";
        }

        private static GodModeAnalysisFinding ToFinding(AiFixPlanFinding finding)
            => new()
            {
                Severity = finding.Severity,
                Score = finding.Score,
                Category = finding.Category,
                Title = finding.Title,
                Detail = finding.Detail,
                EntityType = finding.EntityType,
                EntityName = finding.EntityName,
                EntityAlias = finding.EntityAlias,
                Recommendation = finding.Recommendation,
                AffectedEntities = finding.AffectedEntities.Select(ToAffectedEntity).ToList()
            };

        private static GodModeAffectedEntity ToAffectedEntity(AiAffectedEntityPayload entity)
            => new()
            {
                EntityType = entity.EntityType,
                Name = entity.Name,
                Alias = entity.Alias,
                Key = entity.Key,
                Context = entity.Context
            };

        private sealed class AiFixPlanPayload
        {
            public string Summary { get; set; } = string.Empty;

            public List<AiFixPlanFinding> Findings { get; set; } = [];

            public List<string> SuggestedNextSteps { get; set; } = [];
        }

        private sealed class AiFixPlanFinding
        {
            public string Severity { get; set; } = "Info";

            public int Score { get; set; }

            public string Category { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public string Detail { get; set; } = string.Empty;

            public string EntityType { get; set; } = string.Empty;

            public string EntityName { get; set; } = string.Empty;

            public string EntityAlias { get; set; } = string.Empty;

            public List<AiAffectedEntityPayload> AffectedEntities { get; set; } = [];

            public string Recommendation { get; set; } = string.Empty;
        }

        private sealed class AiAffectedEntityPayload
        {
            public string EntityType { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string Alias { get; set; } = string.Empty;

            public string Key { get; set; } = string.Empty;

            public string Context { get; set; } = string.Empty;
        }
    }
}
