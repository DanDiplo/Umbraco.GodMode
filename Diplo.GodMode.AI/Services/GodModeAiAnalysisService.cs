using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;

namespace Diplo.GodMode.AI.Services
{
    public class GodModeAiAnalysisService : IGodModeAiAnalysisService
    {
        public const string SchemaHealthProviderAlias = "schema-health-ai";

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
        {
            var provider = providers.FirstOrDefault(x => x.Alias == SchemaHealthProviderAlias);
            if (provider is null)
            {
                return new GodModeAnalysisResult
                {
                    ProviderAlias = SchemaHealthProviderAlias,
                    ProviderName = "AI Schema Health",
                    Summary = "No schema health analysis provider is registered."
                };
            }

            var snapshot = await snapshotService.CreateSchemaHealthSnapshotAsync(cancellationToken);
            var request = new GodModeAnalysisRequest
            {
                ProviderAlias = provider.Alias,
                Prompt = "Analyse this Umbraco schema health snapshot and return concise developer-focused findings.",
                Snapshot = snapshot
            };

            return await provider.AnalyzeAsync(request, cancellationToken);
        }
    }
}
