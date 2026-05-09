using System.Text.Json;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;

namespace Diplo.GodMode.AI.Services
{
    public class SchemaHealthAiAnalysisProvider : IGodModeAnalysisProvider
    {
        private const int MaxIssueCandidateItems = 25;
        private const int MaxSignalItems = 20;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false
        };

        private readonly IAIChatService chatService;
        private readonly GodModeAiConfig config;
        private readonly ILogger<SchemaHealthAiAnalysisProvider> logger;

        public SchemaHealthAiAnalysisProvider(
            IAIChatService chatService,
            IOptions<GodModeAiConfig> config,
            ILogger<SchemaHealthAiAnalysisProvider> logger)
        {
            this.chatService = chatService;
            this.config = config.Value;
            this.logger = logger;
        }

        public string Alias => GodModeAiAnalysisService.SchemaHealthProviderAlias;

        public string Name => "AI Schema Health";

        public async Task<GodModeAnalysisResult> AnalyzeAsync(GodModeAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.System, """
                        You are an expert Umbraco CMS technical reviewer helping developers understand schema health.
                        Return only data that conforms to the supplied structured output schema.
                        Prioritise concrete risks over generic advice. Prefer findings that list specific affected entities.
                        Use affectedEntities for every finding when the snapshot contains the relevant item names or aliases.
                        Return at most 5 findings and at most 8 affectedEntities per finding.
                        If more than 8 entities are affected, include the 8 most useful examples and mention the total in detail.
                        If the snapshot is healthy, say so and return an empty findings array.
                        """),
                    new(ChatRole.User, BuildPrompt(request))
                };

                var response = await chatService.GetChatResponseAsync(
                    builder =>
                    {
                        builder.WithAlias("diplo-godmode-ai");
                        builder.WithName("Diplo GodMode AI");

                        var options = BuildChatOptions();
                        if (options is not null)
                        {
                            builder.WithChatOptions(options);
                        }

                        builder.WithOutputSchema(AIOutputSchema.FromType<AiAnalysisPayload>());

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
                logger.LogError(ex, "AI schema health analysis failed.");
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = "AI schema health analysis failed.",
                    Findings =
                    [
                        new GodModeAnalysisFinding
                        {
                            Severity = "High",
                            Score = 80,
                            Category = "AI Configuration",
                            Title = "Schema analysis could not be completed",
                            Detail = ex.Message,
                            EntityType = "AI Provider",
                            EntityName = "Umbraco.AI",
                            EntityAlias = string.Empty,
                            Recommendation = "Check that Umbraco.AI has a configured provider and that the selected chat profile can be used from the backoffice."
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
            var snapshot = ToCompactSnapshot(request.Snapshot);
            return $"""
                Task:
                {request.Prompt}

                Use issueCandidates as the main evidence. Do not restate the snapshot. Merge similar candidates into a single finding where useful.

                Snapshot JSON:
                {JsonSerializer.Serialize(snapshot, JsonOptions)}
                """;
        }

        private static object ToCompactSnapshot(GodModeSnapshot snapshot)
        {
            var contentTypes = snapshot.ContentTypes.ToList();
            var dataTypes = snapshot.DataTypes.ToList();
            var references = snapshot.ReferenceGraph.ToList();
            var templates = snapshot.Templates.ToList();
            var healthFindings = snapshot.HealthRiskFindings.ToList();
            var driftFindings = snapshot.ConfigurationDriftFindings.ToList();
            var orphanedTags = snapshot.OrphanedTags.ToList();
            var orphanedMedia = snapshot.OrphanedMedia.ToList();
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
            var duplicateDataTypes = dataTypes
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name)
                .Where(x => x.Count() > 1)
                .ToList();
            var missingBlockReferences = references.Where(x => x.Relation == "configures missing block type").ToList();
            var unassignedTemplates = templates
                .Where(x => !referencedTemplateKeys.Contains(x.Udi.ToString()) && !templatesReferencedByTemplates.Contains(x.Udi.ToString()))
                .ToList();

            return new
            {
                generatedUtc = snapshot.GeneratedUtc,
                totals = new
                {
                    contentTypes = contentTypes.Count,
                    documentTypes = contentTypes.Count(x => !x.IsElement),
                    elementTypes = contentTypes.Count(x => x.IsElement),
                    dataTypes = dataTypes.Count,
                    templates = templates.Count,
                    references = references.Count,
                    existingHealthFindings = healthFindings.Count,
                    configurationDriftFindings = driftFindings.Count,
                    orphanedTags = orphanedTags.Count,
                    orphanedMedia = orphanedMedia.Count
                },
                issueCandidates = new
                {
                    documentTypesWithNoProperties = ToCandidateSet(noPropertyTypes, ToAffectedEntity),
                    documentTypesWithNoAllowedTemplates = ToCandidateSet(noTemplateTypes, ToAffectedEntity),
                    unusedDataTypes = ToCandidateSet(unusedDataTypes, ToAffectedEntity),
                    duplicateDataTypeNames = new
                    {
                        count = duplicateDataTypes.Count,
                        items = duplicateDataTypes.Take(10).Select(group => new
                        {
                            name = group.Key,
                            count = group.Count(),
                            affectedEntities = group.Take(8).Select(ToAffectedEntity)
                        })
                    },
                    missingBlockReferences = new
                    {
                        count = missingBlockReferences.Count,
                        items = missingBlockReferences.Take(MaxIssueCandidateItems).Select(edge => new
                        {
                            source = ToAffectedEntity(edge.SourceType, edge.SourceName, edge.SourceAlias, edge.SourceKey, edge.Context),
                            missingTarget = ToAffectedEntity(edge.TargetType, edge.TargetName, edge.TargetAlias, edge.TargetKey, edge.Relation)
                        })
                    },
                    unassignedTemplates = ToCandidateSet(unassignedTemplates, ToAffectedEntity),
                    orphanedTags = ToCandidateSet(orphanedTags, x => new
                    {
                        entityType = "Tag",
                        name = x.Text,
                        alias = x.Group,
                        key = x.Id.ToString(),
                        context = x.Culture
                    }),
                    orphanedMedia = ToCandidateSet(orphanedMedia, x => new
                    {
                        entityType = "Media",
                        name = x.Name,
                        alias = x.Path,
                        key = x.Udi.ToString(),
                        context = x.Id.ToString()
                    })
                },
                schemaShape = new
                {
                    rootDocumentTypeCount = contentTypes.Count(x => !x.IsElement && x.AllowedAtRoot),
                    documentTypesWithCompositions = contentTypes.Count(x => !x.IsElement && (x.Compositions?.Any() ?? false)),
                    elementTypesWithCompositions = contentTypes.Count(x => x.IsElement && (x.Compositions?.Any() ?? false)),
                    templatesWithPartials = templates.Count(x => x.Partials?.Any() ?? false),
                    templatesWithViewComponents = templates.Count(x => x.ViewComponents?.Any() ?? false)
                },
                existingFindings = healthFindings.Take(MaxSignalItems).Select(x => new
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
                configurationDrift = driftFindings.Take(MaxSignalItems).Select(x => new
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
            };
        }

        private static object ToCandidateSet<T>(IReadOnlyCollection<T> items, Func<T, object> map)
            => new
            {
                count = items.Count,
                items = items.Take(MaxIssueCandidateItems).Select(map)
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
            var text = response.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = "The AI provider returned an empty response."
                };
            }

            if (response.FinishReason == ChatFinishReason.Length)
            {
                return CreateTruncatedResponseResult(response);
            }

            try
            {
                var json = ExtractJson(text);
                var payload = JsonSerializer.Deserialize<AiAnalysisPayload>(json, JsonOptions);
                return new GodModeAnalysisResult
                {
                    ProviderAlias = Alias,
                    ProviderName = Name,
                    Summary = payload?.Summary ?? "AI schema health analysis completed.",
                    Findings = payload?.Findings.Select(ToFinding).ToList() ?? [],
                    SuggestedNextSteps = payload?.SuggestedNextSteps ?? []
                };
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "AI schema health analysis returned non-JSON content.");
                return CreateInvalidJsonResponseResult(response, ex);
            }
        }

        private GodModeAnalysisResult CreateTruncatedResponseResult(ChatResponse response)
            => new()
            {
                ProviderAlias = Alias,
                ProviderName = Name,
                Summary = "The AI response was cut off before the structured JSON report was complete.",
                Findings =
                [
                    new GodModeAnalysisFinding
                    {
                        Severity = "Medium",
                        Score = 50,
                        Category = "AI Configuration",
                        Title = "Schema analysis response was truncated",
                        Detail = $"The provider stopped because it reached the output token limit. God Mode AI max output override: {config.MaxOutputTokens?.ToString() ?? "not set; using the Umbraco.AI profile/provider setting"}. {UsageText(response)}",
                        EntityType = "AI Provider",
                        EntityName = response.ModelId ?? "Configured chat model",
                        EntityAlias = config.ChatProfileAlias ?? string.Empty,
                        Recommendation = "If this still happens with the profile set to 5000, reduce the number of requested findings or switch to a model with stronger structured-output behaviour."
                    }
                ],
                SuggestedNextSteps =
                [
                    "Remove Diplo:GodMode:AI:MaxOutputTokens to use the Umbraco.AI profile value.",
                    "Keep temperature low, around 0.1, for structured output."
                ]
            };

        private GodModeAnalysisResult CreateInvalidJsonResponseResult(ChatResponse response, JsonException exception)
            => new()
            {
                ProviderAlias = Alias,
                ProviderName = Name,
                Summary = "The AI provider did not return parseable structured JSON.",
                Findings =
                [
                    new GodModeAnalysisFinding
                    {
                        Severity = "Medium",
                        Score = 50,
                        Category = "AI Provider",
                        Title = "Structured JSON was not returned",
                        Detail = $"The schema request was sent, but the returned text could not be parsed as JSON. Parser error: {exception.Message}",
                        EntityType = "AI Provider",
                        EntityName = response.ModelId ?? "Configured chat model",
                        EntityAlias = config.ChatProfileAlias ?? string.Empty,
                        Recommendation = "Check that the selected model/provider supports structured JSON schema responses, keep the profile system prompt from requesting prose, and retry with a low temperature."
                    }
                ],
                SuggestedNextSteps =
                [
                    "Use a model/provider that supports JSON schema structured output.",
                    "Set the Umbraco.AI profile temperature low, around 0.1.",
                    "Remove any profile-level system prompt that asks for markdown or explanatory prose."
                ]
            };

        private static string UsageText(ChatResponse response)
        {
            var usage = response.Usage;

            if (usage is null)
            {
                return string.Empty;
            }

            return $"Token usage reported by provider: input {usage.InputTokenCount?.ToString() ?? "unknown"}, output {usage.OutputTokenCount?.ToString() ?? "unknown"}, total {usage.TotalTokenCount?.ToString() ?? "unknown"}.";
        }

        private static string ExtractJson(string text)
        {
            var trimmed = text.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
        }

        private static GodModeAnalysisFinding ToFinding(AiAnalysisFindingPayload finding)
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

        private sealed class AiAnalysisPayload
        {
            public string Summary { get; set; } = string.Empty;

            public List<AiAnalysisFindingPayload> Findings { get; set; } = [];

            public List<string> SuggestedNextSteps { get; set; } = [];
        }

        private sealed class AiAnalysisFindingPayload
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
