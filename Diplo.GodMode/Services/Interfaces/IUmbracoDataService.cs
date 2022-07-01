using System.Collections.Generic;
using Diplo.GodMode.Models;
using NPoco;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Gets data via Umbraco services and related classes
    /// </summary>
    public interface IUmbracoDataService
    {
        IEnumerable<ContentTypeData> GetCompositions();

        IEnumerable<ContentTypeMap> GetContentTypeMap();

        IEnumerable<DataTypeMap> GetDataTypes();

        IEnumerable<DataTypeMap> GetDataTypesStatus();

        Page<MediaMap> GetMediaPaged(long page = 1, int pageSize = 3, string name = null, int? id = null, int? mediaTypeId = null, string orderBy = "Id", string orderByDir = "ASC");

        IEnumerable<ItemBase> GetMediaTypes();

        IEnumerable<DataTypeMap> GetPropertyEditors();

        IEnumerable<string> GetPropertyGroups();

        IEnumerable<TemplateModel> GetTemplates();

        IEnumerable<TagMapping> GetTagMapping();

        ServerResponse CopyDataType(int id);
    }
}