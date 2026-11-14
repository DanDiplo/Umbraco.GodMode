using NPoco;

namespace Diplo.GodMode.Models;

public class DictionaryBrowserItem
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public bool IsGrouping { get; set; }

    [ResultColumn]
    public Dictionary<string, string?> Values { get; set; } = new();
}

internal class DictionaryBrowserValue
{
    public Guid ItemId { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string? Value { get; set; }
}
