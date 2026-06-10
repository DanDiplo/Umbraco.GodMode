using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
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
        private readonly IScopeProvider scopeProvider;
        private readonly ILogger<UmbracoDatabaseService> logger;
        private readonly IPublishedContentQuery contentQuery;
        private readonly IMemoryCache memoryCache;

        public UmbracoDatabaseService(IScopeProvider scopeProvider, IPublishedContentQuery contentQuery, ILogger<UmbracoDatabaseService> logger, IMemoryCache memoryCache)
        {
            this.scopeProvider = scopeProvider;
            this.contentQuery = contentQuery;
            this.logger = logger;
            this.memoryCache = memoryCache;
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
