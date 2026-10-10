using System.Text.Json;
using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services;

/// <summary>
/// Checks listed stable Umbraco.Cms releases only when the overview requests them.
/// Registered as a singleton: callers share both the cached result and an in-flight lookup.
/// </summary>
public sealed class UmbracoReleaseService
{
    internal const string HttpClientName = "Diplo.GodMode.UmbracoReleases";
    private const string ServiceIndexUrl = "https://api.nuget.org/v3/index.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(10);
    private readonly object gate = new();
    private readonly IHttpClientFactory clientFactory;
    private readonly ILogger<UmbracoReleaseService> logger;
    private readonly TimeProvider clock;
    private readonly TimeSpan lookupTimeout;
    private readonly string currentVersion;
    private readonly Version currentNumber;
    private readonly bool isPrerelease;
    private UmbracoReleaseStatus cached;
    private DateTimeOffset nextCheckAt;
    private Task<UmbracoReleaseStatus>? refreshTask;

    public UmbracoReleaseService(IHttpClientFactory clientFactory, IUmbracoVersion version,
        ILogger<UmbracoReleaseService> logger)
        : this(clientFactory, version.SemanticVersion.ToSemanticString(), logger, TimeProvider.System)
    {
    }

    internal UmbracoReleaseService(IHttpClientFactory clientFactory, string currentVersion,
        ILogger<UmbracoReleaseService> logger, TimeProvider clock, TimeSpan? lookupTimeout = null)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.clock = clock;
        this.lookupTimeout = lookupTimeout ?? TimeSpan.FromSeconds(3);
        this.currentVersion = currentVersion;
        currentNumber = ParseNumber(currentVersion);
        isPrerelease = currentVersion.Split('+', 2)[0].Contains('-');
        cached = new UmbracoReleaseStatus { CurrentVersion = currentVersion };
    }

    public async Task<UmbracoReleaseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Task<UmbracoReleaseStatus> pending;
        lock (gate)
        {
            if (clock.GetUtcNow() < nextCheckAt) return cached;
            if (refreshTask is null || refreshTask.IsCompleted) refreshTask = RefreshAsync();
            pending = refreshTask;
        }

        // Cancelling a browser request must not cancel the lookup shared by other callers.
        return await pending.WaitAsync(cancellationToken);
    }

    private async Task<UmbracoReleaseStatus> RefreshAsync()
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
            var behind = versions.Count(version => version > currentNumber);
            var result = new UmbracoReleaseStatus
            {
                CurrentVersion = currentVersion,
                LatestVersion = latest.ToString(3),
                ReleasesBehind = isPrerelease ? null : behind,
                Status = isPrerelease ? "prerelease" : currentNumber > latest ? "newerThanFeed" : behind > 0 ? "behind" : "upToDate",
                CheckedAt = clock.GetUtcNow(),
                LastAttemptAt = attemptedAt,
                ReleaseNotesUrl = $"https://releases.umbraco.com/release/umbraco/Umbraco-CMS/{latest.ToString(3)}"
            };
            lock (gate)
            {
                cached = result;
                nextCheckAt = clock.GetUtcNow() + CacheDuration;
                return cached;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException
            or InvalidOperationException or KeyNotFoundException or FormatException or IOException)
        {
            logger.LogDebug(ex, "Could not check Umbraco releases on NuGet; retrying after the backoff interval.");
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
        using var registration = await GetJsonAsync(client, registrationBase.TrimEnd('/') + "/umbraco.cms/index.json", cancellationToken);
        var versions = new HashSet<Version>();
        foreach (var page in registration.RootElement.GetProperty("items").EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lower = ParseNumber(page.GetProperty("lower").GetString()!);
            var upper = ParseNumber(page.GetProperty("upper").GetString()!);
            if (lower.Major > currentNumber.Major || upper.Major < currentNumber.Major) continue;

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
            if (number.Major == currentNumber.Major) versions.Add(number);
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
    {
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Missing CMS release version.");
        var numeric = text.Split('+', 2)[0].Split('-', 2)[0];
        if (!Version.TryParse(numeric, out var version) || version.Build < 0 || version.Revision > 0)
            throw new FormatException("Invalid CMS release version.");
        return new Version(version.Major, version.Minor, version.Build);
    }
}
