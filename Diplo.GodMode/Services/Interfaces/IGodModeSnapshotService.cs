using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    public interface IGodModeSnapshotService
    {
        Task<GodModeSnapshot> CreateSchemaHealthSnapshotAsync(CancellationToken cancellationToken = default);
    }
}
