using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;

namespace Diplo.GodMode.Services;

public sealed class GodModeReleaseService
{
    private readonly NuGetReleaseChecker checker;

    public GodModeReleaseService(IHttpClientFactory clientFactory, ILogger<GodModeReleaseService> logger)
        : this(clientFactory, GodModeVersionHelper.GetVersion(), logger, TimeProvider.System)
    {
    }

    internal GodModeReleaseService(IHttpClientFactory clientFactory, string currentVersion,
        ILogger<GodModeReleaseService> logger, TimeProvider clock, TimeSpan? lookupTimeout = null)
    {
        checker = new NuGetReleaseChecker(clientFactory, "Diplo.GodMode", currentVersion, logger, clock,
            lookupTimeout: lookupTimeout);
    }

    public Task<PackageReleaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => checker.GetStatusAsync(cancellationToken);
}
