using Diplo.GodMode.Models;
using NPoco;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Gets data via Umbraco services and related classes
    /// </summary>
    public interface IUmbracoDataService
    {
        IEnumerable<ContentTypeCompositionData> GetCompositions();

        IEnumerable<ContentTypeMap> GetContentTypeMap();

        Task<IEnumerable<DataTypeMap>> GetDataTypes();

        Task<IEnumerable<DataTypeMap>> GetDataTypesStatus();

        Task<IEnumerable<ReferenceEdge>> GetReferenceGraph();

        Task<IEnumerable<ReferenceEdge>> GetUsedBy(string targetType, string targetKey);

        Task<IEnumerable<ReferenceEdge>> GetUses(string sourceType, string sourceKey);

        Task<IEnumerable<ConfigurationDriftFinding>> GetConfigurationDriftFindings();

        Page<MediaMap> GetMediaPaged(long page = 1, int pageSize = 3, string name = null, int? id = null, int? mediaTypeId = null, long? minSizeBytes = null, string orderBy = "Id", string orderByDir = "ASC");

        Task<ContentMediaDetail?> GetContentDetail(int id);

        Task<ContentMediaDetail?> GetMediaDetail(int id);

        IEnumerable<ItemBase> GetMediaTypes();

        Task<IEnumerable<DataTypeMap>> GetPropertyEditors();

        IEnumerable<string> GetPropertyGroups();

        Task<IEnumerable<TemplateModel>> GetTemplates();

        Task<int> FixTemplateMasters();

        Task<IEnumerable<TagMapping>> GetTagMapping();

        Task<ServerResponse> CopyDataType(int id);
    }
}
