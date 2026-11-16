namespace Diplo.GodMode.Models;

public sealed class ServerRegistrationCleanupPreview
{
    public int MinimumAgeDays { get; set; }
    public int MaximumBatchSize { get; set; }
    public DateTime CutoffUtc { get; set; }
    public int TotalEligible { get; set; }
    public List<ServerRegistrationCleanupItem> Servers { get; set; } = [];
}

public sealed class ServerRegistrationCleanupItem
{
    public int Id { get; set; }
    public string Identity { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public DateTime LastNotifiedDate { get; set; }
}

public sealed class ServerRegistrationCleanupRequest
{
    public int[] Ids { get; set; } = [];
    public bool Confirmed { get; set; }
}

public sealed class ServerRegistrationCleanupResult
{
    public List<int> DeletedIds { get; set; } = [];
    public List<int> SkippedIds { get; set; } = [];
}
