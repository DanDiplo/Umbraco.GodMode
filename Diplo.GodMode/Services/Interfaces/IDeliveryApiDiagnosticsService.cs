using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces;

public interface IDeliveryApiDiagnosticsService
{
    Task<DeliveryApiDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}
