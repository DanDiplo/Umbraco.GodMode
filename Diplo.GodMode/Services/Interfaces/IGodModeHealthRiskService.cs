using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    public interface IGodModeHealthRiskService
    {
        Task<IEnumerable<HealthRiskFinding>> BuildHealthRiskFindingsAsync(CancellationToken cancellationToken = default);
    }
}
