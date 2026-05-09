namespace Diplo.GodMode.AI.Services
{
    public interface IGodModeAiExplainService
    {
        Task<GodModeAiExplainResponse> ExplainAsync(GodModeAiExplainRequest request, CancellationToken cancellationToken = default);
    }
}
