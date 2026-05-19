namespace Diplo.GodMode.Models;

public class UtilityDiagnostics
{
    public AppInfo App { get; set; } = new();

    public IEnumerable<AssetCheck> Assets { get; set; } = [];

    public IEnumerable<FolderSizeInfo> Folders { get; set; } = [];

    public CacheStatus Cache { get; set; } = new();

    public ServerStats ServerStats { get; set; } = new();

    public IEnumerable<DatabaseHealthRow> Database { get; set; } = [];
}

public class AppInfo
{
    public string UmbracoVersion { get; set; } = string.Empty;

    public string UmbracoSemanticVersion { get; set; } = string.Empty;

    public string DotNetVersion { get; set; } = string.Empty;

    public string RuntimeIdentifier { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    public string ProcessArchitecture { get; set; } = string.Empty;

    public string WebServer { get; set; } = string.Empty;

    public string ApplicationMainUrl { get; set; } = string.Empty;

    public bool DebugMode { get; set; }

    public string EnvironmentName { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string ContentRootPath { get; set; } = string.Empty;

    public string WebRootPath { get; set; } = string.Empty;

    public int ProcessId { get; set; }

    public DateTime StartedAt { get; set; }

    public string Uptime { get; set; } = string.Empty;

    public string GodModeVersion { get; set; } = string.Empty;
}

public class AssetCheck
{
    public string Label { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long Size { get; set; }
}

public class FolderSizeInfo
{
    public string Label { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public long Size { get; set; }

    public int FileCount { get; set; }
}

public class CacheStatus
{
    public string PublishedContentCacheType { get; set; } = string.Empty;

    public string NuCacheSerializerType { get; set; } = string.Empty;

    public IEnumerable<CacheSettingInfo> Settings { get; set; } = [];

    public IEnumerable<FolderSizeInfo> Folders { get; set; } = [];

    public IEnumerable<DatabaseHealthRow> DatabaseRows { get; set; } = [];
}

public class CacheSettingInfo
{
    public string Label { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class ServerStats
{
    public MemoryStats Memory { get; set; } = new();

    public IEnumerable<DiskStats> Disks { get; set; } = [];

    public int ProcessorCount { get; set; }

    public int ThreadCount { get; set; }

    public int HandleCount { get; set; }
}

public class MemoryStats
{
    public long WorkingSetBytes { get; set; }

    public long PrivateMemoryBytes { get; set; }

    public long ManagedHeapBytes { get; set; }

    public long TotalAvailableMemoryBytes { get; set; }

    public long TotalAllocatedBytes { get; set; }
}

public class DiskStats
{
    public string Name { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public long TotalBytes { get; set; }

    public long FreeBytes { get; set; }

    public long UsedBytes { get; set; }

    public double UsedPercentage { get; set; }
}

public class DatabaseHealthRow
{
    public string Label { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public long Count { get; set; }

    public bool Exists { get; set; } = true;
}
