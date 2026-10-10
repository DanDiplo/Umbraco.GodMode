using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Diplo.GodMode.Services;

public sealed class GodModeReleaseService
{
    private readonly NuGetReleaseChecker checker;

    public GodModeReleaseService(IHttpClientFactory clientFactory, ILogger<GodModeReleaseService> logger,
        IOptionsMonitor<GodModeConfig> config)
        : this(clientFactory, GodModeVersionHelper.GetVersion(), logger, TimeProvider.System,
            isEnabled: () => config.CurrentValue.EnableReleaseCheck)
    {
    }

    internal GodModeReleaseService(IHttpClientFactory clientFactory, string currentVersion,
        ILogger<GodModeReleaseService> logger, TimeProvider clock, TimeSpan? lookupTimeout = null,
        Func<bool>? isEnabled = null)
    {
        checker = new NuGetReleaseChecker(clientFactory, "Diplo.GodMode", currentVersion, logger, clock,
            lookupTimeout: lookupTimeout, isEnabled: isEnabled);
    }

    public Task<PackageReleaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => checker.GetStatusAsync(cancellationToken);
}
