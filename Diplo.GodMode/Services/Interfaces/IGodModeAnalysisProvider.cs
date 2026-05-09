using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    public interface IGodModeAnalysisProvider
    {
        string Alias { get; }

        string Name { get; }

        Task<GodModeAnalysisResult> AnalyzeAsync(GodModeAnalysisRequest request, CancellationToken cancellationToken = default);
    }
}
