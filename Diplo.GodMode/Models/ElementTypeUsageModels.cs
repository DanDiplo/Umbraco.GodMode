using System;

namespace Diplo.GodMode.Models;

/// <summary>
/// Aggregated Element Type usage counts for the main Element Type Usage grid. Stored block
/// occurrences and block-editor configuration references are both included so a zero count is a
/// meaningful unused signal.
/// </summary>
public class ElementTypeUsageSummary
{
    public int ElementTypeId { get; set; }

    public Guid ElementTypeKey { get; set; }

    public string ElementTypeName { get; set; } = string.Empty;

    public string ElementTypeAlias { get; set; } = string.Empty;

    public string? Icon { get; set; }

    /// <summary>
    /// Usages found in Block List / Block Grid <c>contentData</c>.
    /// </summary>
    public int ContentUses { get; set; }

    /// <summary>
    /// Usages found in Block List / Block Grid <c>settingsData</c>.
    /// </summary>
    public int SettingsUses { get; set; }

    /// <summary>
    /// Block editor data-type configurations that reference this Element Type as content or settings.
    /// </summary>
    public int ConfiguredUses { get; set; }

    /// <summary>
    /// Standing Library items of this Element Type. Available when the Umbraco 18 Elements schema
    /// is present; otherwise zero.
    /// </summary>
    public int LibraryItems { get; set; }

    /// <summary>
    /// Element Picker references to Library items of this Element Type. Available when the Umbraco
    /// 18 Elements schema is present; otherwise zero.
    /// </summary>
    public int ElementPickerUses { get; set; }

    /// <summary>
    /// Total usage count across stored blocks, block-editor configuration references, Library
    /// items and Element Picker references.
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// A single usage occurrence for an Element Type, shown in the workspace detail view.
/// </summary>
public class ElementTypeUsageDetail
{
    internal Guid ElementTypeKey { get; set; }

    public int ContentNodeId { get; set; }

    public Guid ContentKey { get; set; }

    public string ContentName { get; set; } = string.Empty;

    public string? ParentName { get; set; }

    public string ContentPath { get; set; } = string.Empty;

    public DateTime VersionDate { get; set; }

    /// <summary>
    /// One of: BlockContent, BlockSettings, ConfigurationContent, ConfigurationSettings,
    /// ElementPicker, LibraryItem, LibraryItem (Trashed).
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// The entity type of the node that owns this usage — content, media, member, dataType, element,
    /// or unknown — used by the client to build the correct edit link.
    /// </summary>
    public string EntityType { get; set; } = "unknown";
}

internal sealed record ElementTypeConfigurationReference(Guid ElementTypeKey, string SourceType);

/// <summary>
/// Reports whether Element Type Usage analysis can run on the current database.
/// </summary>
public class ElementTypeUsageStatus
{
    /// <summary>
    /// False when the analysis query cannot run. SQL Server requires compatibility level 130 or
    /// higher for OPENJSON; SQLite uses its bundled JSON1 support.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    /// True when the Umbraco Elements schema is present, enabling Library Item and Element Picker
    /// reporting. False on Umbraco 17.
    /// </summary>
    public bool LibraryFeatureAvailable { get; set; }

    /// <summary>
    /// Explanatory message shown to the user when <see cref="IsSupported"/> is false.
    /// </summary>
    public string? Message { get; set; }
}
