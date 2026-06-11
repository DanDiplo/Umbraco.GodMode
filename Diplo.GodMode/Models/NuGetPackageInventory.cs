namespace Diplo.GodMode.Models;

public sealed class NuGetPackageInventory
{
    public string RuntimeName { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public int PackageCount { get; set; }

    public int DirectPackageCount { get; set; }

    public int LoadedPackageCount { get; set; }

    public IEnumerable<NuGetPackageInfo> Packages { get; set; } = [];
}

public sealed class NuGetPackageInfo
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsDirect { get; set; }

    public bool IsTransitive { get; set; }

    public bool IsLoaded { get; set; }

    public string NuGetUrl { get; set; } = string.Empty;

    public IEnumerable<string> DirectProjects { get; set; } = [];

    public IEnumerable<string> RequestedBy { get; set; } = [];

    public IEnumerable<string> Dependencies { get; set; } = [];

    public IEnumerable<string> RuntimeAssemblies { get; set; } = [];

    public IEnumerable<string> LoadedAssemblies { get; set; } = [];
}

public sealed class NuGetPackageAuditResult
{
    public DateTimeOffset AuditedAt { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public int PackageCount { get; set; }

    public int VulnerablePackageCount { get; set; }

    public int AdvisoryCount { get; set; }

    public int RestoreWarningCount { get; set; }

    public IEnumerable<NuGetPackageAuditInfo> Packages { get; set; } = [];

    public IEnumerable<NuGetPackageRestoreWarning> RestoreWarnings { get; set; } = [];
}

public sealed class NuGetPackageAuditInfo
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public IEnumerable<NuGetPackageVulnerability> Vulnerabilities { get; set; } = [];
}

public sealed class NuGetPackageVulnerability
{
    public string Severity { get; set; } = string.Empty;

    public int SeverityLevel { get; set; }

    public string AdvisoryUrl { get; set; } = string.Empty;

    public string AffectedVersions { get; set; } = string.Empty;
}

public sealed class NuGetPackageRestoreWarning
{
    public string Project { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string PackageId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int SeverityLevel { get; set; }

    public string AdvisoryUrl { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
