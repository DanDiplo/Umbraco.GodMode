using System.Text.Json;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diplo.GodMode.Services;

public sealed class NuGetPackageAuditService : INuGetPackageAuditService
{
    private const string CacheKey = "Diplo.GodMode.NuGetPackageAudit.v2";
    private const string AuditSourceUrl = "https://data.nuget.org/v3/index.json";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private static readonly Regex RestoreWarningMessageRegex = new(
        @"Package '(.+)' ([^\s]+) has a known (.+) severity vulnerability, (https?://\S+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient httpClient;
    private readonly INuGetPackageInventoryService inventoryService;
    private readonly IMemoryCache memoryCache;
    private readonly ILogger<NuGetPackageAuditService> logger;

    public NuGetPackageAuditService(
        HttpClient httpClient,
        INuGetPackageInventoryService inventoryService,
        IMemoryCache memoryCache,
        ILogger<NuGetPackageAuditService> logger)
    {
        this.httpClient = httpClient;
        this.inventoryService = inventoryService;
        this.memoryCache = memoryCache;
        this.logger = logger;
    }

    public async Task<NuGetPackageAuditResult> AuditRuntimePackagesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh && memoryCache.TryGetValue(CacheKey, out NuGetPackageAuditResult cached))
        {
            return cached;
        }

