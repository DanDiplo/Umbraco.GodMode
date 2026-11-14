namespace Diplo.GodMode.Models;

public record PropertySearchChoice(string Alias, string Name);
public record PropertySearchField(string FieldName, string Alias, string Name, string? Culture);
public record PropertySearchOptions(bool Available, string? Message,
    IReadOnlyList<PropertySearchChoice> Properties, IReadOnlyList<PropertySearchChoice> DocumentTypes);
public record PropertySearchMatch(string PropertyAlias, string PropertyName, string? Culture, string Snippet);
public record PropertySearchItem(string Id, string Key, string Name, string DocumentType, bool? Published,
    IReadOnlyList<PropertySearchMatch> Matches, int MatchingFieldCount, string? PublishedUrl);
public record PropertySearchResults(long TotalItems, int CurrentPage, int TotalPages,
    IReadOnlyList<PropertySearchItem> Items);
