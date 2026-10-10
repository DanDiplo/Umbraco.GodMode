namespace Diplo.GodMode.Models;

public sealed record UmbracoReleaseStatus
{
    public string CurrentVersion { get; init; } = string.Empty;
    public string Status { get; init; } = "unavailable";
    public string? LatestVersion { get; init; }
    public int? ReleasesBehind { get; init; }
    public DateTimeOffset? CheckedAt { get; init; }
    public DateTimeOffset? LastAttemptAt { get; init; }
    public bool IsStale { get; init; }
    public string SourceUrl { get; init; } = "https://www.nuget.org/packages/Umbraco.Cms";
    public string ReleaseNotesUrl { get; init; } = "https://releases.umbraco.com/all-releases";
}
