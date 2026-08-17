using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Some old-fashioned SQL to get stuff in an efficient way that you can't easily get through APIs
    /// </summary>
    public class UmbracoDatabaseService : IUmbracoDatabaseService
    {
        private const string DatabaseTablesCacheKey = "Diplo.GodMode.DatabaseTables";

        /// <summary>
        /// Cache key for the aggregated Element Type usage summary. This query scans every Block
        /// List/Grid property and all block-editor configurations, so it is cached briefly.
        /// </summary>
        private const string ElementTypeUsageSummaryCacheKey = "Diplo.GodMode.ElementTypeUsageSummary";

        private static readonly TimeSpan ElementTypeUsageSummaryCacheDuration = TimeSpan.FromMinutes(5);

        private const string UmbracoElementTableName = "umbracoElement";

        /// <summary>
        /// Mirrors Umbraco.Cms.Core.Constants.ObjectTypes.Strings.Element in Umbraco 18. This
        /// assembly targets Umbraco 17, where that constant is not available at compile time.
        /// </summary>
        private const string ElementObjectTypeString = "3D7B623C-94B1-487D-8554-A46EC37568BE";

        private readonly IScopeProvider scopeProvider;
        private readonly ILogger<UmbracoDatabaseService> logger;
        private readonly IPublishedContentQuery contentQuery;
        private readonly IMemoryCache memoryCache;
        private readonly IDataTypeService dataTypeService;

        public UmbracoDatabaseService(
            IScopeProvider scopeProvider,
            IPublishedContentQuery contentQuery,
            ILogger<UmbracoDatabaseService> logger,
            IMemoryCache memoryCache,
            IDataTypeService dataTypeService)
        {
            this.scopeProvider = scopeProvider;
            this.contentQuery = contentQuery;
            this.logger = logger;
            this.memoryCache = memoryCache;
            this.dataTypeService = dataTypeService;
        }

        /// <summary>
        /// Gets all content type (doc type) aliases for content
        /// </summary>
        public IEnumerable<string> GetContentTypeAliases(bool? isContainer = null, bool? isElement = null)
        {
            string sql = @"SELECT CT.alias FROM cmsContentType CT INNER JOIN umbracoNode N ON CT.nodeId = N.id WHERE N.nodeObjectType = @0 ";

            if (isContainer.HasValue)
            {
                sql += " AND isContainer = " + (isContainer.Value ? "1" : "0");
            }

            if (isElement.HasValue)
            {
                sql += " AND isElement = " + (isElement.Value ? "1" : "0");
            }

            var query = new Sql(sql, Constants.ObjectTypes.Strings.DocumentType);

            query.OrderBy("CT.alias");

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<string>(query);
            }
        }

        /// <summary>
        /// Gets the basic page content for the site
        /// </summary>
        /// <param name="page">The pagination page</param>
        /// <param name="itemsPerPage">The pagination items per page</param>
        /// <param name="criteria">The filter criteria</param>
        /// <param name="orderBy">The order by clause</param>
        /// <returns>A list of content items</returns>
        public Page<ContentItem> GetContent(long page, long itemsPerPage, ContentCriteria criteria = null, string orderBy = "N.id")
        {
            var cultureStatesSql = scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite
                ? @"SELECT group_concat(
                        L.languageISOCode || ':' ||
                        COALESCE(DCV.available, 0) || ':' ||
                        COALESCE(DCV.published, 0) || ':' ||
                        COALESCE(DCV.edited, 0) || ':' ||
                        COALESCE(DCV.name, ''),
                        '|')
                    FROM umbracoLanguage L
                    LEFT JOIN umbracoDocumentCultureVariation DCV ON DCV.languageId = L.id AND DCV.nodeId = D.nodeId"
                : @"SELECT STRING_AGG(
                    CAST(CONCAT(
                        L.languageISOCode, ':',
                        COALESCE(DCV.available, 0), ':',
                        COALESCE(DCV.published, 0), ':',
                        COALESCE(DCV.edited, 0), ':',
                        COALESCE(DCV.name, '')
                    ) AS nvarchar(max)),
                    '|'
                ) WITHIN GROUP (ORDER BY L.id)
                    FROM umbracoLanguage L
                    LEFT JOIN umbracoDocumentCultureVariation DCV ON DCV.languageId = L.id AND DCV.nodeId = D.nodeId";

            var sql = $@"SELECT N.uniqueID as Udi, N.Id, N.ParentId, N.Level, CT.icon, N.Trashed as Trashed, CT.alias, N.Text as Name, 
                N.Path as Path, N.createDate, Creator.Id AS CreatorId, Creator.userName as CreatorName,
                V.versionDate as UpdateDate, Updater.Id as UpdaterID, Updater.userName as UpdaterName,
				DefaultLang.languageISOCode As Culture,
                CASE WHEN CT.variations = 0 THEN 'Invariant' ELSE COALESCE(({cultureStatesSql}), '') END as CultureStates
                FROM umbracoContent C
                INNER JOIN umbracoNode N ON N.Id = C.nodeId
                INNER JOIN cmsContentType CT ON C.contentTypeId = CT.nodeId
                INNER JOIN umbracoDocument D ON D.nodeId = C.nodeId
                INNER JOIN umbracoContentVersion As V ON V.nodeId = N.id
                INNER JOIN umbracoUser AS Creator ON Creator.Id = N.nodeUser
                INNER JOIN umbracoUser As Updater ON V.userId = Updater.id
                LEFT JOIN umbracoLanguage DefaultLang ON DefaultLang.isDefaultVariantLang = 1
                WHERE V.[current] = 1  ";

            var query = new Sql(sql);

            if (criteria != null)
            {
                if (!string.IsNullOrEmpty(criteria.Alias))
                {
                    query = query.Append(" AND CT.alias = @0", criteria.Alias);
                }

                if (!string.IsNullOrWhiteSpace(criteria.Name))
                {
                    var search = criteria.Name.Trim();
                    var likeSearch = "%" + search + "%";

                    query = int.TryParse(search, out var searchId)
                        ? query.Append(" AND (N.text LIKE @0 OR N.id = @1 OR N.uniqueID LIKE @0)", likeSearch, searchId)
                        : query.Append(" AND (N.text LIKE @0 OR N.uniqueID LIKE @0)", likeSearch);
                }

                if (!string.IsNullOrEmpty(criteria.Id))
                {
                    if (int.TryParse(criteria.Id, out int criteriaId))
                    {
                        query = query.Append(" AND N.id = @0", criteriaId);
                    }
                    else
                    {
                        query = query.Append(" AND N.uniqueID LIKE @0", "%" + criteria.Id + "%");
                    }
                }

                if (criteria.Level.HasValue)
                {
                    query = query.Append(" AND N.Level = @0", criteria.Level.Value);
                }

                if (criteria.Trashed.HasValue)
                {
                    query = query.Append(" AND N.Trashed = @0", criteria.Trashed.Value);
                }

                if (criteria.CreatorId.HasValue)
                {
                    query = query.Append(" AND Creator.Id = @0", criteria.CreatorId.Value);
                }

                if (criteria.UpdaterId.HasValue)
                {
                    query = query.Append(" AND Updater.Id = @0", criteria.UpdaterId.Value);
                }

                if (criteria.LanguageId.HasValue)
                {
                    if (criteria.LanguageId.Value == -1)
                    {
                        query = query.Append(" AND CT.variations = 0", criteria.LanguageId.Value);
                    }
                    else
                    {
                        query = query.Append(@" AND CT.variations <> 0
                            AND EXISTS (
                                SELECT 1
                                FROM umbracoDocumentCultureVariation FilterDCV
                                WHERE FilterDCV.nodeId = D.nodeId
                                    AND FilterDCV.languageId = @0
                                    AND FilterDCV.available = 1
                            )", criteria.LanguageId.Value);
                    }
                }

                if (criteria.MissingLanguageId.HasValue)
                {
                    query = query.Append(@" AND CT.variations <> 0
                        AND NOT EXISTS (
                            SELECT 1
                            FROM umbracoDocumentCultureVariation MissingDCV
                            WHERE MissingDCV.nodeId = D.nodeId
                                AND MissingDCV.languageId = @0
                                AND MissingDCV.available = 1
                        )", criteria.MissingLanguageId.Value);
                }

                if (criteria.PublishedLanguageId.HasValue)
                {
                    query = query.Append(@" AND CT.variations <> 0
                        AND EXISTS (
                            SELECT 1
                            FROM umbracoDocumentCultureVariation PublishedDCV
                            WHERE PublishedDCV.nodeId = D.nodeId
                                AND PublishedDCV.languageId = @0
                                AND PublishedDCV.published = 1
                        )", criteria.PublishedLanguageId.Value);
                }

                if (criteria.Edited.HasValue)
                {
                    query = criteria.Edited.Value
                        ? query.Append(@" AND (
                            D.edited = 1
                            OR EXISTS (
                                SELECT 1
                                FROM umbracoDocumentCultureVariation EditedDCV
                                WHERE EditedDCV.nodeId = D.nodeId
                                    AND EditedDCV.edited = 1
                            )
                        )")
                        : query.Append(@" AND D.edited = 0
                            AND NOT EXISTS (
                                SELECT 1
                                FROM umbracoDocumentCultureVariation EditedDCV
                                WHERE EditedDCV.nodeId = D.nodeId
                                    AND EditedDCV.edited = 1
                            )");
                }
            }

            query.GroupBy(@"N.uniqueID, N.Id, N.ParentId, N.Level, CT.icon, N.Trashed, CT.alias, N.Text,
                N.Path, N.createDate, Creator.Id, Creator.userName, V.versionDate, Updater.Id, Updater.userName,
                DefaultLang.languageISOCode, CT.variations, D.nodeId");

            if (!string.IsNullOrEmpty(orderBy))
            {
                query.OrderBy(orderBy);
            }

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var paged = scope.Database.Page<ContentItem>(page, itemsPerPage, query);
                return paged;
            }
        }

        public IEnumerable<Lang> GetLanguages()
        {
            var query = new Sql("SELECT id, languageCultureName as Name, LanguageISOCode as Culture FROM umbracoLanguage");

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<Lang>(query);
            }
        }

        public IEnumerable<Lang> GetLanguagesWithoutAssignedDomains()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var domainTable = GetTableNames(scope.Database).FirstOrDefault(x => x.Name.InvariantEquals("umbracoDomain"));
                if (domainTable is null)
                {
                    return [];
                }

                var domainColumns = GetColumns(scope.Database, domainTable.Schema, domainTable.Name).ToList();
                var languagePredicate = GetDomainLanguagePredicate(domainColumns);
                if (languagePredicate is null)
                {
                    logger.LogDebug("Could not find a language column on umbracoDomain for health risk checks.");
                    return [];
                }

                var query = new Sql($@"SELECT L.id, L.languageCultureName as Name, L.languageISOCode as Culture
                    FROM umbracoLanguage L
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM umbracoDomain D
                        WHERE {languagePredicate}
                            AND COALESCE(D.domainName, '') <> ''
                    )
                    ORDER BY L.languageISOCode");

                return scope.Database.Fetch<Lang>(query);
            }
        }

        /// <summary>
        /// Gets, for every language, how many dictionary items are untranslated (have no
        /// <c>cmsLanguageText</c> row for that language, or a row whose value is blank).
        /// </summary>
        /// <remarks>
        /// This goes straight to the database so the whole site's dictionary can be checked
        /// in a single query rather than materialising every item through the Umbraco services.
        /// </remarks>
        public IEnumerable<DictionaryTranslationStatus> GetDictionaryTranslationStatus()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var tableNames = GetTableNames(scope.Database);

                if (!tableNames.Any(x => x.Name.InvariantEquals("cmsDictionary"))
                    || !tableNames.Any(x => x.Name.InvariantEquals("cmsLanguageText")))
                {
                    logger.LogDebug("Skipping dictionary translation check because the dictionary tables were not found.");
                    return [];
                }

                // cmsLanguageText.value is nvarchar(max) on SQL Server and TEXT on SQLite; both
                // support LTRIM/RTRIM, so a single query works across either provider.
                var query = new Sql(@"SELECT L.id as LanguageId, L.languageCultureName as Name, L.languageISOCode as Culture,
                        (SELECT COUNT(*) FROM cmsDictionary) as TotalItems,
                        (
                            SELECT COUNT(*)
                            FROM cmsDictionary D
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM cmsLanguageText T
                                WHERE T.UniqueId = D.id
                                    AND T.languageId = L.id
                                    AND T.value IS NOT NULL
                                    AND LTRIM(RTRIM(T.value)) <> ''
                            )
                        ) as UntranslatedItems
                    FROM umbracoLanguage L
                    ORDER BY L.languageISOCode");

                return scope.Database.Fetch<DictionaryTranslationStatus>(query);
            }
        }

        /// <summary>
        /// Gets a list of URLs, each corresponding to a page with a unique template
        /// </summary>
        /// <remarks>
        /// This is used so we can ping each URL to "warm-up" the compilation of the view it uses
        /// </remarks>
        /// <returns>A list of URLs</returns>
        public IEnumerable<string> GetTemplateUrlsToPing()
        {
            const string sql = @";WITH UniqueTemplateNode AS
            (
				SELECT C.nodeId,
	            ROW_NUMBER() OVER (PARTITION BY DT.TemplateNodeId ORDER BY C.NodeId) AS rn
				FROM cmsDocumentType DT
				INNER JOIN cmsTemplate ON DT.templateNodeId = cmsTemplate.nodeId
				INNER JOIN umbracoContent C ON C.contentTypeId = DT.contentTypeNodeId
            )
               SELECT nodeId FROM UniqueTemplateNode WHERE rn = 1";

            var query = new Sql(sql);

            var ids = new List<int>();

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                try
                {
                    ids = scope.Database.Fetch<int>(query);
                }
                catch (Exception ex)
                {
                    throw new NotSupportedException("Sorry, this operation is only supported when using SQL Server", ex);
                }
            }

            foreach (var id in ids)
            {
                IPublishedContent node = null;

                try
                {
                    node = this.contentQuery.Content(id);
                }
                catch
                {
                    // we ignore it if we can't get an instance
                }

                if (node != null)
                {
                    string url = null;

                    try
                    {
                        url = node.Url(mode: UrlMode.Absolute);
                    }
                    catch
                    {
                        // we ignore it if the node doesn't have an absolute URL
                    }

                    if (!string.IsNullOrEmpty(url))
                    {
                        yield return url;
                    }
                }
            }
        }

        /// <summary>
        /// Gets what content types are used and how many instances of each there are
        /// </summary>
        /// <param name="id">Optional ID of the content type to filter by</param>
        /// <returns>A list of data</returns>
        public List<UsageModel> GetContentUsageData(int? id = null, string orderBy = "CT.alias")
        {
            const string sql = @"SELECT COUNT(N.id) as NodeCount, COALESCE(CT.description,'') as Description, CT.alias as Alias, CT.icon as Icon, CT.pk As Id, N.nodeObjectType As GuidType
            FROM cmsContentType CT
            JOIN umbracoContent C ON C.contentTypeId = CT.nodeId
            JOIN umbracoNode N ON C.nodeId = N.id  ";

            var query = new Sql(sql);

            if (id != null)
            {
                query = query.Append(" AND CT.pk = @0", id);
            }

            query.GroupBy("CT.alias, CT.icon, CT.description, CT.pk, N.nodeObjectType");

            if (!string.IsNullOrEmpty(orderBy))
            {
                query.OrderBy(orderBy);
            }

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<UsageModel>(query);
            }
        }

        /// <summary>
        /// Reports whether Element Type Usage analysis can run on the current database and detects
        /// the optional Umbraco 18 Elements schema without taking a compile-time v18 dependency.
        /// </summary>
        public ElementTypeUsageStatus GetElementTypeUsageStatus()
        {
            using var scope = this.scopeProvider.CreateScope(autoComplete: true);

            if (this.scopeProvider.SqlContext.DatabaseType != DatabaseType.SQLite)
            {
                var compatibilityLevel = scope.Database.ExecuteScalar<int?>(
                    "SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()");

                if (compatibilityLevel is null or < 130)
                {
                    return new ElementTypeUsageStatus
                    {
                        IsSupported = false,
                        LibraryFeatureAvailable = false,
                        Message = "Element Type Usage requires SQL compatibility level 130 or higher. OPENJSON is unavailable below compatibility level 130."
                    };
                }
            }

            bool libraryFeatureAvailable = GetTableNames(scope.Database)
                .Any(table => table.Name.InvariantEquals(UmbracoElementTableName));

            return new ElementTypeUsageStatus
            {
                IsSupported = true,
                LibraryFeatureAvailable = libraryFeatureAvailable,
                Message = null
            };
        }

        /// <summary>
        /// Gets aggregated stored-block and block-editor-configuration usage counts. A refresh
        /// request bypasses the short-lived cache so the UI reload action always performs a fresh read.
        /// </summary>
        public async Task<IEnumerable<ElementTypeUsageSummary>> GetElementTypeUsageSummary(bool refresh = false)
        {
            if (refresh)
            {
                this.memoryCache.Remove(ElementTypeUsageSummaryCacheKey);
            }
            else if (this.memoryCache.TryGetValue(ElementTypeUsageSummaryCacheKey, out IReadOnlyList<ElementTypeUsageSummary>? cached) && cached is not null)
            {
                return cached;
            }

            ElementTypeUsageStatus status = GetElementTypeUsageStatus();
            if (!status.IsSupported)
            {
                return [];
            }

            List<ElementTypeUsageSummary> result;
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var sql = new Sql(BuildElementTypeUsageCte(status.LibraryFeatureAvailable, this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite) + @"
SELECT
    ET.nodeId AS ElementTypeId,
    ETN.uniqueId AS ElementTypeKey,
    ETN.[text] AS ElementTypeName,
    ET.alias AS ElementTypeAlias,
    ET.icon AS Icon,
    COALESCE(U.ContentUses, 0) AS ContentUses,
    COALESCE(U.SettingsUses, 0) AS SettingsUses,
    0 AS ConfiguredUses,
    0 AS NestedUses,
    COALESCE(U.LibraryItems, 0) AS LibraryItems,
    COALESCE(U.ElementPickerUses, 0) AS ElementPickerUses,
    COALESCE(U.UsageCount, 0) AS UsageCount
FROM cmsContentType ET
INNER JOIN umbracoNode ETN ON ETN.id = ET.nodeId
LEFT JOIN
(
    SELECT
        ElementTypeId,
        SUM(CASE WHEN SourceType = 'BlockContent' THEN 1 ELSE 0 END) AS ContentUses,
        SUM(CASE WHEN SourceType = 'BlockSettings' THEN 1 ELSE 0 END) AS SettingsUses,
        SUM(CASE WHEN SourceType IN ('LibraryItem', 'LibraryItem (Trashed)') THEN 1 ELSE 0 END) AS LibraryItems,
        SUM(CASE WHEN SourceType = 'ElementPicker' THEN 1 ELSE 0 END) AS ElementPickerUses,
        COUNT(*) AS UsageCount
    FROM ElementTypeUsage
    GROUP BY ElementTypeId
) U ON U.ElementTypeId = ET.nodeId
WHERE ET.isElement = 1
ORDER BY ETN.[text]");

                result = scope.Database.Fetch<ElementTypeUsageSummary>(sql);
            }

            var configuredUsage = await this.GetElementTypeConfigurationUsage();
            var nestedUsage = await this.GetElementTypeNestedInlineBlockUsage(status);

            var configuredCounts = configuredUsage
                .GroupBy(x => x.ElementTypeKey)
                .ToDictionary(group => group.Key, group => group.Count());

            var nestedCounts = nestedUsage
                .GroupBy(x => x.ElementTypeKey)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (var item in result)
            {
                item.ConfiguredUses = configuredCounts.GetValueOrDefault(item.ElementTypeKey);
                item.NestedUses = nestedCounts.GetValueOrDefault(item.ElementTypeKey);
                item.UsageCount += item.ConfiguredUses + item.NestedUses;
            }

            this.memoryCache.Set(ElementTypeUsageSummaryCacheKey, result, ElementTypeUsageSummaryCacheDuration);

            return result;
        }

        /// <summary>
        /// Gets every individual usage occurrence for a single Element Type, across all sources.
        /// </summary>
        public async Task<IEnumerable<ElementTypeUsageDetail>> GetElementTypeUsageDetail(Guid elementTypeKey)
        {
            ElementTypeUsageStatus status = GetElementTypeUsageStatus();
            if (!status.IsSupported)
            {
                return [];
            }

            List<ElementTypeUsageDetail> result;
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                bool isSqlite = this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite;
                string elementTypeKeyPredicate = BuildElementTypeUsageKeyPredicate(isSqlite);
                var sql = new Sql(BuildElementTypeUsageCte(status.LibraryFeatureAvailable, isSqlite) + $@"
SELECT
    ContentNodeId,
    ContentKey,
    ContentName,
    ParentName,
    ContentPath,
    VersionDate,
    SourceType,
    CASE
        WHEN ReferencingNodeObjectType = @0 THEN 'content'
        WHEN ReferencingNodeObjectType = @1 THEN 'media'
        WHEN ReferencingNodeObjectType = @2 THEN 'member'
        WHEN ReferencingNodeObjectType = @3 THEN 'element'
        ELSE 'unknown'
    END AS EntityType
FROM ElementTypeUsage
WHERE {elementTypeKeyPredicate}
ORDER BY SourceType, ContentPath",
                Constants.ObjectTypes.Strings.Document,
                Constants.ObjectTypes.Strings.Media,
                Constants.ObjectTypes.Strings.Member,
                ElementObjectTypeString,
                elementTypeKey);

                result = scope.Database.Fetch<ElementTypeUsageDetail>(sql);
            }

            result.AddRange((await this.GetElementTypeConfigurationUsage())
                .Where(x => x.ElementTypeKey == elementTypeKey));

            result.AddRange((await this.GetElementTypeNestedInlineBlockUsage(status))
                .Where(x => x.ElementTypeKey == elementTypeKey));

            return result.OrderBy(x => x.SourceType).ThenBy(x => x.ContentName);
        }

        internal static string BuildElementTypeUsageKeyPredicate(bool isSqlite)
            => isSqlite ? "ElementTypeKey = @4 COLLATE NOCASE" : "ElementTypeKey = @4";

        /// <summary>
        /// Builds the shared usage CTE. Stored blocks use provider-specific JSON functions; Library
        /// Item and Element Picker sources are appended only when the Elements schema is available.
        /// </summary>
        internal static string BuildElementTypeUsageCte(bool includeLibrarySources, bool isSqlite)
        {
            // Leading semicolon is required: NPoco's EnableAutoSelect otherwise prepends
            // "SELECT <cols> FROM <table>" to any query that doesn't start with SELECT/EXEC,
            // which mangles a CTE's leading WITH into an invalid table hint. NPoco explicitly
            // strips a leading ";" and skips that rewrite (see NPoco.AutoSelectHelper.AddSelectClause).
            const string cteHeaderSql = @";
WITH BlockPropertyTypes AS
(
    SELECT cpt.id, udt.propertyEditorAlias AS EditorAlias
    FROM cmsPropertyType cpt
    INNER JOIN umbracoDataType udt ON udt.nodeId = cpt.dataTypeId
    WHERE udt.propertyEditorAlias IN ('Umbraco.BlockList', 'Umbraco.BlockGrid', 'Umbraco.RichText')
),
ElementTypeUsage AS
(";

            // SQL Server: CROSS APPLY OPENJSON, typed straight to uniqueidentifier so the join and
            // the later comparisons against Constants.ObjectTypes.Strings.* are case-insensitive by
            // virtue of being a real GUID comparison rather than a string comparison.
            const string sqlServerBlockSourcesSql = @"
    SELECT
        ucv.nodeId AS ContentNodeId,
        un.uniqueId AS ContentKey,
        ucv.[text] AS ContentName,
        up.[text] AS ParentName,
        un.[path] AS ContentPath,
        ucv.versionDate AS VersionDate,
        un.nodeObjectType AS ReferencingNodeObjectType,
        JsonData.contentTypeKey AS ElementTypeKey,
        et.[text] AS ElementTypeName,
        et.id AS ElementTypeId,
        JsonData.SourceType AS SourceType
    FROM umbracoPropertyData upd
    INNER JOIN umbracoContentVersion ucv ON ucv.id = upd.versionId
    INNER JOIN umbracoNode un ON un.id = ucv.nodeId
    LEFT JOIN umbracoNode up ON up.id = un.parentId
    INNER JOIN BlockPropertyTypes bpt ON bpt.id = upd.propertyTypeId
    CROSS APPLY
    (
        SELECT contentTypeKey, 'BlockContent' AS SourceType
        FROM OPENJSON(
            CASE WHEN ISJSON(upd.textValue) = 1 THEN
                CASE WHEN bpt.EditorAlias = 'Umbraco.RichText'
                    THEN JSON_QUERY(upd.textValue, '$.blocks.contentData')
                    ELSE JSON_QUERY(upd.textValue, '$.contentData')
                END
            END) WITH (contentTypeKey UNIQUEIDENTIFIER '$.contentTypeKey')
        UNION ALL
        SELECT contentTypeKey, 'BlockSettings' AS SourceType
        FROM OPENJSON(
            CASE WHEN ISJSON(upd.textValue) = 1 THEN
                CASE WHEN bpt.EditorAlias = 'Umbraco.RichText'
                    THEN JSON_QUERY(upd.textValue, '$.blocks.settingsData')
                    ELSE JSON_QUERY(upd.textValue, '$.settingsData')
                END
            END) WITH (contentTypeKey UNIQUEIDENTIFIER '$.contentTypeKey')
    ) JsonData
    INNER JOIN umbracoNode et ON et.uniqueId = JsonData.contentTypeKey
    WHERE ucv.[current] = 1 AND JsonData.contentTypeKey IS NOT NULL";

            // SQLite: json_each is a table-valued function that SQLite allows to be "left
            // correlated" against a column from an earlier table in the same FROM clause (upd.
            // textValue here) - but only when called directly in the FROM list, not from inside a
            // derived subquery. So, unlike the SQL Server CROSS APPLY above, contentData and
            // settingsData can't be combined into one shared derived table; they're written as two
            // separate SELECTs unioned together instead. The join below requires an explicit
            // COLLATE NOCASE: unlike umbracoNode.path/text, the uniqueId and nodeObjectType columns
            // are plain TEXT (default BINARY collation) in Umbraco's SQLite schema - confirmed by
            // inspecting a live database - and .NET's Guid.ToString() serializes the Block List
            // JSON's contentTypeKey in lowercase while uniqueId is stored uppercase, so a plain "="
            // silently matches zero rows without it.
            const string sqliteBlockSourcesSql = @"
    SELECT
        ucv.nodeId AS ContentNodeId,
        un.uniqueId AS ContentKey,
        ucv.[text] AS ContentName,
        up.[text] AS ParentName,
        un.[path] AS ContentPath,
        ucv.versionDate AS VersionDate,
        un.nodeObjectType AS ReferencingNodeObjectType,
        et.uniqueId AS ElementTypeKey,
        et.[text] AS ElementTypeName,
        et.id AS ElementTypeId,
        'BlockContent' AS SourceType
    FROM umbracoPropertyData upd
    INNER JOIN umbracoContentVersion ucv ON ucv.id = upd.versionId
    INNER JOIN umbracoNode un ON un.id = ucv.nodeId
    LEFT JOIN umbracoNode up ON up.id = un.parentId
    INNER JOIN BlockPropertyTypes bpt ON bpt.id = upd.propertyTypeId
    , json_each(CASE WHEN json_valid(upd.textValue) THEN
        CASE WHEN bpt.EditorAlias = 'Umbraco.RichText'
            THEN json_extract(upd.textValue, '$.blocks.contentData')
            ELSE json_extract(upd.textValue, '$.contentData')
        END
      END) je
    INNER JOIN umbracoNode et ON et.uniqueId = json_extract(je.value, '$.contentTypeKey') COLLATE NOCASE
    WHERE ucv.[current] = 1

    UNION ALL

    SELECT
        ucv.nodeId,
        un.uniqueId,
        ucv.[text],
        up.[text],
        un.[path],
        ucv.versionDate,
        un.nodeObjectType,
        et.uniqueId,
        et.[text],
        et.id,
        'BlockSettings'
    FROM umbracoPropertyData upd
    INNER JOIN umbracoContentVersion ucv ON ucv.id = upd.versionId
    INNER JOIN umbracoNode un ON un.id = ucv.nodeId
    LEFT JOIN umbracoNode up ON up.id = un.parentId
    INNER JOIN BlockPropertyTypes bpt ON bpt.id = upd.propertyTypeId
    , json_each(CASE WHEN json_valid(upd.textValue) THEN
        CASE WHEN bpt.EditorAlias = 'Umbraco.RichText'
            THEN json_extract(upd.textValue, '$.blocks.settingsData')
            ELSE json_extract(upd.textValue, '$.settingsData')
        END
      END) je
    INNER JOIN umbracoNode et ON et.uniqueId = json_extract(je.value, '$.contentTypeKey') COLLATE NOCASE
    WHERE ucv.[current] = 1";

            // Both sources are v18-only and are appended only after capability detection confirms
            // the Elements table exists. Keep this SQL isolated so the v17 execution path never
            // parses or resolves v18-only tables.
            const string librarySourcesSql = @"

    UNION ALL

    SELECT
        referencingNode.id,
        referencingNode.uniqueId,
        rucv.[text],
        rup.[text],
        referencingNode.[path],
        rucv.versionDate,
        referencingNode.nodeObjectType,
        et2.uniqueId,
        et2.[text],
        et2.id,
        'ElementPicker'
    FROM umbracoRelation r
    INNER JOIN umbracoRelationType rt ON rt.id = r.relType AND rt.alias = 'umbElement'
    INNER JOIN umbracoNode referencingNode ON referencingNode.id = r.parentId
    INNER JOIN umbracoContentVersion rucv ON rucv.nodeId = referencingNode.id AND rucv.[current] = 1
    LEFT JOIN umbracoNode rup ON rup.id = referencingNode.parentId
    INNER JOIN umbracoNode pickedElementNode ON pickedElementNode.id = r.childId
    INNER JOIN umbracoContent pickedElementContent ON pickedElementContent.nodeId = pickedElementNode.id
    INNER JOIN umbracoNode et2 ON et2.id = pickedElementContent.contentTypeId

    UNION ALL

    SELECT
        libNode.id,
        libNode.uniqueId,
        lucv.[text],
        lup.[text],
        libNode.[path],
        lucv.versionDate,
        libNode.nodeObjectType,
        et3.uniqueId,
        et3.[text],
        et3.id,
        CASE WHEN libNode.trashed = 1 THEN 'LibraryItem (Trashed)' ELSE 'LibraryItem' END
    FROM umbracoElement ue
    INNER JOIN umbracoNode libNode ON libNode.id = ue.nodeId
    INNER JOIN umbracoContent uc ON uc.nodeId = libNode.id
    INNER JOIN umbracoNode et3 ON et3.id = uc.contentTypeId
    INNER JOIN umbracoContentVersion lucv ON lucv.nodeId = libNode.id AND lucv.[current] = 1
    LEFT JOIN umbracoNode lup ON lup.id = libNode.parentId";

            string blockSourcesSql = isSqlite ? sqliteBlockSourcesSql : sqlServerBlockSourcesSql;

            return cteHeaderSql
                + blockSourcesSql
                + (includeLibrarySources ? librarySourcesSql : string.Empty)
                + "\n)\n";
        }

        private async Task<List<ElementTypeUsageDetail>> GetElementTypeConfigurationUsage()
        {
            var result = new List<ElementTypeUsageDetail>();
            var dataTypes = await this.dataTypeService.GetAllAsync();

            foreach (var dataType in dataTypes)
            {
                foreach (var reference in GetConfiguredElementTypeReferences(dataType.ConfigurationObject))
                {
                    result.Add(new ElementTypeUsageDetail
                    {
                        ContentNodeId = dataType.Id,
                        ContentKey = dataType.Key,
                        ContentName = dataType.Name ?? "Unnamed data type",
                        ParentName = dataType.EditorAlias,
                        ContentPath = "Data Type configuration",
                        VersionDate = dataType.UpdateDate,
                        SourceType = reference.SourceType,
                        EntityType = "dataType",
                        ElementTypeKey = reference.ElementTypeKey
                    });
                }
            }

            return result;
        }

        internal static IEnumerable<ElementTypeConfigurationReference> GetConfiguredElementTypeReferences(object configuration)
        {
            IEnumerable<(Guid? ContentKey, Guid? SettingsKey)> blocks = configuration switch
            {
                BlockListConfiguration blockList => blockList.Blocks?.Select(x => ((Guid?)x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                BlockGridConfiguration blockGrid => blockGrid.Blocks?.Select(x => ((Guid?)x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                RichTextConfiguration richText => richText.Blocks?.Select(x => ((Guid?)x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                SingleBlockConfiguration singleBlock => singleBlock.Blocks?.Select(x => ((Guid?)x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                _ => []
            };

            foreach (var block in blocks)
            {
                if (block.ContentKey.HasValue)
                {
                    yield return new ElementTypeConfigurationReference(block.ContentKey.Value, "ConfigurationContent");
                }

                if (block.SettingsKey.HasValue)
                {
                    yield return new ElementTypeConfigurationReference(block.SettingsKey.Value, "ConfigurationSettings");
                }
            }
        }

        /// <summary>
        /// Finds Element Types occurring nested inside another block's Rich Text property — e.g. an
        /// inline block inserted into an RTE that is itself a property of a Block List/Grid/RTE
        /// block — which <see cref="BuildElementTypeUsageCte"/> cannot see because the entire nested
        /// tree is serialized into the single outer property's <c>textValue</c>, not a separate row.
        /// </summary>
        /// <remarks>
        /// Rather than recursively deserializing the block tree (unbounded depth, and awkward to do
        /// in SQL on both providers), this scans each candidate property's raw <c>textValue</c> text
        /// for the literal GUID of every Element Type known to be configured as an insertable inline
        /// block in some Rich Text data type (see <see cref="GetRiskyInlineElementTypeKeys"/> — in
        /// practice a small list). This works at any nesting depth because a GUID's characters are
        /// unaffected by however many layers of JSON string-escaping surround it — only the
        /// surrounding quotes/backslashes change per escaping level, never the 36 hex/hyphen
        /// characters of the GUID itself. A random GUID coincidentally appearing in unrelated text is
        /// negligibly unlikely (122 bits of randomness), so a plain case-insensitive substring count
        /// is reliable without parsing the JSON at all.
        /// <para>
        /// Occurrences already found by <see cref="BuildElementTypeUsageCte"/> as a top-level
        /// BlockContent/BlockSettings/ElementPicker/LibraryItem match on the same content node are
        /// subtracted so they aren't double-counted as "nested". This is scoped per content node,
        /// not per property, so it will under-count by one in the narrow case where the same risky
        /// Element Type is both nested in one property AND separately used top-level (or via
        /// ElementPicker/LibraryItem) in a different property on the very same node.
        /// </para>
        /// </remarks>
        private async Task<List<ElementTypeUsageDetail>> GetElementTypeNestedInlineBlockUsage(ElementTypeUsageStatus status)
        {
            var riskyKeys = await this.GetRiskyInlineElementTypeKeys();
            if (riskyKeys.Count == 0)
            {
                return [];
            }

            using var scope = this.scopeProvider.CreateScope(autoComplete: true);
            bool isSqlite = this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite;

            var rawSql = new Sql(@"
SELECT
    ucv.nodeId AS ContentNodeId,
    un.uniqueId AS ContentKey,
    ucv.[text] AS ContentName,
    up.[text] AS ParentName,
    un.[path] AS ContentPath,
    ucv.versionDate AS VersionDate,
    un.nodeObjectType AS ReferencingNodeObjectType,
    upd.textValue AS TextValue
FROM umbracoPropertyData upd
INNER JOIN umbracoContentVersion ucv ON ucv.id = upd.versionId
INNER JOIN umbracoNode un ON un.id = ucv.nodeId
LEFT JOIN umbracoNode up ON up.id = un.parentId
INNER JOIN cmsPropertyType cpt ON cpt.id = upd.propertyTypeId
INNER JOIN umbracoDataType udt ON udt.nodeId = cpt.dataTypeId
WHERE ucv.[current] = 1
AND udt.propertyEditorAlias IN ('Umbraco.BlockList', 'Umbraco.BlockGrid', 'Umbraco.RichText')
AND upd.textValue IS NOT NULL");

            var rows = scope.Database.Fetch<NestedScanRow>(rawSql);
            if (rows.Count == 0)
            {
                return [];
            }

            var topLevel = GetStoredElementTypeUsageKeysForKeys(scope.Database, status, isSqlite, riskyKeys);

            var result = new List<ElementTypeUsageDetail>();

            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.TextValue))
                {
                    continue;
                }

                foreach (var riskyKey in riskyKeys)
                {
                    int rawCount = CountOccurrences(row.TextValue, riskyKey);
                    if (rawCount == 0)
                    {
                        continue;
                    }

                    int alreadyCounted = topLevel[(row.ContentNodeId, riskyKey)].Count();
                    int nestedCount = rawCount - alreadyCounted;

                    for (int i = 0; i < nestedCount; i++)
                    {
                        result.Add(new ElementTypeUsageDetail
                        {
                            ContentNodeId = row.ContentNodeId,
                            ContentKey = row.ContentKey,
                            ContentName = row.ContentName,
                            ParentName = row.ParentName,
                            ContentPath = row.ContentPath,
                            VersionDate = row.VersionDate,
                            SourceType = "NestedInlineBlock",
                            EntityType = MapEntityType(row.ReferencingNodeObjectType),
                            ElementTypeKey = riskyKey
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the distinct set of Element Type keys configured as insertable inline blocks (content
        /// or settings) in any Rich Text data type — the candidate set for nested-usage scanning.
        /// </summary>
        private async Task<List<Guid>> GetRiskyInlineElementTypeKeys()
        {
            var dataTypes = await this.dataTypeService.GetAllAsync();

            return dataTypes
                .Where(dataType => dataType.EditorAlias == "Umbraco.RichText")
                .SelectMany(dataType => GetConfiguredElementTypeReferences(dataType.ConfigurationObject))
                .Select(reference => reference.ElementTypeKey)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets (ContentNodeId, ElementTypeKey) occurrences already found by
        /// <see cref="BuildElementTypeUsageCte"/>, restricted to the given keys, so
        /// <see cref="GetElementTypeNestedInlineBlockUsage"/> can subtract them from its raw text-scan
        /// counts and avoid double-counting non-nested usage as nested.
        /// </summary>
        private static ILookup<(int ContentNodeId, Guid ElementTypeKey), StoredUsageKeyRow> GetStoredElementTypeUsageKeysForKeys(
            IUmbracoDatabase database, ElementTypeUsageStatus status, bool isSqlite, IReadOnlyList<Guid> keys)
        {
            string placeholders = string.Join(", ", Enumerable.Range(0, keys.Count).Select(i => $"@{i}"));
            string predicate = isSqlite
                ? $"ElementTypeKey COLLATE NOCASE IN ({placeholders})"
                : $"ElementTypeKey IN ({placeholders})";

            var sql = new Sql(BuildElementTypeUsageCte(status.LibraryFeatureAvailable, isSqlite) + $@"
SELECT ContentNodeId, ElementTypeKey
FROM ElementTypeUsage
WHERE {predicate}",
                keys.Cast<object>().ToArray());

            return database.Fetch<StoredUsageKeyRow>(sql)
                .ToLookup(row => (row.ContentNodeId, row.ElementTypeKey));
        }

        /// <summary>
        /// Counts case-insensitive, non-overlapping occurrences of a GUID's literal text within
        /// <paramref name="text"/>. A plain substring scan is used instead of Regex: a GUID's
        /// hex/hyphen characters have no regex-special meaning, so there is nothing a Regex would
        /// buy here over <see cref="string.IndexOf(string, int, StringComparison)"/>, which is both
        /// simpler and avoids constructing a pattern per key.
        /// </summary>
        private static int CountOccurrences(string text, Guid key)
        {
            string needle = key.ToString();
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(needle, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }

        /// <summary>
        /// Maps an <c>umbracoNode.nodeObjectType</c> GUID to the client-facing entity type used to
        /// build edit links. Compares as parsed GUIDs (not raw strings) so it is unaffected by any
        /// casing differences between SQL Server and SQLite storage.
        /// </summary>
        private static string MapEntityType(string? nodeObjectType)
        {
            if (nodeObjectType is not null && Guid.TryParse(nodeObjectType, out var objectType))
            {
                if (objectType == Guid.Parse(Constants.ObjectTypes.Strings.Document)) return "content";
                if (objectType == Guid.Parse(Constants.ObjectTypes.Strings.Media)) return "media";
                if (objectType == Guid.Parse(Constants.ObjectTypes.Strings.Member)) return "member";
                if (objectType == Guid.Parse(ElementObjectTypeString)) return "element";
            }

            return "unknown";
        }

        private sealed class NestedScanRow
        {
            public int ContentNodeId { get; set; }
            public Guid ContentKey { get; set; } = Guid.Empty;
            public string ContentName { get; set; } = string.Empty;
            public string? ParentName { get; set; }
            public string ContentPath { get; set; } = string.Empty;
            public DateTime VersionDate { get; set; }
            public string? ReferencingNodeObjectType { get; set; }
            public string? TextValue { get; set; }
        }

        private sealed class StoredUsageKeyRow
        {
            public int ContentNodeId { get; set; }
            public Guid ElementTypeKey { get; set; }
        }

        /// <summary>
        /// Gets all Umbraco members, paginated, with optional filters
        /// </summary>
        /// <param name="page">The current page</param>
        /// <param name="itemsPerPage">How many results per page</param>
        /// <param name="groupId">Optional member group Id</param>
        /// <param name="search">Optional search term</param>
        /// <param name="orderBy">Column to order results by</param>
        /// <returns>A collection of members</returns>
        public Page<MemberModel> GetMembers(
            long page,
            long itemsPerPage,
            int? groupId = null,
            int? memberTypeId = null,
            bool? isApproved = null,
            bool? isLockedOut = null,
            bool? usesTwoFactor = null,
            string search = null,
            string orderBy = "MN.text")
        {
            var groupNamesSql = scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite
                ? @"SELECT group_concat(GN.text, ', ')
                    FROM cmsMember2MemberGroup MGM
                    INNER JOIN umbracoNode GN ON GN.id = MGM.MemberGroup
                    WHERE MGM.Member = M.nodeId"
                : @"SELECT STRING_AGG(CAST(GN.text AS nvarchar(max)), ', ') WITHIN GROUP (ORDER BY GN.text)
                    FROM cmsMember2MemberGroup MGM
                    INNER JOIN umbracoNode GN ON GN.id = MGM.MemberGroup
                    WHERE MGM.Member = M.nodeId";

            string sql = $@"SELECT M.nodeId as Id, M.LoginName as UserName, MN.text as Name, M.Email,
                CT.nodeId as MemberTypeId, CTN.text as MemberTypeName, CT.alias as MemberTypeAlias,
                COALESCE(({groupNamesSql}), '') as Groups,
                M.isApproved as IsApproved, M.isLockedOut as IsLockedOut,
                CASE WHEN EXISTS (SELECT 1 FROM umbracoTwoFactorLogin TFL WHERE TFL.userOrMemberKey = MN.uniqueId) THEN 1 ELSE 0 END as UsesTwoFactor,
                MN.createDate, MN.uniqueId as Udi
            FROM cmsMember M
            INNER JOIN umbracoNode MN ON M.nodeId = MN.id
            INNER JOIN umbracoContent C ON C.nodeId = M.nodeId
            INNER JOIN cmsContentType CT ON CT.nodeId = C.contentTypeId
            INNER JOIN umbracoNode CTN ON CTN.id = CT.nodeId
            WHERE 1 = 1";

            var memberQuery = new Sql(sql);

            if (groupId.HasValue)
            {
                memberQuery.Append(" AND EXISTS (SELECT 1 FROM cmsMember2MemberGroup MG WHERE MG.Member = M.nodeId AND MG.MemberGroup = @0)", groupId.Value);
            }

            if (memberTypeId.HasValue)
            {
                memberQuery.Append(" AND CT.nodeId = @0", memberTypeId.Value);
            }

            if (isApproved.HasValue)
            {
                memberQuery.Append(" AND M.isApproved = @0", isApproved.Value);
            }

            if (isLockedOut.HasValue)
            {
                memberQuery.Append(" AND M.isLockedOut = @0", isLockedOut.Value);
            }

            if (usesTwoFactor.HasValue)
            {
                memberQuery.Append(
                    usesTwoFactor.Value
                        ? " AND EXISTS (SELECT 1 FROM umbracoTwoFactorLogin TFL WHERE TFL.userOrMemberKey = MN.uniqueId)"
                        : " AND NOT EXISTS (SELECT 1 FROM umbracoTwoFactorLogin TFL WHERE TFL.userOrMemberKey = MN.uniqueId)");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                var likeSearch = "%" + trimmedSearch + "%";

                memberQuery.Append(
                    int.TryParse(trimmedSearch, out var searchId)
                        ? " AND (MN.text LIKE @0 OR M.Email LIKE @0 OR M.LoginName LIKE @0 OR MN.uniqueId LIKE @0 OR M.nodeId = @1)"
                        : " AND (MN.text LIKE @0 OR M.Email LIKE @0 OR M.LoginName LIKE @0 OR MN.uniqueId LIKE @0)",
                    likeSearch,
                    searchId);
            }

            memberQuery.OrderBy(string.IsNullOrWhiteSpace(orderBy) ? "MN.text" : orderBy);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Page<MemberModel>(page, itemsPerPage, memberQuery);
            }
        }

        /// <summary>
        /// Gets all Umbraco member groups
        /// </summary>
        /// <returns>A list of groups</returns>
        public IEnumerable<MemberGroupModel> GetMemberGroups()
        {
            var query = new Sql(@"SELECT GN.id as Id, GN.text as Name
                FROM umbracoNode GN
                WHERE GN.nodeObjectType = @0
                ORDER BY GN.text", Constants.ObjectTypes.Strings.MemberGroup);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<MemberGroupModel>(query);
            }
        }

        /// <summary>
        /// Gets all Umbraco member types
        /// </summary>
        /// <returns>A list of member types</returns>
        public IEnumerable<MemberGroupModel> GetMemberTypes()
        {
            var query = new Sql(@"SELECT CTN.id as Id, CTN.text as Name
                FROM umbracoNode CTN
                WHERE CTN.nodeObjectType = @0
                ORDER BY CTN.text", Constants.ObjectTypes.Strings.MemberType);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<MemberGroupModel>(query);
            }
        }

        /// <summary>
        /// Gets the database type
        /// </summary>
        /// <returns></returns>
        public DatabaseType GetDatabaseType() => this.scopeProvider.SqlContext.DatabaseType;

        /// <summary>
        /// Gets a list of registered servers
        /// </summary>
        public IEnumerable<ServerModel> GetRegistredServers()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                const string sql = @"SELECT Id, Address, ComputerName, RegisteredDate, LastNotifiedDate, IsActive, isSchedulingPublisher FROM umbracoServer";
                return scope.Database.Fetch<ServerModel>(sql);
            }
        }

        /// <summary>
        /// Gets the key values table
        /// </summary>
        public IEnumerable<UmbracoKeyValue> GetKeyValues()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                const string sql = @" SELECT [key], [value], Updated FROM umbracoKeyValue ORDER BY Updated";
                return scope.Database.Fetch<UmbracoKeyValue>(sql);
            }
        }

        public bool UpdateKeyValue(string key, string value)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var affected = scope.Database.Execute(
                    "UPDATE umbracoKeyValue SET [value] = @0, Updated = @1 WHERE [key] = @2",
                    value,
                    DateTime.UtcNow,
                    key);

                return affected > 0;
            }
        }

        public bool CreateKeyValue(string key, string value)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var affected = scope.Database.Execute(
                    "INSERT INTO umbracoKeyValue ([key], [value], Updated) VALUES (@0, @1, @2)",
                    key,
                    value,
                    DateTime.UtcNow);

                return affected > 0;
            }
        }

        public bool DeleteKeyValue(string key)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Execute("DELETE FROM umbracoKeyValue WHERE [key] = @0", key) > 0;
            }
        }

        /// <summary>
        /// Gets a single nu cache item by Node Id
        /// </summary>
        /// <param name="id">The node Id</param>
        public NuCacheItem GetNuCacheItem(int id)
        {
            var query = new Sql("SELECT nodeId as Id, data as Data, text as Title, createDate as CreateDate FROM cmsContentNu Nu INNER JOIN umbracoNode N on Nu.nodeId = N.id WHERE Nu.published = 1 AND NodeId = @0", id);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.FirstOrDefault<NuCacheItem>(query) ?? new NuCacheItem();
            }
        }

        /// <summary>
        /// Deletes a tag from the database
        /// </summary>
        /// <param name="id">The tag ID</param>
        /// <returns>True if deleted; false if not</returns>
        public bool DeleteTag(int id)
        {
            int success = 0;

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                success += scope.Database.Execute(new Sql("DELETE FROM cmsTagRelationship WHERE tagId = @0", id));

                success += scope.Database.Execute(new Sql("DELETE FROM cmsTags WHERE id = @0", id));
            }

            return success > 0;
        }

        /// <summary>
        /// Gets all tags that are not associated with any content
        /// </summary>
        /// <returns>A list of tags</returns>
        public List<Tag> GetOrphanedTags()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<Diplo.GodMode.Models.Tag>("SELECT T.Id, T.[Group], T.Tag as Text, L.languageISOCode as Culture FROM cmsTags T LEFT JOIN umbracoLanguage L ON T.languageId = L.id  WHERE T.id NOT IN (SELECT tagId FROM cmsTagRelationship)");
            }
        }

        public long GetOrphanedMediaCount()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.ExecuteScalar<long>(
                    "SELECT COUNT(*) FROM umbracoNode N WHERE N.nodeObjectType = @0 AND N.trashed = 0 AND N.id NOT IN (SELECT DISTINCT childId FROM umbracoRelation WHERE childId IS NOT NULL)",
                    Constants.ObjectTypes.Media);
            }
        }

        public List<MediaMap> GetOrphanedMedia()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Fetch<MediaMap>(
                    "SELECT N.id as Id, N.uniqueId as Udi, N.text as Name, N.path as Path FROM umbracoNode N WHERE N.nodeObjectType = @0 AND N.trashed = 0 AND N.id NOT IN (SELECT DISTINCT childId FROM umbracoRelation WHERE childId IS NOT NULL) ORDER BY N.text",
                    Constants.ObjectTypes.Media);
            }
        }

        public long GetLogRowCount()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                try
                {
                    return scope.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM umbracoLog");
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not read umbracoLog row count; trying umbLog.");
                    return scope.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM umbLog");
                }
            }
        }

        public int DeleteLogRows(DateTimeOffset? olderThan = null)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var table = GetTableNames(scope.Database)
                    .FirstOrDefault(x => x.Name.InvariantEquals("umbracoLog"))
                    ?? GetTableNames(scope.Database).FirstOrDefault(x => x.Name.InvariantEquals("umbLog"));

                if (table is null)
                {
                    logger.LogDebug("Could not delete Umbraco log rows because no umbracoLog or umbLog table exists.");
                    return 0;
                }

                var tableName = QuoteTableName(table.Schema, table.Name);

                if (olderThan is null)
                {
                    return scope.Database.Execute($"DELETE FROM {tableName}");
                }

                var columns = GetColumns(scope.Database, table.Schema, table.Name).ToList();
                var dateColumn = GetLogDateColumn(columns);
                if (dateColumn is null)
                {
                    logger.LogWarning("Could not delete Umbraco log rows older than {Cutoff} because {TableName} has no known date column.", olderThan, table.Name);
                    return 0;
                }

                object cutoff = this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite
                    ? olderThan.Value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    : olderThan.Value.UtcDateTime;

                return scope.Database.Execute($"DELETE FROM {tableName} WHERE {QuoteIdentifier(dateColumn)} < @0", cutoff);
            }
        }

        public long GetContentVersionCount()
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.ExecuteScalar<long>("SELECT COUNT(*) FROM umbracoContentVersion");
            }
        }

        public long GetContentWithExcessiveVersionsCount(int versionThreshold)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.ExecuteScalar<long>(
                    @"SELECT COUNT(*) FROM (
                        SELECT nodeId
                        FROM umbracoContentVersion
                        GROUP BY nodeId
                        HAVING COUNT(*) > @0
                    ) VersionedContent",
                    versionThreshold);
            }
        }

        public IEnumerable<DatabaseHealthRow> GetDatabaseHealthRows()
        {
            return
            [
                CountTableRows("Logs", "umbracoLog", "umbLog"),
                CountTableRows("Content versions", "umbracoContentVersion"),
                CountTableRows("Audit", "umbracoAudit", "umbracoAuditEntry"),
                CountTableRows("Key/value", "umbracoKeyValue"),
                CountTableRows("Locks", "umbracoLock")
            ];
        }

        public IEnumerable<DatabaseHealthRow> GetCacheHealthRows()
        {
            return
            [
                CountTableRows("Persisted content cache", "cmsContentNu"),
                CountTableRows("Cache instructions", "umbracoCacheInstruction"),
                CountTableRows("Repository cache versions", "umbracoRepositoryCacheVersion"),
                CountTableRows("Last synced markers", "umbracoLastSynced"),
                CountTableRows("Registered servers", "umbracoServer")
            ];
        }

        public IEnumerable<DatabaseTableInfo> GetDatabaseTables()
        {
            if (this.memoryCache.TryGetValue(DatabaseTablesCacheKey, out IReadOnlyList<DatabaseTableInfo>? cached) && cached is not null)
            {
                return cached;
            }

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var tables = GetTableNames(scope.Database).ToList();

                var tableInfo = tables
                    .Select(table =>
                    {
                        var info = CreateTableInfo(table.Schema, table.Name);
                        try
                        {
                            info.RowCount = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {QuoteTableName(table.Schema, table.Name)}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(ex, "Could not count rows in {TableName}", table.Name);
                            info.CountSucceeded = false;
                            info.Warning = "Could not count rows";
                        }

                        ApplyRowCountWarning(info);
                        return info;
                    })
                    .OrderByDescending(x => x.RowCount)
                    .ThenBy(x => x.Name)
                    .ToList();

                this.memoryCache.Set(DatabaseTablesCacheKey, tableInfo, TimeSpan.FromMinutes(1));
                return tableInfo;
            }
        }

        public DatabaseTableDetail? GetDatabaseTableDetail(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var table = GetTableNames(scope.Database)
                    .FirstOrDefault(x => x.Name.InvariantEquals(tableName) || $"{x.Schema}.{x.Name}".InvariantEquals(tableName));

                if (table is null)
                {
                    return null;
                }

                var baseInfo = CreateTableInfo(table.Schema, table.Name);
                try
                {
                    baseInfo.RowCount = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {QuoteTableName(table.Schema, table.Name)}");
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not count rows in {TableName}", table.Name);
                    baseInfo.CountSucceeded = false;
                    baseInfo.Warning = "Could not count rows";
                }

                ApplyRowCountWarning(baseInfo);
                var relationships = GetRelationships(scope.Database).ToList();

                return new DatabaseTableDetail
                {
                    Name = baseInfo.Name,
                    Schema = baseInfo.Schema,
                    Category = baseInfo.Category,
                    Purpose = baseInfo.Purpose,
                    RowCount = baseInfo.RowCount,
                    CountSucceeded = baseInfo.CountSucceeded,
                    Warning = baseInfo.Warning,
                    Columns = GetColumns(scope.Database, table.Schema, table.Name).OrderBy(x => x.Ordinal).ToList(),
                    OutgoingRelationships = relationships
                        .Where(x => x.FromSchema.InvariantEquals(table.Schema) && x.FromTable.InvariantEquals(table.Name))
                        .OrderBy(x => x.FromColumn)
                        .ToList(),
                    IncomingRelationships = relationships
                        .Where(x => x.ToSchema.InvariantEquals(table.Schema) && x.ToTable.InvariantEquals(table.Name))
                        .OrderBy(x => x.FromTable)
                        .ThenBy(x => x.FromColumn)
                        .ToList()
                };
            }
        }

        public DatabaseTableRows? GetDatabaseTableRows(string tableName, long page = 1, long pageSize = 25)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return null;
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                var table = GetTableNames(scope.Database)
                    .FirstOrDefault(x => x.Name.InvariantEquals(tableName) || $"{x.Schema}.{x.Name}".InvariantEquals(tableName));

                if (table is null)
                {
                    return null;
                }

                var columns = GetColumns(scope.Database, table.Schema, table.Name)
                    .OrderBy(x => x.Ordinal)
                    .ToList();

                if (columns.Count == 0)
                {
                    return new DatabaseTableRows
                    {
                        Columns = columns,
                        CurrentPage = page,
                        ItemsPerPage = pageSize,
                        TotalItems = 0,
                        TotalPages = 0,
                        Items = []
                    };
                }

                var totalItems = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {QuoteTableName(table.Schema, table.Name)}");
                var totalPages = totalItems == 0 ? 0 : (long)Math.Ceiling(totalItems / (double)pageSize);
                page = totalPages == 0 ? 1 : Math.Min(page, totalPages);

                var sql = BuildPagedRowsSql(table.Schema, table.Name, columns, page, pageSize);
                var rows = scope.Database.Fetch<dynamic>(sql)
                    .Select(row => (IDictionary<string, object?>)NormalizeDatabaseRow(row, columns))
                    .ToList();

                return new DatabaseTableRows
                {
                    Columns = columns,
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = rows
                };
            }
        }

        private DatabaseHealthRow CountTableRows(string label, params string[] tableNames)
        {
            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                foreach (var tableName in tableNames)
                {
                    try
                    {
                        return new DatabaseHealthRow
                        {
                            Label = label,
                            Table = tableName,
                            Count = scope.Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM {tableName}"),
                            Exists = true
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Could not count rows in {TableName}", tableName);
                    }
                }
            }

            return new DatabaseHealthRow
            {
                Label = label,
                Table = string.Join(" / ", tableNames),
                Exists = false
            };
        }

        private List<DatabaseTableName> GetTableNames(IUmbracoDatabase database)
        {
            if (this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite)
            {
                return database.Fetch<DatabaseTableName>(
                    "SELECT '' AS [Schema], name AS [Name] FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
            }

            return database.Fetch<DatabaseTableName>(
                @"SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name]
                  FROM INFORMATION_SCHEMA.TABLES
                  WHERE TABLE_TYPE = 'BASE TABLE'
                  ORDER BY TABLE_SCHEMA, TABLE_NAME");
        }

        private IEnumerable<DatabaseColumnInfo> GetColumns(IUmbracoDatabase database, string schema, string tableName)
        {
            if (this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite)
            {
                return database.Fetch<SQLiteColumnInfo>(
                    @"SELECT
                        cid AS Cid,
                        name AS Name,
                        type AS Type,
                        [notnull] AS [NotNull],
                        pk AS Pk
                      FROM pragma_table_info(@0)",
                    tableName)
                    .Select(column => new DatabaseColumnInfo
                    {
                        Name = column.Name,
                        DataType = column.Type,
                        Nullable = column.NotNull == 0,
                        PrimaryKey = column.Pk > 0,
                        Ordinal = column.Cid
                    });
            }

            return database.Fetch<DatabaseColumnInfo>(
                @"SELECT
                    C.COLUMN_NAME AS Name,
                    C.DATA_TYPE AS DataType,
                    CASE WHEN C.CHARACTER_MAXIMUM_LENGTH < 0 THEN NULL ELSE C.CHARACTER_MAXIMUM_LENGTH END AS MaxLength,
                    CASE WHEN C.IS_NULLABLE = 'YES' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS Nullable,
                    CASE WHEN KCU.COLUMN_NAME IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS PrimaryKey,
                    C.ORDINAL_POSITION AS Ordinal
                  FROM INFORMATION_SCHEMA.COLUMNS C
                  LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
                    ON TC.TABLE_SCHEMA = C.TABLE_SCHEMA
                    AND TC.TABLE_NAME = C.TABLE_NAME
                    AND TC.CONSTRAINT_TYPE = 'PRIMARY KEY'
                  LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU
                    ON KCU.CONSTRAINT_SCHEMA = TC.CONSTRAINT_SCHEMA
                    AND KCU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
                    AND KCU.TABLE_SCHEMA = C.TABLE_SCHEMA
                    AND KCU.TABLE_NAME = C.TABLE_NAME
                    AND KCU.COLUMN_NAME = C.COLUMN_NAME
                  WHERE C.TABLE_SCHEMA = @0 AND C.TABLE_NAME = @1
                  ORDER BY C.ORDINAL_POSITION",
                schema,
                tableName);
        }

        private IEnumerable<DatabaseRelationshipInfo> GetRelationships(IUmbracoDatabase database)
        {
            if (this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite)
            {
                return GetTableNames(database)
                    .SelectMany(table => database.Fetch<SQLiteForeignKeyInfo>(
                        @"SELECT
                            id AS Id,
                            [table] AS [Table],
                            [from] AS [From],
                            [to] AS [To]
                          FROM pragma_foreign_key_list(@0)",
                        table.Name)
                        .Select(foreignKey => new DatabaseRelationshipInfo
                        {
                            ConstraintName = $"FK_{table.Name}_{foreignKey.Table}_{foreignKey.Id}",
                            FromSchema = table.Schema,
                            FromTable = table.Name,
                            FromColumn = foreignKey.From,
                            ToSchema = table.Schema,
                            ToTable = foreignKey.Table,
                            ToColumn = foreignKey.To
                        }));
            }

            return database.Fetch<DatabaseRelationshipInfo>(
                @"SELECT
                    RC.CONSTRAINT_NAME AS ConstraintName,
                    FKCU.TABLE_SCHEMA AS FromSchema,
                    FKCU.TABLE_NAME AS FromTable,
                    FKCU.COLUMN_NAME AS FromColumn,
                    PKCU.TABLE_SCHEMA AS ToSchema,
                    PKCU.TABLE_NAME AS ToTable,
                    PKCU.COLUMN_NAME AS ToColumn
                  FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
                  INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE FKCU
                    ON FKCU.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA
                    AND FKCU.CONSTRAINT_NAME = RC.CONSTRAINT_NAME
                  INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE PKCU
                    ON PKCU.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA
                    AND PKCU.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME
                    AND PKCU.ORDINAL_POSITION = FKCU.ORDINAL_POSITION
                  ORDER BY FKCU.TABLE_NAME, FKCU.COLUMN_NAME");
        }

        private static string? GetDomainLanguagePredicate(IEnumerable<DatabaseColumnInfo> columns)
        {
            var columnNames = columns.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (columnNames.Contains("domainDefaultLanguage"))
            {
                return "D.domainDefaultLanguage = L.id";
            }

            if (columnNames.Contains("languageId"))
            {
                return "D.languageId = L.id";
            }

            if (columnNames.Contains("languageIsoCode"))
            {
                return "D.languageIsoCode = L.languageISOCode";
            }

            return null;
        }

        private static string? GetLogDateColumn(IEnumerable<DatabaseColumnInfo> columns)
        {
            var columnNames = columns.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in new[] { "Datestamp", "DateStamp", "Timestamp", "TimeStamp", "LogDate", "Date", "CreateDate" })
            {
                if (columnNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private DatabaseTableInfo CreateTableInfo(string schema, string tableName)
        {
            var category = GetTableCategory(tableName);

            return new DatabaseTableInfo
            {
                Name = tableName,
                Schema = schema,
                Category = category,
                Purpose = GetTablePurpose(tableName, category)
            };
        }

        private static string GetTableCategory(string tableName)
        {
            var name = tableName.ToLowerInvariant();

            return name switch
            {
                // =========================
                // Infrastructure
                // =========================
                "__efmigrationshistory" or
                "__efmigrationslock" or
                "__umbracoaimigrationshistory" or
                "umbracokeyvalue" or
                "umbracolock" or
                "umbracodistributedjob" or
                "umbracoserver" or
                "umbracolongrunningoperation" or
                "sqlite_sequence"
                    => "Infrastructure",

                // =========================
                // Content & Publishing
                // =========================
                "umbraconode" or
                "umbracocontent" or
                "umbracocontentversion" or
                "umbracocontentversionculturevariation" or
                "umbracocontentversioncleanuppolicy" or
                "umbracodocument" or
                "umbracodocumentversion" or
                "umbracodocumentculturevariation" or
                "umbracocontentschedule" or
                "umbracodocumenturl" or
                "umbracodocumenturlalias" or
                "umbracoredirecturl"
                    => "Content & Publishing",

                // =========================
                // Media
                // =========================
                "umbracomediaversion"
                    => "Media",

                // =========================
                // Cache / NuCache
                // =========================
                "cmscontentnu" or
                "umbracocacheinstruction" or
                "umbracorepositorycacheversion" or
                "umbracolastsynced"
                    => "Caching & NuCache",

                // =========================
                // Content Type Schema
                // =========================
                "cmscontenttype" or
                "cmscontenttype2contenttype" or
                "cmscontenttypeallowedcontenttype" or
                "cmspropertytype" or
                "cmspropertytypegroup" or
                "umbracodatatype"
                    => "Content Type Schema",

                // =========================
                // Property Data
                // =========================
                "umbracopropertydata"
                    => "Property Data",

                // =========================
                // Rendering
                // =========================
                "cmstemplate"
                    => "Rendering & Templates",

                // =========================
                // Localization
                // =========================
                "cmsdictionary" or
                "cmslanguagetext" or
                "umbracolanguage" or
                "umbracodomain"
                    => "Localization",

                // =========================
                // Members
                // =========================
                "cmsmember" or
                "cmsmembertype" or
                "cmsmember2membergroup"
                    => "Members",

                // =========================
                // Users & Security
                // =========================
                "umbracouser" or
                "umbracousergroup" or
                "umbracouser2usergroup" or
                "umbracouserlogin" or
                "umbracouserstartnode" or
                "umbracouser2nodenotify" or
                "umbracouser2clientid" or
                "umbracouserdata" or
                "umbracotwofactorlogin" or
                "umbracoexternallogin" or
                "umbracoexternallogintoken" or
                "umbracoaccess" or
                "umbracoaccessrule"
                    => "Users & Security",

                // =========================
                // Authentication
                // =========================
                "umbracoopenid" or
                "umbracoopeniddictapplications" or
                "umbracoopeniddictauthorizations" or
                "umbracoopeniddicttokens" or
                "umbracoopeniddictscopes"
                    => "Authentication",

                // =========================
                // Permissions
                // =========================
                "umbracouser2nodepermission" or
                "umbracousergroup2nodepermission" or
                "umbracousergroup2permission" or
                "umbracousergroup2granularpermission" or
                "umbracousergroup2app" or
                "umbracousergroup2language"
                    => "Permissions",

                // =========================
                // Relations & Tagging
                // =========================
                "cmstags" or
                "cmstagrelationship" or
                "umbracorelation" or
                "umbracorelationtype"
                    => "Relations & Tagging",

                // =========================
                // Audit & Logging
                // =========================
                "umbracoaudit" or
                "umbracoauditentry" or
                "umbracolog" or
                "umbracologviewerquery" or
                "umblog"
                    => "Audit & Logging",

                // =========================
                // Privacy
                // =========================
                "umbracoconsent"
                    => "Privacy & Consent",

                // =========================
                // Packages
                // =========================
                "umbracocreatedpackageschema" or
                "umbracopackagemigration" or
                "umbracopackagemigrationplan"
                    => "Packages",

                // =========================
                // Prefix-based categories
                // =========================
                _ when name.StartsWith("uf")
                    => "Forms",

                _ when name.StartsWith("umbracoforms")
                    => "Forms",

                _ when name.StartsWith("umbracoai")
                    => "AI",

                _ when name.StartsWith("umbracowebhook")
                    => "Webhooks",

                _ when name.StartsWith("cms")
                    => "CMS",

                _ when name.StartsWith("umbraco")
                    => "Umbraco",

                _ => "Custom / Package"
            };
        }

        private static string GetTablePurpose(string tableName, string category)
        {
            var name = tableName.ToLowerInvariant();

            return name switch
            {
                // Migrations / locking
                "__efmigrationshistory" => "Entity Framework Core migration history table used by EF-based packages or features.",
                "__efmigrationslock" => "Entity Framework Core migration lock table used to prevent concurrent EF migrations.",
                "__umbracoaimigrationshistory" => "Migration history for the Umbraco AI package.",
                "umbracokeyvalue" => "Key/value storage used for migrations, runtime state, locks, and package state.",
                "umbracolock" => "Distributed lock rows used to coordinate operations across servers.",
                "umbracodistributedlock" => "Distributed lock rows used to coordinate operations across servers.",
                "umbracodistributedjob" => "Queued distributed jobs used to sync work across multiple Umbraco servers.",
                "umbracoserver" => "Registered Umbraco server instances in a load-balanced or distributed setup.",

                // Core node/content model
                "umbraconode" => "Shared tree node metadata for content, media, members, types, data types, templates, and other entities.",
                "umbracocontent" => "Base content rows for documents, media, and members linked to content types.",
                "umbracocontentversion" => "Version records for content, media, and member data.",
                "umbracocontentversionculturevariation" => "Culture-specific names and values associated with content versions.",
                "umbracocontentversioncleanuppolicy" => "Version cleanup policy settings for content types.",
                "umbracocontentnu" => "NuCache persisted content cache data used to rebuild the published snapshot/cache.",
                "umbracodocument" => "Document-specific publishing state for content nodes.",
                "umbracodocumentversion" => "Document version publishing metadata, including published and edited state.",
                "umbracodocumentculturevariation" => "Culture-specific document publish/edit state and names.",
                "umbracodocumenturl" => "Generated and stored document URLs for routing and redirects.",
                "umbracodocumenturlalias" => "Custom URL aliases for documents.",
                "umbracocontentschedule" => "Scheduled publish and unpublish records for content.",

                // Content type / property system
                "cmscontenttype" => "Document, media, member, and element type definitions.",
                "cmscontenttype2contenttype" => "Parent/child structure rules between content types.",
                "cmscontenttypeallowedcontenttype" => "Allowed child content type rules for document type structure.",
                "cmspropertytype" => "Property definitions for content, media, member, and element types.",
                "cmspropertytypegroup" => "Property group/tab definitions used by content types.",
                "umbracodatatype" => "Data type definitions and property editor configuration.",
                "umbracopropertydata" => "Stored property values for content, media, and members.",
                "cmsdictionary" => "Dictionary item definitions for localization.",
                "cmslanguagetext" => "Localized dictionary values for each language.",
                "umbracolanguage" => "Configured languages/cultures for the site.",
                "umbracodomain" => "Hostnames and culture/domain bindings.",

                // Templates / rendering
                "cmstemplate" => "Template definitions used for rendering document types.",

                // Members
                "cmsmember" => "Member account records linked to Umbraco content rows.",
                "cmsmembertype" => "Member type definitions.",
                "cmsmember2membergroup" => "Member-to-member-group assignments.",
                "umbracoaccess" => "Protected content access rules.",
                "umbracoaccessrule" => "Individual public access rules for protected content.",

                // Tags
                "cmstags" => "Tag definitions.",
                "cmstagrelationship" => "Tag assignments to content, media, or members.",

                // Users / security / audit
                "umbracouser" => "Backoffice user accounts.",
                "umbracoUserData" => "Backoffice user data",
                "umbracoUser2NodeNotify" => "Links backoffice users with nodes that are watching",
                "umbracoTwoFactorLogin" => "2FA login data for backoffice users",
                "umbracoExternalLogin" => "External Umbraco login user accounts",
                "umbracousergroup" => "Backoffice user groups.",
                "umbracouser2usergroup" => "Backoffice user-to-group assignments.",
                "umbracouserlogin" => "Backoffice user login records.",
                "umbracouserstartnode" => "Configured start nodes for backoffice users.",
                "umbracouser2nodepermission" => "Per-node permissions assigned to backoffice users.",
                "umbracousergroup2nodepermission" => "Per-node permissions assigned to backoffice user groups.",
                "umbracousergroup2app" => "Section/app access assigned to backoffice user groups.",
                "umbracousergroup2language" => "Language permissions assigned to backoffice user groups.",
                "umbracoexternallogin" => "External login providers linked to backoffice users.",
                "umbracoexternallogintoken" => "Tokens associated with external login providers.",
                "umbracoaudit" => "Audit trail records for backoffice and content actions.",
                "umbracoauditentry" => "Audit trail entries for backoffice and content actions.",
                "umbracolog" or "umblog" => "Backoffice log entries where present.",
                "umbracoconsent" => "Consent records, commonly used for privacy/GDPR-related consent state.",
                "umbracologviewerquery" => "Saved queries for the backoffice serilog viewer",
                "umbracoUsergroup2granularoermission" => "Maps user groups with specific permissions",
                "umbracolongrunningpperation" => "Stores data about long running background operations",

                "umbracouserdata" => "Additional per-user backoffice data and preferences.",
                "umbracouser2nodenotify" => "Notification subscriptions linking backoffice users to content nodes.",
                "umbracotwofactorlogin" => "Two-factor authentication login data for backoffice users and members.",
                "umbracousergroup2granularpermission" => "Granular backoffice permissions assigned to user groups.",
                "umbracolongrunningoperation" => "Tracks long-running backoffice or server operations and their status.",

                // Relations / redirects / cache
                "umbracorelation" => "Relations between Umbraco entities, including content, media, members, and custom relation types.",
                "umbracorelationtype" => "Definitions of relation types used by Umbraco relations.",
                "umbracoredirecturl" => "Stored URL redirects created when document URLs change.",
                "umbracocacheinstruction" => "Cache invalidation instructions used by distributed/runtime cache refresh logic.",
                "umbracolastsynced" => "Tracks last synced IDs/timestamps for database-to-cache synchronization.",

                // Packages
                "umbracocreatedpackageschema" => "Schema metadata for packages created from the backoffice/package system.",
                "umbracopackagemigrationplan" => "Package migration plan state.",
                "umbracopackagemigration" => "Package migration state.",

                // Umbraco AI package
                var x when x.StartsWith("umbracoai") => "Table used by the Umbraco AI package for AI configuration, profiles, guardrails, usage, tests, context, or transcripts.",

                // WebHooks
                var x when x.StartsWith("umbracowebhook") => "Umbraco webhook related data",

                "umbracousergroup2permission"
                => "Backoffice section, tree, or action permissions assigned to user groups.",

                "umbracouser2clientid"
                    => "Maps backoffice users to client identifiers used by authentication/session infrastructure.",

                "umbracomediaversion"
                    => "Media-specific version metadata linked to media content versions.",

                "umbracoopeniddictapplications"
                    => "OAuth/OpenID Connect client application registrations used by OpenIddict.",

                "umbracoopeniddicttokens"
                    => "OAuth/OpenID Connect tokens issued or tracked by OpenIddict.",

                "umbracoopeniddictscopes"
                    => "OAuth/OpenID Connect scope definitions used by OpenIddict.",

                "umbracorepositorycacheversion"
                    => "Tracks repository cache version state used to invalidate or refresh cached repository data.",

                // Fallbacks
                var x when x.StartsWith("cms") => "Umbraco CMS platform table, usually part of the core content, member, type, tag, or template schema.",
                var x when x.StartsWith("umbraco") => "Umbraco platform table used by core CMS services, backoffice, runtime, cache, security, or packages.",
                var x when x.StartsWith("__ef") => "Entity Framework Core infrastructure table.",
                _ => category == "Custom / Package"
                    ? "Package or custom application table."
                    : "Umbraco platform table."
            };
        }

        private static void ApplyRowCountWarning(DatabaseTableInfo info)
        {
            if (!info.CountSucceeded)
            {
                return;
            }

            if (info.RowCount >= 100000)
            {
                info.Warning = "Large table";
            }
            else if (info.Name.InvariantEquals("umbracoContentVersion") && info.RowCount >= 10000)
            {
                info.Warning = "Many content versions";
            }
            else if ((info.Name.InvariantEquals("umbracoLog") || info.Name.InvariantEquals("umbLog")) && info.RowCount >= 50000)
            {
                info.Warning = "Large log table";
            }
        }

        private static string QuoteTableName(string schema, string tableName)
            => string.IsNullOrWhiteSpace(schema)
                ? QuoteIdentifier(tableName)
                : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}";

        private static string QuoteIdentifier(string identifier)
            => $"[{identifier.Replace("]", "]]")}]";

        private string BuildPagedRowsSql(string schema, string tableName, IReadOnlyList<DatabaseColumnInfo> columns, long page, long pageSize)
        {
            var offset = (page - 1) * pageSize;
            var table = QuoteTableName(schema, tableName);
            var orderBy = BuildRowsOrderBy(columns);

            if (this.scopeProvider.SqlContext.DatabaseType == DatabaseType.SQLite)
            {
                return $"SELECT * FROM {table} ORDER BY {orderBy} LIMIT {pageSize} OFFSET {offset}";
            }

            return $"SELECT * FROM {table} ORDER BY {orderBy} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }

        private static string BuildRowsOrderBy(IReadOnlyList<DatabaseColumnInfo> columns)
        {
            var orderColumns = columns
                .Where(x => x.PrimaryKey)
                .DefaultIfEmpty(columns.FirstOrDefault(IsOrderableColumn))
                .Where(x => x is not null)
                .Select(x => QuoteIdentifier(x!.Name));

            var orderBy = string.Join(", ", orderColumns);
            return string.IsNullOrWhiteSpace(orderBy) ? "(SELECT NULL)" : orderBy;
        }

        private static bool IsOrderableColumn(DatabaseColumnInfo column)
        {
            var dataType = column.DataType.ToLowerInvariant();
            return dataType is not "text" and not "ntext" and not "image" and not "xml" and not "binary" and not "varbinary";
        }

        private static IDictionary<string, object?> NormalizeDatabaseRow(object row, IEnumerable<DatabaseColumnInfo> columns)
        {
            var values = row as IDictionary<string, object?> ?? new Dictionary<string, object?>();

            return columns.ToDictionary(
                column => column.Name,
                column => values.TryGetValue(column.Name, out var value) ? NormalizeDatabaseValue(column.Name, value) : null);
        }

        private static object? NormalizeDatabaseValue(string columnName, object? value)
        {
            if (value is null || value is DBNull)
            {
                return null;
            }

            if (IsSensitiveColumn(columnName))
            {
                return "[redacted]";
            }

            if (value is byte[] bytes)
            {
                return $"[binary: {bytes.Length:n0} bytes]";
            }

            if (value is string text && text.Length > 1000)
            {
                return text[..1000] + "...";
            }

            return value;
        }

        private static bool IsSensitiveColumn(string columnName)
        {
            var name = columnName.ToLowerInvariant();
            return name.Contains("password")
                || name.Contains("secret")
                || name.Contains("token")
                || name.Contains("apikey")
                || name.Contains("api_key")
                || name.Contains("securitystamp");
        }

        private sealed class DatabaseTableName
        {
            public string Schema { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;
        }

        private sealed class SQLiteColumnInfo
        {
            public int Cid { get; set; }

            public string Name { get; set; } = string.Empty;

            public string Type { get; set; } = string.Empty;

            public int NotNull { get; set; }

            public int Pk { get; set; }
        }

        private sealed class SQLiteForeignKeyInfo
        {
            public int Id { get; set; }

            public string Table { get; set; } = string.Empty;

            public string From { get; set; } = string.Empty;

            public string To { get; set; } = string.Empty;
        }
    }
}
