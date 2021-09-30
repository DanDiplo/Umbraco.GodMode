using Diplo.GodMode.Controllers;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Querying;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Used to get data out of Umbraco
    /// </summary>
    /// <remarks>Really needs breaking down into smaller classes!</remarks>
    public class UmbracoDataService : IUmbracoDataService
    {
        private readonly IContentService contentService;
        private readonly IContentTypeService contentTypeService;
        private readonly IDataTypeService dataTypeService;
        private readonly IMediaTypeService mediaTypeService;
        private readonly IFileService fileService;
        private readonly IMediaService mediaService;
        private readonly IScopeProvider scopeProvider;
        private readonly ILogger<UmbracoDataService> logger;

        public UmbracoDataService(IScopeProvider scopeProvider, IContentService contentService, IContentTypeService contentTypeService, IDataTypeService dataTypeService, IMediaTypeService mediaTypeService, IFileService fileService, IMediaService mediaService, ILogger<UmbracoDataService> logger)
        {
            this.contentService = contentService;
            this.contentTypeService = contentTypeService;
            this.dataTypeService = dataTypeService;
            this.mediaTypeService = mediaTypeService;
            this.fileService = fileService;
            this.mediaService = mediaService;
            this.scopeProvider = scopeProvider;
            this.logger = logger;
        }

        /// <summary>
        /// Gets all content types (doc types) with associated mapped properties
        /// </summary>
        public IEnumerable<ContentTypeMap> GetContentTypeMap()
        {
            var allContentTypes = this.contentTypeService.GetAll() ?? Enumerable.Empty<IContentType>();

            var mapping = new List<ContentTypeMap>();

            foreach (var ct in allContentTypes.OrderBy(x => x.Name))
            {
                var map = new ContentTypeMap
                {
                    Alias = ct.Alias,
                    Icon = ct.Icon,
                    Name = ct.Name,
                    Id = ct.Id,
                    Udi = ct.GetUdi().Guid,
                    Description = ct.Description,
                    VariesBy = ct.Variations,
                    IsListView = ct.IsContainer,
                    IsElement = ct.IsElement,
                    AllowedAtRoot = ct.AllowedAsRoot,
                    VariesByCulture = ct.VariesByCulture(),
                    CreateDate = ct.CreateDate,
                    Templates = ct.AllowedTemplates != null ? ct.AllowedTemplates.
                    Select(x => new TemplateMap()
                    {
                        Alias = x.Alias,
                        Id = x.Id,
                        Name = x.Name,
                        Path = x.VirtualPath,
                        IsDefault = ct.DefaultTemplate != null && ct.DefaultTemplate.Id == x.Id
                    }) : Enumerable.Empty<TemplateMap>(),
                    Properties = ct.PropertyTypes != null ? ct.PropertyTypes.Select(p => new PropertyTypeMap(p)) : Enumerable.Empty<PropertyTypeMap>(),
                    CompositionProperties = ct.CompositionPropertyTypes != null ? ct.CompositionPropertyTypes.Where(p => ct.PropertyTypes != null && !ct.PropertyTypes.Select(x => x.Id).Contains(p.Id)).Select(pt => new PropertyTypeMap(pt)) : Enumerable.Empty<PropertyTypeMap>(),
                    Compositions = ct.ContentTypeComposition != null ? ct.ContentTypeComposition.
                    Select(x => new ContentTypeData()
                    {
                        Alias = x.Alias,
                        Description = x.Description,
                        Id = x.Id,
                        Icon = x.Icon,
                        Name = x.Name
                    }) : Enumerable.Empty<ContentTypeData>()
                };

                map.AllProperties = map.Properties.Concat(map.CompositionProperties ?? Enumerable.Empty<PropertyTypeMap>());
                map.HasCompositions = ct.ContentTypeComposition != null && ct.ContentTypeComposition.Any();
                map.HasTemplates = ct.AllowedTemplates != null && ct.AllowedTemplates.Any();
                map.PropertyGroups = ct.PropertyGroups != null ? ct.PropertyGroups.Select(x => x.Name) : Enumerable.Empty<string>();

                mapping.Add(map);
            }

            return mapping;
        }

        /// <summary>
        /// Gets all property groups
        /// </summary>
        public IEnumerable<string> GetPropertyGroups()
        {
            return this.contentTypeService.GetAll().
                SelectMany(x => x.PropertyGroups.Select(p => p.Name)).
                Distinct().OrderBy(p => p);
        }

        /// <summary>
        /// Gets all compositions
        /// </summary>
        public IEnumerable<ContentTypeData> GetCompositions()
        {
            return this.contentTypeService.GetAll().
                SelectMany(x => x.ContentTypeComposition).
                DistinctBy(x => x.Id).
                Select(c => new ContentTypeData() { Id = c.Id, Alias = c.Alias, Name = c.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all data types
        /// </summary>
        public IEnumerable<DataTypeMap> GetDataTypes()
        {
            return this.dataTypeService.GetAll().
                Select(x => new DataTypeMap { Id = x.Id, Udi = x.GetUdi().Guid, Name = x.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all property editors
        /// </summary>
        public IEnumerable<DataTypeMap> GetPropertyEditors()
        {
            return this.dataTypeService.GetAll().
                Select(x => new DataTypeMap { Id = x.Id, Udi = x.GetUdi().Guid, Alias = x.EditorAlias }).
                DistinctBy(p => p.Alias).
                OrderBy(p => p.Alias);
        }

        /// <summary>
        /// Gets all data types, including the status of whether they are being used
        /// </summary>
        public IEnumerable<DataTypeMap> GetDataTypesStatus()
        {

            var dataTypes = this.dataTypeService.GetAll().ToList();
            var contentTypes = this.contentTypeService.GetAll();
            var mediaTypes = this.mediaTypeService.GetAll();

            var usedPropertyTypes = contentTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes)).Union(mediaTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes)));
            var usedIds = dataTypes.Where(x => usedPropertyTypes.Select(y => y.DataTypeId).Contains(x.Id)).Select(d => d.Id).ToList();

            return dataTypes.Select(x => new DataTypeMap
            {
                Id = x.Id,
                Udi = x.GetUdi().Guid,
                Name = x.Name,
                Alias = x.EditorAlias,
                DbType = x.DatabaseType.ToString(),
                IsUsed = usedIds.Contains(x.Id)
            }).
            OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all templates
        /// </summary>
        public IEnumerable<TemplateModel> GetTemplates()
        {
            var templateModels = new List<TemplateModel>();
            var templates = this.fileService.GetTemplates();

            foreach (var template in templates)
            {
                var model = new TemplateModel(template)
                {

                    IsMaster = template.IsMasterTemplate,
                    MasterAlias = template.MasterTemplateAlias,
                    Partials = PartialHelper.GetPartialInfo(template.Content, template.Id, template.Alias),
                    Path = template.Path,
                    VirtualPath = template.VirtualPath,
                    Parents = templates.Where(t => template.Path.Split(',').Select(x => Convert.ToInt32(x)).Contains(t.Id)).Select(t => new TemplateModel(t)).OrderBy(d => template.Path.Split(',').IndexOf(d.Id.ToString()))
                };

                templateModels.Add(model);
            }

            return templateModels;
        }

        /// <summary>
        /// Gets all media
        /// </summary>
        public Page<MediaMap> GetMediaPaged(long page = 1, int pageSize = 3, string name = null, int? id = null, int? mediaTypeId = null, string orderBy = "Id", string orderByDir = "ASC")
        {
            string tempExt;

            IQuery<IMedia> criteria = new Query<IMedia>(this.scopeProvider.SqlContext);

            if (!string.IsNullOrEmpty(name))
            {
                criteria = criteria.Where(m => m.Name.Contains(name));
            }

            if (id.HasValue)
            {
                criteria = criteria.Where(m => m.Id == id.Value);
            }

            if (mediaTypeId.HasValue)
            {
                criteria = criteria.Where(m => m.ContentTypeId == mediaTypeId);
            }

            var order = new Ordering(orderBy, orderByDir == "ASC" ? Direction.Ascending : Direction.Descending);

            var media = this.mediaService.GetPagedDescendants(-1, page - 1, pageSize, out long totalRecords, filter: criteria, ordering: order).
                Select(x => new MediaMap()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Alias = x.ContentType.Name,
                    Ext = tempExt = FileTypeHelper.GetExtensionFromMedia(x),
                    Type = FileTypeHelper.GetFileTypeName(tempExt),
                    Size = FileTypeHelper.GetFileSize(x),
                    Udi = x.Key,
                    CreateDate = x.CreateDate,
                }
            ).ToList();

            var paged = new Page<MediaMap>()
            {
                CurrentPage = page,
                Items = media,
                ItemsPerPage = pageSize,
                TotalItems = totalRecords,
                TotalPages = totalRecords / pageSize
            };


            return paged;
        }

        public IEnumerable<ItemBase> GetMediaTypes()
        {
            return this.mediaTypeService.GetAll().Select(x => new ItemBase()
            {
                Id = x.Id,
                Name = x.Name,
                Alias = x.Alias
            });
        }
    }
}