using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;

namespace Diplo.GodMode.AI.Services
{
    public class GodModeAiExplainService : IGodModeAiExplainService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false
        };

        private readonly IAIChatService chatService;
        private readonly GodModeAiConfig config;
        private readonly ILogger<GodModeAiExplainService> logger;

        public GodModeAiExplainService(
            IAIChatService chatService,
            IOptions<GodModeAiConfig> config,
            ILogger<GodModeAiExplainService> logger)
        {
            this.chatService = chatService;
            this.config = config.Value;
            this.logger = logger;
        }

        public async Task<GodModeAiExplainResponse> ExplainAsync(GodModeAiExplainRequest request, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, """
                    You explain Umbraco developer diagnostics clearly and concisely.
                    Return only data that conforms to the supplied structured output schema.
                    Use the supplied row data only. Do not invent hidden configuration or project details.
                    Keep the response practical: what it is, why it matters, likely risk, and next steps.
                    Treat redacted or empty values carefully and say when there is not enough context.
                    """),
                new(ChatRole.User, $"""
                    Explain this God Mode item.

                    Subject type:
                    {request.SubjectType}

                    Title:
                    {request.Title}

                    Row data JSON:
                    {JsonSerializer.Serialize(request.Data, JsonOptions)}

                    Additional context JSON:
                    {SerializeContext(request.Context)}
                    """)
            };

            try
            {
                var response = await chatService.GetChatResponseAsync(
                    builder =>
                    {
                        builder.WithAlias("diplo-godmode-ai-explain");
                        builder.WithName("Diplo GodMode AI Explain");

                        var options = BuildChatOptions();
                        if (options is not null)
                        {
                            builder.WithChatOptions(options);
                        }

                        builder.WithOutputSchema(AIOutputSchema.FromType<GodModeAiExplainResponse>());

                        if (!string.IsNullOrWhiteSpace(config.ChatProfileAlias))
                        {
                            builder.WithProfile(config.ChatProfileAlias);
                        }
                    },
                    messages,
                    cancellationToken);

                var json = ExtractJson(response.Text);
                var result = JsonSerializer.Deserialize<GodModeAiExplainResponse>(json, JsonOptions);

                return result ?? new GodModeAiExplainResponse
                {
                    Summary = "No explanation was returned.",
                    SuggestedNextSteps = ["Try again or choose a more specific item."]
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "AI explain request failed for {SubjectType} {Title}.", request.SubjectType, request.Title);
                return new GodModeAiExplainResponse
                {
                    Summary = "The AI explanation could not be created.",
                    Risk = ex.Message,
                    SuggestedNextSteps = ["Check the Umbraco.AI profile configuration and retry."]
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

        private static string SerializeContext(JsonElement? context)
            => context.HasValue && context.Value.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null
                ? JsonSerializer.Serialize(context.Value, JsonOptions)
                : "{}";

        private static string ExtractJson(string text)
        {
            var trimmed = text.Trim();
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
        }
    }
}
