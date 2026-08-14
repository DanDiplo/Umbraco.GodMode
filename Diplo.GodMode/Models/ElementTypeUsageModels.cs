using System;

namespace Diplo.GodMode.Models;

/// <summary>
/// Aggregated Element Type usage counts for the main Element Type Usage grid. Returned for every
/// Element Type in the installation, including those with zero usages (so unused types can be
/// identified and filtered).
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
    /// Standing Library items of this Element Type. Umbraco 18+ only; always 0 below Umbraco 18.
    /// </summary>
    public int LibraryItems { get; set; }

    /// <summary>
    /// Element Picker references to Library items of this Element Type. Umbraco 18+ only; always 0
    /// below Umbraco 18.
    /// </summary>
    public int ElementPickerUses { get; set; }

    /// <summary>
    /// Total usage count across all sources. An Element Type is genuinely unused only when this is
    /// zero across BlockContent, BlockSettings, ElementPicker and LibraryItem sources.
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// A single usage occurrence for an Element Type, shown in the workspace detail view.
/// </summary>
public class ElementTypeUsageDetail
{
    public int ContentNodeId { get; set; }

    public Guid ContentKey { get; set; }

    public string ContentName { get; set; } = string.Empty;

    public string? ParentName { get; set; }

    public string ContentPath { get; set; } = string.Empty;

    public DateTime VersionDate { get; set; }

    /// <summary>
    /// One of: BlockContent, BlockSettings, ElementPicker, LibraryItem, LibraryItem (Trashed).
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// The entity type of the node that owns this usage — content, media, member, element, or
    /// unknown — used by the client to build the correct edit link.
    /// </summary>
    public string EntityType { get; set; } = "unknown";
}

/// <summary>
/// Reports whether Element Type Usage analysis can run on the current database, and which of the
/// four usage sources are available.
/// </summary>
public class ElementTypeUsageStatus
{
    /// <summary>
    /// False when the analysis query cannot run at all (SQLite, or SQL Server below compatibility
    /// level 130 — both BlockContent and BlockSettings sources rely on OPENJSON). When false, no
    /// usage query is executed and <see cref="Message"/> explains why.
    /// </summary>
    public bool IsSupported { get; set; }

    /// <summary>
    /// True when the <c>umbracoElement</c> table exists (Umbraco 18+), enabling Library Item and
    /// Element Picker reporting alongside BlockContent/BlockSettings. When false, only the block
    /// editor sources are reported.
    /// </summary>
    public bool LibraryFeatureAvailable { get; set; }

    /// <summary>
    /// Explanatory message shown to the user when <see cref="IsSupported"/> is false, or an
    /// informational note when <see cref="LibraryFeatureAvailable"/> is false. Null when everything
    /// is fully supported.
    /// </summary>
    public string? Message { get; set; }
}
