using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NPoco;
using System;
using System.Collections.Generic;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Some old-fashioned SQL to get stuff in an efficient way that you can't easily get through APIs
    /// </summary>
    public class UmbracoDatabaseService : IUmbracoDatabaseService
    {
        private readonly IScopeProvider scopeProvider;
        private readonly ILogger<UmbracoDatabaseService> logger;
        private readonly IPublishedContentQuery contentQuery;

        public UmbracoDatabaseService(IScopeProvider scopeProvider, IPublishedContentQuery contentQuery, ILogger<UmbracoDatabaseService> logger)
        {
            this.scopeProvider = scopeProvider;
            this.contentQuery = contentQuery;
            this.logger = logger;
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
            var sql = @"SELECT N.uniqueID as Udi, N.Id, N.ParentId, N.Level, CT.icon, N.Trashed as Trashed, CT.alias, N.Text as Name, 
                N.Path as Path, N.createDate, Creator.Id AS CreatorId, Creator.userName as CreatorName,
                V.versionDate as UpdateDate, Updater.Id as UpdaterID, Updater.userName as UpdaterName,
				Lang.languageISOCode As Culture
                FROM umbracoContent C
                INNER JOIN umbracoNode N ON N.Id = C.nodeId
                INNER JOIN cmsContentType CT ON C.contentTypeId = CT.nodeId
                INNER JOIN umbracoDocument D ON D.nodeId = C.nodeId
                INNER JOIN umbracoContentVersion As V ON V.nodeId = N.id
                INNER JOIN umbracoUser AS Creator ON Creator.Id = N.nodeUser
                INNER JOIN umbracoUser As Updater ON V.userId = Updater.id
				LEFT JOIN umbracoDocumentCultureVariation DCV ON DCV.nodeId = D.nodeId
				LEFT JOIN umbracoLanguage Lang ON Lang.id = DCV.languageId
                WHERE V.[current] = 1  ";

            var query = new Sql(sql);

            if (criteria != null)
            {
                if (!string.IsNullOrEmpty(criteria.Alias))
                {
                    query = query.Append(" AND CT.alias = @0", criteria.Alias);
                }

                if (!string.IsNullOrEmpty(criteria.Name))
                {
                    query = query.Append(" AND N.text LIKE @0", "%" + criteria.Name + "%");
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
                        query = query.Append(" AND Lang.Id IS NULL", criteria.LanguageId.Value);
                    }
                    else
                    {
                        query = query.Append(" AND Lang.Id = @0", criteria.LanguageId.Value);
                    }
                }
            }

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
            LEFT JOIN umbracoContent C ON C.contentTypeId = CT.nodeId
            LEFT JOIN umbracoNode N ON C.nodeId = N.id  ";

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
        public Page<MemberModel> GetMembers(long page, long itemsPerPage, int? groupId = null, string search = null, string orderBy = "MN.text")
        {
            string sql = @"SELECT M.nodeId as Id, M.LoginName as UserName, MN.text as Name, M.Email, MN.createDate, MN.uniqueId as Udi
            FROM cmsMember M 
            INNER JOIN umbracoNode MN ON M.nodeId = MN.id ";

            var memberQuery = new Sql(sql);

            if (groupId.HasValue)
            {
                memberQuery.Append(" LEFT JOIN cmsMember2MemberGroup MG ON MG.Member = M.nodeId WHERE MG.MemberGroup = @0 ", groupId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                sql = string.Format(" {0} (MN.text LIKE @0 OR M.Email LIKE @0 OR M.LoginName LIKE @0)", groupId.HasValue ? "AND" : "WHERE");
                memberQuery.Append(sql, "%" + search + "%");
            }

            memberQuery.OrderBy(orderBy);

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
            var query = new Sql(string.Format("SELECT id as Id, text as Name FROM umbracoNode GN WHERE nodeObjectType = '{0}'", Constants.ObjectTypes.MemberGroup)).OrderBy("text");

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

        /// <summary>
        /// Gets a single nu cache item by Node Id
        /// </summary>
        /// <param name="id">The node Id</param>
        public NuCacheItem GetNuCacheItem(int id)
        {
            var query = new Sql("SELECT nodeId as Id, data as Data, text as Title, createDate as CreateDate FROM cmsContentNu Nu INNER JOIN umbracoNode N on Nu.nodeId = N.id WHERE Nu.published = 1 AND NodeId = @0", id);

            using (var scope = this.scopeProvider.CreateScope(autoComplete: true))
            {
                return scope.Database.Single<NuCacheItem>(query);
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
    }
}
