using Diplo.GodMode.Models;
using Examine;
using Examine.Search;
using Lucene.Net.QueryParsers.Classic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services;

public class PropertySearchService(IExamineManager examineManager, IContentTypeService contentTypes,
    Interfaces.IUmbracoDatabaseService database, IOptions<GodModeConfig> config,
    IPublishedContentQuery publishedContent, IPublishedUrlProvider urlProvider, ILogger<PropertySearchService> logger)
{
    public PropertySearchOptions GetOptions()
    {
        var available = examineManager.TryGetIndex(Constants.UmbracoIndexes.InternalIndexName, out var index)
            && index.IndexExists();
        var types = contentTypes.GetAll().Where(x => !x.IsElement).ToArray();
        var properties = GetFields().GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PropertySearchChoice(x.First().Alias, x.First().Name)).OrderBy(x => x.Name).ToArray();
        return new(available, available ? null : "InternalIndex is unavailable. Check Examine Management before searching.",
            properties, types.Select(x => new PropertySearchChoice(x.Alias, x.Name ?? x.Alias)).OrderBy(x => x.Name).ToArray());
    }

    public PropertySearchResults Search(string text, string mode, string? property, string? documentType,
        string? culture, int page, int pageSize)
    {
        if (!examineManager.TryGetIndex(Constants.UmbracoIndexes.InternalIndexName, out var index) || !index.IndexExists())
            throw new InvalidOperationException("InternalIndex is unavailable. Check Examine Management before searching.");

        var fields = GetFields().Where(x => (string.IsNullOrEmpty(property) || x.Alias == property)
            && (string.IsNullOrEmpty(culture) || x.Culture is null || x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (fields.Length == 0) return new(0, page, 0, []);
        if (fields.Length > 256) throw new ArgumentException("Select a property or language to narrow the search.");

        var query = BuildQuery(text, mode, fields.Select(x => x.FieldName), documentType);
        var hits = index.Searcher.CreateQuery().NativeQuery(query).Execute(new QueryOptions((page - 1) * pageSize, pageSize));
        var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var items = hits.Select(hit => {
            var matches = fields.SelectMany(field => hit.GetValues(field.FieldName).Select(value => (field, value)))
                .Where(x => MatchesValue(x.value, tokens, mode))
                .Select(x => new PropertySearchMatch(x.field.Alias, x.field.Name, x.field.Culture, MakeSnippet(x.value, tokens)))
                .ToArray();
            var nameField = string.IsNullOrEmpty(culture) ? "nodeName" : "nodeName_" + culture.ToLowerInvariant();
            var state = hit.Values.GetValueOrDefault("__Published");
            var key = hit.Values.GetValueOrDefault("__Key") ?? "";
            return new PropertySearchItem(hit.Id, key,
                hit.Values.GetValueOrDefault(nameField) ?? hit.Values.GetValueOrDefault("nodeName") ?? hit.Id,
                hit.Values.GetValueOrDefault("__NodeTypeAlias") ?? "", state is null ? null : state == "y",
                matches.Take(6).ToArray(), matches.Length, ResolvePublishedUrl(key, culture, matches));
        }).ToArray();
        return new(hits.TotalItemCount, page, (int)Math.Ceiling((double)hits.TotalItemCount / pageSize), items);
    }

    private string? ResolvePublishedUrl(string key, string? culture, PropertySearchMatch[] matches)
    {
        if (!Guid.TryParse(key, out var contentKey)) return null;
        try
        {
            // Resolve from the published snapshot, not from the index's potentially
            // stale publication flag or a guessed concatenation of URL segments.
            var content = publishedContent.Content(contentKey);
            if (content is null) return null;
            string? viewCulture = null;
            if (content.ContentType.VariesByCulture())
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    if (!content.Cultures.ContainsKey(culture)) return null;
                    viewCulture = culture;
                }
                else
                {
                    viewCulture = matches.Select(x => x.Culture).FirstOrDefault(c => c is not null && content.Cultures.ContainsKey(c))
                        ?? content.Cultures.Keys.FirstOrDefault();
                    if (viewCulture is null) return null;
                }
            }
            var url = content.Url(urlProvider, culture: viewCulture, mode: UrlMode.Absolute);
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https") ? url : null;
        }
        catch (Exception exception)
        {
            // A custom URL provider must not prevent otherwise valid search results.
            logger.LogDebug(exception, "Could not resolve a published URL for Property Search result {ContentKey}", key);
            return null;
        }
    }

    private PropertySearchField[] GetFields()
    {
        var cultures = database.GetLanguages().Select(x => x.Culture).ToArray();
        var ignored = config.Value.AliasesToIgnore;
        return contentTypes.GetAll().Where(x => !x.IsElement)
            .SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes))
            .Where(p => !ignored.Contains(p.Alias, StringComparer.OrdinalIgnoreCase))
            .SelectMany(p => p.VariesByCulture()
                ? cultures.Select(c => new PropertySearchField(p.Alias + "_" + c.ToLowerInvariant(), p.Alias, p.Name ?? p.Alias, c))
                : [new PropertySearchField(p.Alias, p.Alias, p.Name ?? p.Alias, null)])
            .DistinctBy(x => x.FieldName).OrderBy(x => x.FieldName).ToArray();
    }

    internal static string BuildQuery(string text, string mode, IEnumerable<string> fields, string? documentType)
    {
        var terms = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var clauses = fields.Select(field => {
            var escapedField = QueryParserBase.Escape(field);
            return mode == "phrase"
                ? escapedField + ":\"" + QueryParserBase.Escape(text.Trim()) + "\""
                : "(" + string.Join(" AND ", terms.Select(term => escapedField + ":" + (mode == "prefix"
                    ? QueryParserBase.Escape(term) + "*" : "\"" + QueryParserBase.Escape(term) + "\""))) + ")";
        });
        // Paths include comma-separated ancestry. Both recycle-bin descendants and
        // their root are excluded before paging, independently of publication flags.
        var query = "+__IndexType:content -path:\\-1,\\-20* +(" + string.Join(" OR ", clauses) + ")";
        if (!string.IsNullOrEmpty(documentType)) query += " +__NodeTypeAlias:\"" + QueryParserBase.Escape(documentType) + "\"";
        return query;
    }

    internal static bool MatchesValue(string value, string[] terms, string mode)
    {
        // Mirrors the default InternalIndex whitespace analyzer for snippet selection.
        // Custom analyzers may produce hits whose original stored values do not match
        // these rules; retain those hits and show a clear fallback in the UI.
        var words = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (mode == "phrase")
            return Enumerable.Range(0, Math.Max(0, words.Length - terms.Length + 1))
                .Any(i => terms.Select((term, j) => words[i + j].Equals(term, StringComparison.OrdinalIgnoreCase)).All(x => x));
        return terms.All(term => words.Any(word => mode == "prefix"
            ? word.StartsWith(term, StringComparison.OrdinalIgnoreCase)
            : word.Equals(term, StringComparison.OrdinalIgnoreCase)));
    }

    internal static string MakeSnippet(string value, string[] terms)
    {
        var first = terms.Select(t => value.IndexOf(t, StringComparison.OrdinalIgnoreCase)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Max(0, first - 100);
        var length = Math.Min(420, value.Length - start);
        return (start > 0 ? "…" : "") + value.Substring(start, length) + (start + length < value.Length ? "…" : "");
    }
}
