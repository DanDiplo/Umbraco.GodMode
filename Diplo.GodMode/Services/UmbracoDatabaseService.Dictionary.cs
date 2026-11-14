using Diplo.GodMode.Models;
using NPoco;

namespace Diplo.GodMode.Services;

public partial class UmbracoDatabaseService
{
    // Shared by grouping and completeness checks. A row containing only spaces,
    // tabs or line breaks is still missing a translation on both database providers.
    private const string DictionaryValuePresent = "NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(T.value, CHAR(9), ''), CHAR(10), ''), CHAR(13), ''))), '') IS NOT NULL";
    private const string DictionaryGrouping = "EXISTS (SELECT 1 FROM cmsDictionary C WHERE C.parent = D.id) AND NOT EXISTS (SELECT 1 FROM cmsLanguageText T WHERE T.UniqueId = D.id AND " + DictionaryValuePresent + ")";

    public Page<DictionaryBrowserItem> GetDictionaryItems(long page, int pageSize, string? search = null,
        int? languageId = null, string status = "all", bool includeGroups = false)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var result = scope.Database.Page<DictionaryBrowserItem>(page, pageSize,
            BuildDictionaryBrowserQuery(search, languageId, status, includeGroups));

        // Fetch translations only for this page, in one query rather than one call per key.
        if (result.Items.Count > 0)
        {
            var values = scope.Database.Fetch<DictionaryBrowserValue>(new Sql(@"
                SELECT T.UniqueId AS ItemId, L.languageISOCode AS Culture, T.value AS Value
                FROM cmsLanguageText T
                INNER JOIN umbracoLanguage L ON L.id = T.languageId
                WHERE T.UniqueId IN (@0)", result.Items.Select(x => x.Id).ToArray()));
            var byItem = values.ToLookup(x => x.ItemId);
            foreach (var item in result.Items)
            {
                item.Values = byItem[item.Id].ToDictionary(x => x.Culture, x => x.Value);
            }
        }

        return result;
    }

    internal static Sql BuildDictionaryBrowserQuery(string? search, int? languageId, string status, bool includeGroups)
    {
        var query = new Sql($@"
            SELECT D.id AS Id, D.[key] AS [Key], P.id AS ParentId, P.[key] AS ParentName,
                CASE WHEN {DictionaryGrouping} THEN 1 ELSE 0 END AS IsGrouping
            FROM cmsDictionary D
            LEFT JOIN cmsDictionary P ON P.id = D.parent
            WHERE 1 = 1");

        if (!includeGroups)
        {
            query.Append($"AND NOT ({DictionaryGrouping})");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Parameterise and escape LIKE metacharacters so a key containing '%' or '_'
            // is searched literally. '!' is supported as an escape by SQL Server and SQLite.
            var pattern = "%" + search.Trim().ToLowerInvariant()
                .Replace("!", "!!").Replace("%", "!%").Replace("_", "!_").Replace("[", "![") + "%";
            var languagePredicate = languageId.HasValue ? " AND T.languageId = @1" : string.Empty;
            query.Append("AND (LOWER(D.[key]) LIKE @0 ESCAPE '!' OR EXISTS " +
                "(SELECT 1 FROM cmsLanguageText T WHERE T.UniqueId = D.id AND LOWER(T.value) LIKE @0 ESCAPE '!'" +
                languagePredicate + "))", pattern, languageId);
        }

        if (status is "missing" or "complete")
        {
            var languagePredicate = languageId.HasValue ? "L.id = @0 AND " : string.Empty;
            var missing = $@"EXISTS (SELECT 1 FROM umbracoLanguage L WHERE {languagePredicate}
                NOT EXISTS (SELECT 1 FROM cmsLanguageText T WHERE T.UniqueId = D.id
                    AND T.languageId = L.id AND {DictionaryValuePresent}))";
            query.Append(status == "missing" ? $"AND {missing}" : $"AND NOT ({missing})", languageId);
        }

        query.Append("ORDER BY D.[key], D.id");
        return query;
    }
}
