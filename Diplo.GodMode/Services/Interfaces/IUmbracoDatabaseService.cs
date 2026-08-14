using Diplo.GodMode.Models;
using NPoco;
using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Gets data from the Umbraco database
    /// </summary>
    public interface IUmbracoDatabaseService
    {
        Page<ContentItem> GetContent(long page, long itemsPerPage, ContentCriteria criteria = null, string orderBy = "N.id");

        IEnumerable<string> GetContentTypeAliases(bool? isContainer = null, bool? isElement = null);

        List<UsageModel> GetContentUsageData(int? id = null, string orderBy = "CT.alias");

        DatabaseType GetDatabaseType();

        /// <summary>
        /// Reports whether Element Type Usage analysis can run on the current database (SQL Server
        /// with compatibility level 130+ for OPENJSON), and whether Library Item / Element Picker
        /// reporting is available (requires the Umbraco 18+ <c>umbracoElement</c> table).
        /// </summary>
        ElementTypeUsageStatus GetElementTypeUsageStatus();

        /// <summary>
        /// Gets aggregated usage counts for every Element Type (BlockContent, BlockSettings, Library
        /// Items and Element Picker references), including Element Types with zero usages.
        /// </summary>
        IEnumerable<ElementTypeUsageSummary> GetElementTypeUsageSummary();

        /// <summary>
        /// Gets every individual usage occurrence for a single Element Type, across all sources.
        /// </summary>
        IEnumerable<ElementTypeUsageDetail> GetElementTypeUsageDetail(Guid elementTypeKey);

        IEnumerable<Lang> GetLanguages();

        IEnumerable<Lang> GetLanguagesWithoutAssignedDomains();

        IEnumerable<DictionaryTranslationStatus> GetDictionaryTranslationStatus();

        IEnumerable<MemberGroupModel> GetMemberGroups();

        IEnumerable<MemberGroupModel> GetMemberTypes();

        Page<MemberModel> GetMembers(
            long page,
            long itemsPerPage,
            int? groupId = null,
            int? memberTypeId = null,
            bool? isApproved = null,
            bool? isLockedOut = null,
            bool? usesTwoFactor = null,
            string search = null,
            string orderBy = "MN.text");

        IEnumerable<string> GetTemplateUrlsToPing();

        IEnumerable<ServerModel> GetRegistredServers();

        IEnumerable<UmbracoKeyValue> GetKeyValues();

        bool CreateKeyValue(string key, string value);

        bool UpdateKeyValue(string key, string value);

        bool DeleteKeyValue(string key);

        NuCacheItem GetNuCacheItem(int id);

        bool DeleteTag(int id);

        List<Tag> GetOrphanedTags();

        List<MediaMap> GetOrphanedMedia();

        long GetOrphanedMediaCount();

        long GetLogRowCount();

        int DeleteLogRows(DateTimeOffset? olderThan = null);

        long GetContentVersionCount();

        long GetContentWithExcessiveVersionsCount(int versionThreshold);

        IEnumerable<DatabaseHealthRow> GetDatabaseHealthRows();

        IEnumerable<DatabaseHealthRow> GetCacheHealthRows();

        IEnumerable<DatabaseTableInfo> GetDatabaseTables();

        DatabaseTableDetail? GetDatabaseTableDetail(string tableName);

        DatabaseTableRows? GetDatabaseTableRows(string tableName, long page = 1, long pageSize = 25);
    }
}
