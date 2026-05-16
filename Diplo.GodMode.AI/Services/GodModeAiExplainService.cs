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
                    You explain Umbraco developer diagnostics clearly and concisely for an experienced Umbraco developer.
                    Return only data that conforms to the supplied structured output schema.
                    Use both the supplied row data JSON and the additional context JSON. The additional context is usually more important than the row data.
                    Do not invent hidden configuration or project details.
                    Prefer concrete facts from the JSON over generic CMS advice.
                    If a count or collection is present, mention what it shows. For example: audit trail entries, populated properties, published/edited state, relations, used-by references, and uses references.
                    If a relevant collection is empty, say that explicitly when it affects risk or next steps.
                    For content and media items, ground the answer in the detail object if it exists.
                    Keep the response practical: what it is, why it matters, likely risk, and next steps.
                    Put three to six concrete facts in observedDetails. Each fact must be directly supported by the JSON.
                    Treat redacted or empty values carefully and say when there is not enough context.

                    For subjectType "Umbraco Log Event", adapt to the log level and payload:
                    - Do not assume every log event is an error. Information, Debug, and Verbose entries are often normal operational telemetry.
                    - Treat a log event as an exception/error only when it has an exception value, stackFrames, or level Error/Fatal. Warning may or may not be a failure; decide from the message and payload.
                    - For exception/error events, fill primaryDiagnosis with the most likely root cause in one or two precise sentences.
                    - For exception/error events, fill whereToLook with the most actionable source location from the stack trace. Prefer the first frame in application/package code over framework frames. Include file path, line number, type, and method when present.
                    - For exception/error events, fill likelyCause with why that exact source location probably produced the exception. Mention the failing exception type/message, SQL/database provider details, request path, and relevant arguments if they are present.
                    - For exception/error events, fill howToFix with concrete fix steps tied to the failing source location. Prefer code/query/config changes over generic advice.
                    - For non-error log events, fill primaryDiagnosis with what happened and whether it appears expected, noteworthy, or benign.
                    - For non-error log events, fill whereToLook only when there is a meaningful SourceContext, RequestPath, component, or configuration area to inspect. Do not invent file paths or line numbers.
                    - For non-error log events, fill likelyCause with the likely trigger or lifecycle event, not a "root cause".
                    - For non-error log events, fill howToFix with action only if action is warranted. If no action is needed, say so plainly and suggest what to monitor instead.
                    - For stack traces, separate symptoms from cause: framework and middleware frames are usually execution plumbing, not the code to fix.
                    - If the exception contains file paths and line numbers, do not omit them. If no project frame exists, say which library frame is closest and what evidence is missing.
                    - Use observedDetails sparingly for log events. Only include high-signal evidence that supports the explanation, not a restatement of timestamp, level, or message.

                    For subjectType "Umbraco Log Insight", explain the grouped pattern rather than an individual row:
                    - Fill primaryDiagnosis with what repeated issue or warning pattern the group represents.
                    - Use count, firstSeen, lastSeen, sourceContext, requestPaths, exceptionType, normalizedMessage, samples, and firstActionableFrame to assess priority.
                    - Fill whereToLook with the most useful component/source/request path or application stack frame. Include file and line only if present.
                    - Fill likelyCause with the shared cause suggested by the repeated samples, not every possible cause.
                    - Fill howToFix with practical triage/fix steps in priority order.
                    - Mention whether the pattern is noisy-but-benign, user-impacting, or likely a bug in custom/package code when the evidence supports it.
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
