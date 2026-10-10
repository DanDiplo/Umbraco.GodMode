using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services;

public sealed class UmbracoReleaseService
{
    private readonly NuGetReleaseChecker checker;

    public UmbracoReleaseService(IHttpClientFactory clientFactory, IUmbracoVersion version,
        ILogger<UmbracoReleaseService> logger, IOptionsMonitor<GodModeConfig> config)
        : this(clientFactory, version.SemanticVersion.ToSemanticString(), logger, TimeProvider.System,
            isEnabled: () => config.CurrentValue.EnableReleaseCheck)
    {
    }

    internal UmbracoReleaseService(IHttpClientFactory clientFactory, string currentVersion,
        ILogger<UmbracoReleaseService> logger, TimeProvider clock, TimeSpan? lookupTimeout = null,
        Func<bool>? isEnabled = null)
    {
        checker = new NuGetReleaseChecker(clientFactory, "Umbraco.Cms", currentVersion, logger, clock,
            latest => $"https://releases.umbraco.com/release/umbraco/Umbraco-CMS/{latest}",
            "https://releases.umbraco.com/all-releases", lookupTimeout, isEnabled);
    }

    public Task<PackageReleaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => checker.GetStatusAsync(cancellationToken);
}
