using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;

namespace Diplo.GodMode.AI.Services
{
    public class GodModeAiAnalysisService : IGodModeAiAnalysisService
    {
        public const string SchemaHealthProviderAlias = "schema-health-ai";
        public const string FixPlanProviderAlias = "fix-plan-ai";

        private readonly IGodModeSnapshotService snapshotService;
        private readonly IEnumerable<IGodModeAnalysisProvider> providers;

        public GodModeAiAnalysisService(
            IGodModeSnapshotService snapshotService,
            IEnumerable<IGodModeAnalysisProvider> providers)
        {
            this.snapshotService = snapshotService;
            this.providers = providers;
        }

        public async Task<GodModeAnalysisResult> AnalyzeSchemaHealthAsync(CancellationToken cancellationToken = default)
            => await AnalyzeAsync(
                SchemaHealthProviderAlias,
                "AI Schema Health",
                "Analyse this Umbraco schema health snapshot and return concise developer-focused findings.",
                cancellationToken);

        public async Task<GodModeAnalysisResult> CreateFixPlanAsync(CancellationToken cancellationToken = default)
            => await AnalyzeAsync(
                FixPlanProviderAlias,
                "AI Fix Plan",
                "Create a prioritised remediation plan from this Umbraco schema health snapshot. Return ordered, practical fixes with impact, effort, affected entities, and a clear next action.",
                cancellationToken);

        private async Task<GodModeAnalysisResult> AnalyzeAsync(
            string providerAlias,
            string providerName,
            string prompt,
            CancellationToken cancellationToken)
        {
            var provider = providers.FirstOrDefault(x => x.Alias == providerAlias);
            if (provider is null)
            {
                return new GodModeAnalysisResult
                {
                    ProviderAlias = providerAlias,
                    ProviderName = providerName,
                    Summary = $"No {providerName} provider is registered."
                };
            }

            var snapshot = await snapshotService.CreateSchemaHealthSnapshotAsync(cancellationToken);
            var request = new GodModeAnalysisRequest
            {
                ProviderAlias = provider.Alias,
                Prompt = prompt,
                Snapshot = snapshot
            };

            return await provider.AnalyzeAsync(request, cancellationToken);
        }
    }
}
