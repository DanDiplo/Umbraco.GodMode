using Diplo.GodMode.Models;
using NPoco;
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

        IEnumerable<Lang> GetLanguages();

        IEnumerable<MemberGroupModel> GetMemberGroups();

        Page<MemberModel> GetMembers(long page, long itemsPerPage, int? groupId = null, string search = null, string orderBy = "MN.text");

        IEnumerable<string> GetTemplateUrlsToPing();

        IEnumerable<ServerModel> GetRegistredServers();

        IEnumerable<UmbracoKeyValue> GetKeyValues();

        NuCacheItem GetNuCacheItem(int id);
    }
}