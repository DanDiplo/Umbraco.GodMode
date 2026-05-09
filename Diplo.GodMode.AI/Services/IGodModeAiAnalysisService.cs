using Diplo.GodMode.Models;

namespace Diplo.GodMode.AI.Services
{
    public interface IGodModeAiAnalysisService
    {
        Task<GodModeAnalysisResult> AnalyzeSchemaHealthAsync(CancellationToken cancellationToken = default);

        Task<GodModeAnalysisResult> CreateFixPlanAsync(CancellationToken cancellationToken = default);
    }
}
