namespace Diplo.GodMode.Models;

public class UtilityDiagnostics
{
    public AppInfo App { get; set; } = new();

    public IEnumerable<AssetCheck> Assets { get; set; } = [];

    public IEnumerable<FolderSizeInfo> Folders { get; set; } = [];

    public CacheStatus Cache { get; set; } = new();

    public IEnumerable<DatabaseHealthRow> Database { get; set; } = [];
}

public class AppInfo
{
    public string EnvironmentName { get; set; } = string.Empty;

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
    public IEnumerable<CacheSettingInfo> Settings { get; set; } = [];

    public IEnumerable<FolderSizeInfo> Folders { get; set; } = [];
}

public class CacheSettingInfo
{
    public string Label { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class DatabaseHealthRow
{
    public string Label { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public long Count { get; set; }

    public bool Exists { get; set; } = true;
}
