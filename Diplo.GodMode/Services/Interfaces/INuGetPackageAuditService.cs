using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces;

public interface INuGetPackageAuditService
{
    Task<NuGetPackageAuditResult> AuditRuntimePackagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}
