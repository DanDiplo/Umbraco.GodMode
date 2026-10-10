namespace Diplo.GodMode.Models;

/// <summary>
/// Describes the server clock, time zone and the Umbraco 17 UTC system date migration.
/// </summary>
public class ServerTimeInfo
{
    public DateTime ServerUtcNow { get; set; }

    public DateTimeOffset ServerLocalNow { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>IANA id (e.g. Europe/London) usable by the browser's Intl API, when one can be resolved.</summary>
    public string IanaId { get; set; } = string.Empty;

    /// <summary>Windows id (e.g. GMT Standard Time), which is what SQL Server and the Umbraco migration settings use.</summary>
    public string WindowsId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string StandardName { get; set; } = string.Empty;

    public string DaylightName { get; set; } = string.Empty;

    public string UtcOffset { get; set; } = string.Empty;

    public int UtcOffsetMinutes { get; set; }

    public int BaseUtcOffsetMinutes { get; set; }

    public bool IsUtc { get; set; }

    public bool SupportsDaylightSavingTime { get; set; }

    public bool IsDaylightSavingTime { get; set; }

    public DateTimeOffset? NextTransition { get; set; }

    public string NextTransitionUtcOffset { get; set; } = string.Empty;

    public string TzEnvironmentVariable { get; set; } = string.Empty;

    public string TimeProviderType { get; set; } = string.Empty;

    public string CultureName { get; set; } = string.Empty;

    public string ShortDatePattern { get; set; } = string.Empty;

    public string LongTimePattern { get; set; } = string.Empty;

    public DateTime ProcessStartedUtc { get; set; }

    public SystemDateMigrationInfo SystemDateMigration { get; set; } = new();
}

/// <summary>
/// The state of Umbraco's <c>Umbraco:CMS:SystemDateMigration</c> settings and the v17 MigrateSystemDatesToUtc migration.
/// </summary>
public class SystemDateMigrationInfo
{
    public bool Enabled { get; set; }

    public string ConfiguredTimeZone { get; set; } = string.Empty;

    /// <summary>Validity for the database provider; null when unconfigured or validation is unavailable.</summary>
    public bool? ConfiguredTimeZoneValid { get; set; }

    public string TimeZoneValidationMessage { get; set; } = string.Empty;

    public bool EffectiveTimeZoneResolved { get; set; }

    public bool? EffectiveTimeZoneSupportsDaylightSaving { get; set; }

    /// <summary>The time zone the migration would use (configured, or the detected server zone).</summary>
    public string EffectiveTimeZone { get; set; } = string.Empty;

    /// <summary>Whether the effective time zone has the same adjustment rules as this server.</summary>
    public bool EffectiveTimeZoneMatchesServer { get; set; }

    /// <summary>True when the effective time zone is UTC, so the migration has nothing to convert.</summary>
    public bool EffectiveTimeZoneIsUtc { get; set; }

    public string DatabaseType { get; set; } = string.Empty;

    /// <summary>True on SQLite, where the migration applies the base UTC offset only and ignores daylight saving.</summary>
    public bool UsesBaseOffsetOnly { get; set; }

    /// <summary>True when the database has all Umbraco migrations applied (an upgraded or fresh v17 install).</summary>
    public bool UpgradeComplete { get; set; }

    public string CurrentMigrationState { get; set; } = string.Empty;

    public string FinalMigrationState { get; set; } = string.Empty;
}

/// <summary>
/// Evidence of how the v17 UTC migration actually ran, gathered from the log files and database.
/// </summary>
public class SystemDateEvidence
{
    public DateTime CheckedAtUtc { get; set; }

    public DateTime FutureDateCutoffUtc { get; set; }

    public bool LogCheckSucceeded { get; set; }

    public string LogCheckMessage { get; set; } = string.Empty;

    public bool DatabaseCheckSucceeded { get; set; }

    public string DatabaseCheckMessage { get; set; } = string.Empty;

    public IEnumerable<SystemDateLogEntry> MigrationLogEntries { get; set; } = [];

    public IEnumerable<FutureDatedRows> FutureDatedRows { get; set; } = [];
}

public class SystemDateLogEntry
{
    public DateTimeOffset? Timestamp { get; set; }

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public class FutureDatedRows
{
    public bool CheckSucceeded { get; set; }

    public string CheckMessage { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public string Column { get; set; } = string.Empty;

    public long Count { get; set; }

    public DateTime? Latest { get; set; }
}

public class SystemDateLogEvidence
{
    public bool CheckSucceeded { get; set; }

    public string CheckMessage { get; set; } = string.Empty;

    public IEnumerable<SystemDateLogEntry> Entries { get; set; } = [];
}