        var inventory = inventoryService.GetInventory();
        var vulnerabilityIndexUrl = await GetVulnerabilityIndexUrlAsync(cancellationToken);
        var vulnerabilityDataUrls = await GetVulnerabilityDataUrlsAsync(vulnerabilityIndexUrl, cancellationToken);
        var advisoriesByPackage = new Dictionary<string, List<VulnerabilityRecord>>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in vulnerabilityDataUrls)
        {
            await LoadVulnerabilityDataAsync(url, advisoriesByPackage, cancellationToken);
        }

        var restoreWarnings = GetRestoreWarnings().ToList();
        var auditedPackages = inventory.Packages
            .Select(package => new NuGetPackageAuditInfo
            {
                Id = package.Id,
                Version = package.Version,
                Vulnerabilities = GetMatchingVulnerabilities(package, advisoriesByPackage)
            })
            .Where(package => package.Vulnerabilities.Any())
            .OrderBy(package => package.Id)
            .ThenBy(package => package.Version)
            .ToList();

        var result = new NuGetPackageAuditResult
        {
            AuditedAt = DateTimeOffset.UtcNow,
            SourceUrl = AuditSourceUrl,
            PackageCount = inventory.PackageCount,
            VulnerablePackageCount = auditedPackages.Count,
            AdvisoryCount = auditedPackages.Sum(package => package.Vulnerabilities.Count()),
            RestoreWarningCount = restoreWarnings.Count,
            Packages = auditedPackages,
            RestoreWarnings = restoreWarnings
        };

        memoryCache.Set(CacheKey, result, CacheDuration);
        return result;
    }

    private async Task<string> GetVulnerabilityIndexUrlAsync(CancellationToken cancellationToken)
    {
        using var document = await GetJsonDocumentAsync(AuditSourceUrl, cancellationToken);
        var resources = document.RootElement.GetProperty("resources");

        foreach (var resource in resources.EnumerateArray())
        {
            var type = resource.GetProperty("@type").GetString() ?? string.Empty;

            if (type.StartsWith("VulnerabilityInfo/", StringComparison.OrdinalIgnoreCase))
            {
                return resource.GetProperty("@id").GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("NuGet audit source did not advertise a VulnerabilityInfo resource.");
    }

    private async Task<IEnumerable<string>> GetVulnerabilityDataUrlsAsync(string vulnerabilityIndexUrl, CancellationToken cancellationToken)
    {
        using var document = await GetJsonDocumentAsync(vulnerabilityIndexUrl, cancellationToken);

        return document.RootElement
            .EnumerateArray()
            .Select(resource => resource.GetProperty("@id").GetString())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Cast<string>()
            .ToList();
    }

    private async Task LoadVulnerabilityDataAsync(string url, Dictionary<string, List<VulnerabilityRecord>> advisoriesByPackage, CancellationToken cancellationToken)
    {
        using var document = await GetJsonDocumentAsync(url, cancellationToken);

        foreach (var packageProperty in document.RootElement.EnumerateObject())
        {
            if (!advisoriesByPackage.TryGetValue(packageProperty.Name, out var advisories))
            {
                advisories = [];
                advisoriesByPackage[packageProperty.Name] = advisories;
            }

            if (packageProperty.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var advisory in packageProperty.Value.EnumerateArray())
            {
                try
                {
                    advisories.Add(new VulnerabilityRecord(
                        advisory.GetProperty("url").GetString() ?? string.Empty,
                        advisory.GetProperty("severity").GetInt32(),
                        advisory.GetProperty("versions").GetString() ?? string.Empty));
                }
                catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or JsonException)
                {
                    logger.LogDebug(ex, "Skipping malformed NuGet vulnerability record for package {PackageId}.", packageProperty.Name);
                }
            }
        }
    }

    private async Task<JsonDocument> GetJsonDocumentAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static IEnumerable<NuGetPackageVulnerability> GetMatchingVulnerabilities(NuGetPackageInfo package, Dictionary<string, List<VulnerabilityRecord>> advisoriesByPackage)
    {
        if (!advisoriesByPackage.TryGetValue(package.Id, out var advisories))
        {
            return [];
        }

        return advisories
            .Where(advisory => VersionRangeContains(advisory.VersionRange, package.Version))
            .Select(advisory => new NuGetPackageVulnerability
            {
                Severity = SeverityName(advisory.Severity),
                SeverityLevel = advisory.Severity,
                AdvisoryUrl = advisory.Url,
                AffectedVersions = advisory.VersionRange
            })
            .OrderByDescending(advisory => advisory.SeverityLevel)
            .ThenBy(advisory => advisory.AdvisoryUrl)
            .ToList();
    }

    private IEnumerable<NuGetPackageRestoreWarning> GetRestoreWarnings()
    {
        foreach (var path in GetProjectAssetsPaths())
        {
            NuGetPackageRestoreWarning[] warnings;

            try
            {
                warnings = ReadRestoreWarnings(path).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                logger.LogDebug(ex, "Could not read NuGet restore warnings from {ProjectAssetsPath}.", path);
                continue;
            }

            foreach (var warning in warnings)
            {
                yield return warning;
            }
        }
    }

    private static IEnumerable<string> GetProjectAssetsPaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in GetCandidateProjectDirectories())
        {
            var path = Path.Combine(directory, "obj", "project.assets.json");
            if (File.Exists(path))
            {
                paths.Add(path);
            }
        }

        return paths.OrderBy(path => path);
    }

    private static IEnumerable<string> GetCandidateProjectDirectories()
    {
        foreach (var directory in WalkAncestorDirectories(AppContext.BaseDirectory))
        {
            yield return directory;

            foreach (var childDirectory in GetChildProjectDirectories(directory))
            {
                yield return childDirectory;
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic))
        {
            string? location;

            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(location);
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            foreach (var ancestor in WalkAncestorDirectories(directory))
            {
                yield return ancestor;

                foreach (var childDirectory in GetChildProjectDirectories(ancestor))
                {
                    yield return childDirectory;
                }
            }
        }
    }

    private static IEnumerable<string> GetChildProjectDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        IEnumerable<string> childDirectories;

        try
        {
            childDirectories = Directory.EnumerateDirectories(directory).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var childDirectory in childDirectories)
        {
            var assetsPath = Path.Combine(childDirectory, "obj", "project.assets.json");
            if (File.Exists(assetsPath))
            {
                yield return childDirectory;
            }
        }
    }

    private static IEnumerable<string> WalkAncestorDirectories(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        for (var i = 0; i < 6 && directory is not null; i++)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private static IEnumerable<NuGetPackageRestoreWarning> ReadRestoreWarnings(string projectAssetsPath)
    {
        using var stream = File.OpenRead(projectAssetsPath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("logs", out var logs) || logs.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        var project = new DirectoryInfo(Path.GetDirectoryName(projectAssetsPath) ?? string.Empty).Parent?.Name ?? string.Empty;

        foreach (var log in logs.EnumerateArray())
        {
            var code = log.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? string.Empty : string.Empty;
            if (!code.StartsWith("NU190", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var message = log.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? string.Empty : string.Empty;
            var libraryId = log.TryGetProperty("libraryId", out var libraryElement) ? libraryElement.GetString() ?? string.Empty : string.Empty;
            var match = RestoreWarningMessageRegex.Match(message);
            var packageId = match.Success ? match.Groups[1].Value : libraryId;
            var severity = match.Success ? match.Groups[3].Value : string.Empty;

            yield return new NuGetPackageRestoreWarning
            {
                Project = project,
                Code = code,
                PackageId = packageId,
                Version = match.Success ? match.Groups[2].Value : string.Empty,
                Severity = string.IsNullOrWhiteSpace(severity) ? SeverityName(SeverityLevelFromWarningCode(code)) : severity,
                SeverityLevel = SeverityLevelFromWarningCode(code),
                AdvisoryUrl = match.Success ? match.Groups[4].Value : string.Empty,
                Message = message
            };
        }
    }

    private static int SeverityLevelFromWarningCode(string code)
        => code.ToUpperInvariant() switch
        {
            "NU1901" => 0,
            "NU1902" => 1,
            "NU1903" => 2,
            "NU1904" => 3,
            _ => -1
        };

    private static string SeverityName(int severity)
        => severity switch
        {
            0 => "Low",
            1 => "Moderate",
            2 => "High",
            3 => "Critical",
            _ => "Unknown"
        };

    private static bool VersionRangeContains(string range, string version)
    {
        if (string.IsNullOrWhiteSpace(range) || string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        range = range.Trim();

        if (range.Length < 2 || !range.Contains(','))
        {
            return CompareVersions(version, range.Trim('[', ']', '(', ')')) == 0;
        }

        var minInclusive = range[0] == '[';
        var maxInclusive = range[^1] == ']';
        var bounds = range[1..^1].Split(',', 2);
        var min = bounds[0].Trim();
        var max = bounds.Length > 1 ? bounds[1].Trim() : string.Empty;

        if (!string.IsNullOrEmpty(min))
        {
            var minCompare = CompareVersions(version, min);
            if (minCompare < 0 || (minCompare == 0 && !minInclusive))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(max))
        {
            var maxCompare = CompareVersions(version, max);
            if (maxCompare > 0 || (maxCompare == 0 && !maxInclusive))
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareVersions(string left, string right)
    {
        var leftVersion = ParsedVersion.Parse(left);
        var rightVersion = ParsedVersion.Parse(right);

        for (var i = 0; i < Math.Max(leftVersion.Parts.Length, rightVersion.Parts.Length); i++)
        {
            var leftPart = i < leftVersion.Parts.Length ? leftVersion.Parts[i] : 0;
            var rightPart = i < rightVersion.Parts.Length ? rightVersion.Parts[i] : 0;

            if (leftPart != rightPart)
            {
                return leftPart.CompareTo(rightPart);
            }
        }

        if (leftVersion.Prerelease == rightVersion.Prerelease)
        {
            return 0;
        }

        if (string.IsNullOrEmpty(leftVersion.Prerelease))
        {
            return 1;
        }

        if (string.IsNullOrEmpty(rightVersion.Prerelease))
        {
            return -1;
        }

        return string.Compare(leftVersion.Prerelease, rightVersion.Prerelease, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record VulnerabilityRecord(string Url, int Severity, string VersionRange);

    private sealed record ParsedVersion(int[] Parts, string Prerelease)
    {
        public static ParsedVersion Parse(string version)
        {
            var withoutMetadata = version.Split('+', 2)[0];
            var versionParts = withoutMetadata.Split('-', 2);
            var parts = versionParts[0]
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => int.TryParse(part, out var value) ? value : 0)
                .ToArray();
            var prerelease = versionParts.Length > 1 ? versionParts[1] : string.Empty;

            return new ParsedVersion(parts, prerelease);
        }
    }
}
