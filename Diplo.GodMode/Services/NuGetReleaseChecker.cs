using System.Text.Json;
using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;

namespace Diplo.GodMode.Services;

/// <summary>
/// Shared same-major release comparison, caching, and single-flight lookup for one package.
/// Each owning singleton service keeps its own checker so package results and failures stay independent.
/// </summary>
internal sealed class NuGetReleaseChecker
{
    internal const string HttpClientName = "Diplo.GodMode.PackageReleases";
    private const string ServiceIndexUrl = "https://api.nuget.org/v3/index.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(10);
    private readonly object gate = new();
    private readonly IHttpClientFactory clientFactory;
    private readonly ILogger logger;
    private readonly string packageId;
    private readonly Func<string, string> releaseNotesUrl;
    private readonly TimeProvider clock;
    private readonly TimeSpan lookupTimeout;
    private readonly Func<bool> isEnabled;
    private readonly string currentVersion;
    private readonly Version? currentNumber;
    private readonly bool isPrerelease;
    private PackageReleaseStatus cached;
    private DateTimeOffset nextCheckAt;
    private Task<PackageReleaseStatus>? refreshTask;

    internal NuGetReleaseChecker(IHttpClientFactory clientFactory, string packageId, string currentVersion,
        ILogger logger, TimeProvider clock, Func<string, string>? releaseNotesUrl = null,
        string? fallbackNotesUrl = null, TimeSpan? lookupTimeout = null, Func<bool>? isEnabled = null)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.packageId = packageId;
        var sourceUrl = $"https://www.nuget.org/packages/{packageId}";
        this.releaseNotesUrl = releaseNotesUrl ?? (version => $"{sourceUrl}/{version}");
        this.clock = clock;
        this.lookupTimeout = lookupTimeout ?? TimeSpan.FromSeconds(3);
        this.isEnabled = isEnabled ?? (() => true);
        this.currentVersion = currentVersion;
        // An unparseable running version can never be compared, so it stays unavailable rather than failing construction.
        currentNumber = TryParseNumber(currentVersion);
        if (currentNumber is null)
            logger.LogDebug("Release checks for {PackageId} are unavailable because the running version '{Version}' could not be parsed.", packageId, currentVersion);
        isPrerelease = currentVersion.Split('+', 2)[0].Contains('-');
        cached = new PackageReleaseStatus
        {
            CurrentVersion = currentVersion,
            SourceUrl = sourceUrl,
            ReleaseNotesUrl = fallbackNotesUrl ?? sourceUrl
        };
    }

    public async Task<PackageReleaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!isEnabled()) return cached with { Status = "disabled", LatestVersion = null, ReleasesBehind = null, IsStale = false };
        if (currentNumber is null) return cached;
        Task<PackageReleaseStatus> pending;
        lock (gate)
        {
            if (clock.GetUtcNow() < nextCheckAt) return cached;
            if (refreshTask is null || refreshTask.IsCompleted) refreshTask = RefreshAsync();
            pending = refreshTask;
        }

        // Cancelling a browser request must not cancel the lookup shared by other callers.
        return await pending.WaitAsync(cancellationToken);
    }

    private async Task<PackageReleaseStatus> RefreshAsync()
    {
        var attemptedAt = clock.GetUtcNow();
        try
        {
            // One deadline for discovery, all selected pages, and JSON parsing.
            using var deadline = new CancellationTokenSource(lookupTimeout);
            using var client = clientFactory.CreateClient(HttpClientName);
            var versions = await LoadVersionsAsync(client, deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            if (versions.Count == 0) throw new JsonException("No listed stable releases found for the running major.");
            var latest = versions.Max()!;
            var running = currentNumber!;
            var behind = versions.Count(version => version > running);
            var result = new PackageReleaseStatus
            {
                CurrentVersion = currentVersion,
                LatestVersion = latest.ToString(3),
                ReleasesBehind = isPrerelease ? null : behind,
                Status = isPrerelease ? "prerelease" : running > latest ? "newerThanFeed" : behind > 0 ? "behind" : "upToDate",
                CheckedAt = clock.GetUtcNow(),
                LastAttemptAt = attemptedAt,
                SourceUrl = cached.SourceUrl,
                ReleaseNotesUrl = releaseNotesUrl(latest.ToString(3))
            };
            lock (gate)
            {
                cached = result;
                nextCheckAt = clock.GetUtcNow() + CacheDuration;
                return cached;
            }
        }
        catch (Exception ex)
        {
            // The check is informational only: any failure, expected or not, is cached and backed off rather than surfaced.
            logger.LogDebug(ex, "Could not check releases for {PackageId} on NuGet; retrying after the backoff interval.", packageId);
            lock (gate)
            {
                cached = cached with { IsStale = cached.CheckedAt.HasValue, LastAttemptAt = attemptedAt };
                nextCheckAt = clock.GetUtcNow() + FailureBackoff;
                return cached;
            }
        }
    }

    private async Task<HashSet<Version>> LoadVersionsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var index = await GetJsonAsync(client, ServiceIndexUrl, cancellationToken);
        var registrationBase = index.RootElement.GetProperty("resources").EnumerateArray()
            .First(resource => resource.GetProperty("@type").GetString() == "RegistrationsBaseUrl/3.6.0")
            .GetProperty("@id").GetString() ?? throw new JsonException("Missing NuGet registration URL.");
        using var registration = await GetJsonAsync(client, registrationBase.TrimEnd('/') + $"/{packageId.ToLowerInvariant()}/index.json", cancellationToken);
        var versions = new HashSet<Version>();
        foreach (var page in registration.RootElement.GetProperty("items").EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lower = ParseNumber(page.GetProperty("lower").GetString()!);
            var upper = ParseNumber(page.GetProperty("upper").GetString()!);
            if (lower.Major > currentNumber!.Major || upper.Major < currentNumber.Major) continue;

            if (page.TryGetProperty("items", out var items)) AddVersions(items, versions);
            else
            {
                using var details = await GetJsonAsync(client, page.GetProperty("@id").GetString()!, cancellationToken);
                AddVersions(details.RootElement.GetProperty("items"), versions);
            }
        }
        return versions;
    }

    private void AddVersions(JsonElement items, HashSet<Version> versions)
    {
        foreach (var item in items.EnumerateArray())
        {
            var entry = item.GetProperty("catalogEntry");
            if (entry.TryGetProperty("listed", out var listed) && !listed.GetBoolean()) continue;
            var text = entry.GetProperty("version").GetString() ?? throw new JsonException("Missing package version.");
            if (text.Split('+', 2)[0].Contains('-')) continue;
            var number = ParseNumber(text);
            if (number.Major == currentNumber!.Major) versions.Add(number);
        }
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        // Follow only NuGet links, never arbitrary hosts returned by a remote document.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host != "api.nuget.org" || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo))
            throw new JsonException("Unexpected NuGet metadata URL.");
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static Version ParseNumber(string text)
        => TryParseNumber(text) ?? throw new FormatException("Invalid package release version.");

    private static Version? TryParseNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var numeric = text.Split('+', 2)[0].Split('-', 2)[0];
        if (!Version.TryParse(numeric, out var version) || version.Build < 0 || version.Revision > 0) return null;
        return new Version(version.Major, version.Minor, version.Build);
    }
}
