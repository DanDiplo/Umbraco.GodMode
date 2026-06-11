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
