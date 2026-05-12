namespace Diplo.GodMode.Models;

public sealed class GodModeLogEvent
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string MessageTemplate { get; set; } = string.Empty;

    public string Exception { get; set; } = string.Empty;

    public string SourceContext { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string RequestPath { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public int? ProcessId { get; set; }

    public int? ThreadId { get; set; }

    public string LogFile { get; set; } = string.Empty;

    public Dictionary<string, object?> Properties { get; set; } = [];

    public string RawJson { get; set; } = string.Empty;
}

public sealed class GodModeLogOverview
{
    public string LogFolder { get; set; } = string.Empty;

    public bool Exists { get; set; }

    public int FileCount { get; set; }
}
