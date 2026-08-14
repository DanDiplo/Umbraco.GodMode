using Diplo.GodMode.Models;
using NPoco;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        /// Reports whether Element Type Usage analysis can run on the current database and whether
        /// the optional Umbraco Elements schema is available.
        /// </summary>
        ElementTypeUsageStatus GetElementTypeUsageStatus();

        /// <summary>
        /// Gets aggregated stored, configured, Library Item and Element Picker usage counts for
        /// every Element Type, including Element Types with zero usages.
        /// </summary>
        Task<IEnumerable<ElementTypeUsageSummary>> GetElementTypeUsageSummary(bool refresh = false);

        /// <summary>
        /// Gets every individual usage occurrence for a single Element Type, across all sources.
        /// </summary>
        Task<IEnumerable<ElementTypeUsageDetail>> GetElementTypeUsageDetail(Guid elementTypeKey);

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
