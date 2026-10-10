namespace Diplo.GodMode.Models;

/// <summary>
/// Pending scheduled publish/unpublish entries plus the state of the jobs that process them.
/// </summary>
public class ContentScheduleOverview
{
    public DateTime ServerUtcNow { get; set; }

    /// <summary>Schedules this far in the past are reported as overdue (the publishing job runs every minute).</summary>
    public int OverdueAfterMinutes { get; set; }

    public IEnumerable<ContentScheduleItem> Items { get; set; } = [];

    public IEnumerable<DistributedJobInfo> Jobs { get; set; } = [];

    public IEnumerable<RegisteredServerInfo> Servers { get; set; } = [];

    public string CurrentServerRole { get; set; } = string.Empty;

    public bool AutomaticServerRegistration { get; set; }

    public bool ScheduledPublishingSuspended { get; set; }

    public bool JobsCheckSucceeded { get; set; }

    public bool ServersCheckSucceeded { get; set; }
}

public class ContentScheduleItem
{
    public Guid Id { get; set; }

    public int NodeId { get; set; }

    public Guid NodeKey { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContentTypeAlias { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    /// <summary>The ISO code of the culture being scheduled, or empty for invariant content.</summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>Release (publish) or Expire (unpublish).</summary>
    public string Action { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public bool Published { get; set; }

    public bool Trashed { get; set; }

    public bool IsOverdue { get; set; }
}

/// <summary>
/// A row from Umbraco 17's umbracoDistributedJob table.
/// </summary>
public class DistributedJobInfo
{
    public string Name { get; set; } = string.Empty;

    public DateTime LastRun { get; set; }

    public DateTime LastAttemptedRun { get; set; }

    public long PeriodSeconds { get; set; }

    public bool IsRunning { get; set; }

    /// <summary>True when the job has not run for several periods.</summary>
    public bool IsStale { get; set; }
}

/// <summary>
/// A row from umbracoServer with a staleness flag.
/// </summary>
public class RegisteredServerInfo
{
    public int Id { get; set; }

    public string Address { get; set; } = string.Empty;

    public string ComputerName { get; set; } = string.Empty;

    public DateTime RegisteredDate { get; set; }

    public DateTime LastNotifiedDate { get; set; }

    public bool IsActive { get; set; }

    public bool IsSchedulingPublisher { get; set; }

    public bool IsStale { get; set; }
}
