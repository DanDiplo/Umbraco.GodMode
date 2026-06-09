using System.Text.Json;
using Diplo.GodMode.Controllers;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence.Querying;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Used to get data out of Umbraco
    /// </summary>
    /// <remarks>Really needs breaking down into smaller classes!</remarks>
    public class UmbracoDataService : IUmbracoDataService
    {
        private const string ReferenceGraphCacheKey = "Diplo.GodMode.ReferenceGraph";
        private const string SchemaReferenceGraphCacheKey = "Diplo.GodMode.SchemaReferenceGraph";
        private const string DataTypesStatusCacheKey = "Diplo.GodMode.DataTypesStatus";
        private const string TemplatesCacheKey = "Diplo.GodMode.Templates";
        private const string ConfigurationDriftCacheKey = "Diplo.GodMode.ConfigurationDriftFindings";
        private const string TagMappingCacheKey = "Diplo.GodMode.TagMapping";
        private static readonly SemaphoreSlim ReferenceGraphCacheLock = new(1, 1);
        private static readonly SemaphoreSlim SchemaReferenceGraphCacheLock = new(1, 1);
        private readonly IContentService contentService;
        private readonly IContentTypeService contentTypeService;
        private readonly IDataTypeService dataTypeService;
        private readonly IMediaTypeService mediaTypeService;
        private readonly IMemberTypeService memberTypeService;
        private readonly ITemplateService templateService;
        private readonly IMediaService mediaService;
        private readonly IAuditService auditService;
        private readonly IRelationService relationService;
        private readonly IScopeProvider scopeProvider;
        private readonly ITagService tagService;
        private readonly ILanguageService languageService;
        private readonly IIdKeyMap idKeyMap;
        private readonly IConfigurationEditorJsonSerializer serializer;
        private readonly GodModeConfig godModeConfig;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService;
        private readonly IMemoryCache memoryCache;

        public UmbracoDataService(IScopeProvider scopeProvider, IContentService contentService, IContentTypeService contentTypeService, IDataTypeService dataTypeService, IMediaTypeService mediaTypeService, IMemberTypeService memberTypeService, ITemplateService templateService, IMediaService mediaService, IAuditService auditService, IRelationService relationService, ITagService tagService, ILanguageService languageService, IIdKeyMap idKeyMap, IConfigurationEditorJsonSerializer serializer, IOptions<GodModeConfig> godModeConfig, IWebHostEnvironment webHostEnvironment, IDeliveryApiDiagnosticsService deliveryApiDiagnosticsService, IMemoryCache memoryCache)
        {
            this.contentTypeService = contentTypeService;
            this.dataTypeService = dataTypeService;
            this.mediaTypeService = mediaTypeService;
            this.memberTypeService = memberTypeService;
            this.templateService = templateService;
            this.mediaService = mediaService;
            this.auditService = auditService;
            this.relationService = relationService;
            this.scopeProvider = scopeProvider;
            this.tagService = tagService;
            this.contentService = contentService;
            this.languageService = languageService;
            this.idKeyMap = idKeyMap;
            this.serializer = serializer;
            this.godModeConfig = godModeConfig.Value;
            this.webHostEnvironment = webHostEnvironment;
            this.deliveryApiDiagnosticsService = deliveryApiDiagnosticsService;
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Gets all content types (doc types) with associated mapped properties
        /// </summary>
        public IEnumerable<ContentTypeMap> GetContentTypeMap()
        {

            var allContentTypes = this.contentTypeService.GetAll() ?? [];
            var deliveryApiContentTypes = this.deliveryApiDiagnosticsService.GetDiagnosticsAsync().GetAwaiter().GetResult()
                .ContentTypes
                .ToDictionary(x => x.Alias, StringComparer.OrdinalIgnoreCase);

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
                    DeliveryApiExposed = deliveryApiContentTypes.TryGetValue(ct.Alias, out var exposure) && exposure.IsExposed,
                    DeliveryApiExposure = deliveryApiContentTypes.TryGetValue(ct.Alias, out exposure) ? exposure.Exposure : "Unknown",
                    DeliveryApiSensitiveAlias = deliveryApiContentTypes.TryGetValue(ct.Alias, out exposure) && exposure.SensitiveAlias,
                    IsListView = ct.ListView is not null,
                    IsElement = ct.IsElement,
                    AllowedAtRoot = ct.AllowedAsRoot,
                    VariesByCulture = ct.VariesByCulture(),
                    CreateDate = ct.CreateDate,
                    Templates = ct.AllowedTemplates != null ? ct.AllowedTemplates.
                    Select(x => new TemplateMap()
                    {
                        Alias = x.Alias,
                        Id = x.Id,
                        Udi = x.GetUdi().Guid,
                        Name = x.Name,
                        Path = x.VirtualPath,
                        IsDefault = ct.DefaultTemplate != null && ct.DefaultTemplate.Id == x.Id
                    }) : [],
                    Properties = ct.PropertyTypes != null ? ct.PropertyTypes.Select(p => new PropertyTypeMap(p)) : [],
                    CompositionProperties = ct.CompositionPropertyTypes != null ? ct.CompositionPropertyTypes.Where(p => ct.PropertyTypes != null && !ct.PropertyTypes.Select(x => x.Id).Contains(p.Id)).Select(pt => new PropertyTypeMap(pt)) : [],
                    Compositions = ct.ContentTypeComposition != null ? ct.ContentTypeComposition.
                    Select(x => new ContentTypeCompositionData()
                    {
                        Alias = x.Alias,
                        Description = x.Description,
                        Id = x.Id,
                        Udi = x.GetUdi().Guid,
                        Icon = x.Icon,
                        Name = x.Name,
                        IsElement = x.IsElement,
                        VariesBy = x.Variations.ToString(),
                        VariesByCulture = x.VariesByCulture(),
                        HasCompositions = x.ContentTypeComposition != null && x.ContentTypeComposition.Any(),
                        PropertyCount = x.PropertyTypes?.Count() ?? 0,
                        PropertyGroupCount = x.PropertyGroups?.Count() ?? 0
                    }) : []
                };

                map.AllProperties = map.Properties.Concat(map.CompositionProperties ?? []);
                map.HasCompositions = ct.ContentTypeComposition != null && ct.ContentTypeComposition.Any();
                map.HasTemplates = ct.AllowedTemplates != null && ct.AllowedTemplates.Any();
                map.PropertyGroups = ct.PropertyGroups != null ? ct.PropertyGroups.Select(x => x.Name) : [];

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
        public IEnumerable<ContentTypeCompositionData> GetCompositions()
        {
            return this.contentTypeService.GetAll().
                SelectMany(x => x.ContentTypeComposition).
                DistinctBy(x => x.Id).
                Select(c => new ContentTypeCompositionData()
                {
                    Id = c.Id,
                    Udi = c.GetUdi().Guid,
                    Alias = c.Alias,
                    Name = c.Name,
                    Icon = c.Icon,
                    Description = c.Description,
                    IsElement = c.IsElement,
                    VariesBy = c.Variations.ToString(),
                    VariesByCulture = c.VariesByCulture(),
                    HasCompositions = c.ContentTypeComposition != null && c.ContentTypeComposition.Any(),
                    PropertyCount = c.PropertyTypes?.Count() ?? 0,
                    PropertyGroupCount = c.PropertyGroups?.Count() ?? 0
                }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all data types
        /// </summary>
        public async Task<IEnumerable<DataTypeMap>> GetDataTypes()
        {
            return (await this.dataTypeService.GetAllAsync()).
                Select(x => new DataTypeMap { Id = x.Id, Udi = x.GetUdi().Guid, Name = x.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all property editors
        /// </summary>
        public async Task<IEnumerable<DataTypeMap>> GetPropertyEditors()
        {
            return (await this.dataTypeService.GetAllAsync()).
                Select(x => new DataTypeMap { Id = x.Id, Udi = x.GetUdi().Guid, Alias = x.EditorAlias }).
                DistinctBy(p => p.Alias).
                OrderBy(p => p.Alias);
        }

        /// <summary>
        /// Gets all data types, including the status of whether they are being used
        /// </summary>
        public async Task<IEnumerable<DataTypeMap>> GetDataTypesStatus()
            => await this.GetCachedAsync(DataTypesStatusCacheKey, TimeSpan.FromMinutes(2), this.BuildDataTypesStatus);

        private async Task<IReadOnlyList<DataTypeMap>> BuildDataTypesStatus()
        {
            var dataTypes = (await this.dataTypeService.GetAllAsync()).ToList();
            var contentTypes = this.contentTypeService.GetAll().ToList();
            var mediaTypes = this.mediaTypeService.GetAll().ToList();

            var usedPropertyTypes = contentTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes)).Union(mediaTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes)));
            var usedIds = usedPropertyTypes.Select(y => y.DataTypeId).ToHashSet();
            var nestedUsedIds = this.GetNestedDataTypeIds(dataTypes, contentTypes);

            return dataTypes.Select(x => new DataTypeMap
            {
                Id = x.Id,
                Udi = x.GetUdi().Guid,
                Name = x.Name,
                Alias = x.EditorAlias,
                DbType = x.DatabaseType.ToString(),
                IsUsed = usedIds.Contains(x.Id),
                IsNestedUsed = nestedUsedIds.Contains(x.Id),
                UpdateDate = x.UpdateDate == default ? x.CreateDate : x.UpdateDate
            }).
            OrderBy(x => x.Name)
            .ToList();
        }

        private HashSet<int> GetNestedDataTypeIds(IReadOnlyCollection<IDataType> dataTypes, IEnumerable<IContentType> contentTypes)
        {
            var nestedDataTypeIds = new HashSet<int>();
            var dataTypesById = dataTypes.ToDictionary(x => x.Id);
            var contentTypesByKey = contentTypes.ToDictionary(x => x.Key);

            foreach (var dataType in dataTypes)
            {
                this.CollectNestedDataTypeIdsFromDataType(dataType, dataTypesById, contentTypesByKey, nestedDataTypeIds, [], []);
            }

            return nestedDataTypeIds;
        }

        public async Task<IEnumerable<ReferenceEdge>> GetReferenceGraph()
            => await this.GetReferenceGraphCached();

        public async Task<IEnumerable<ReferenceEdge>> GetSchemaReferenceGraph()
            => await this.GetSchemaReferenceGraphCached();

        private async Task<IReadOnlyList<ReferenceEdge>> GetReferenceGraphCached()
        {
            if (this.memoryCache.TryGetValue(ReferenceGraphCacheKey, out IReadOnlyList<ReferenceEdge>? cached) && cached is not null)
            {
                return cached;
            }

            await ReferenceGraphCacheLock.WaitAsync();
            try
            {
                if (this.memoryCache.TryGetValue(ReferenceGraphCacheKey, out cached) && cached is not null)
                {
                    return cached;
                }

                var built = await this.BuildReferenceGraph();
                this.memoryCache.Set(ReferenceGraphCacheKey, built, TimeSpan.FromMinutes(2));
                return built;
            }
            finally
            {
                ReferenceGraphCacheLock.Release();
            }
        }

        private async Task<IReadOnlyList<ReferenceEdge>> GetSchemaReferenceGraphCached()
        {
            if (this.memoryCache.TryGetValue(SchemaReferenceGraphCacheKey, out IReadOnlyList<ReferenceEdge>? cached) && cached is not null)
            {
                return cached;
            }

            await SchemaReferenceGraphCacheLock.WaitAsync();
            try
            {
                if (this.memoryCache.TryGetValue(SchemaReferenceGraphCacheKey, out cached) && cached is not null)
                {
                    return cached;
                }

                var built = await this.BuildReferenceGraph(includeContentNodeTemplateEdges: false);
                this.memoryCache.Set(SchemaReferenceGraphCacheKey, built, TimeSpan.FromMinutes(2));
                return built;
            }
            finally
            {
                SchemaReferenceGraphCacheLock.Release();
            }
        }

        private async Task<IReadOnlyList<ReferenceEdge>> BuildReferenceGraph()
            => await this.BuildReferenceGraph(includeContentNodeTemplateEdges: true);

        private async Task<IReadOnlyList<ReferenceEdge>> BuildReferenceGraph(bool includeContentNodeTemplateEdges)
        {
            var edges = new List<ReferenceEdge>();
            var dataTypes = (await this.dataTypeService.GetAllAsync()).ToList();
            var dataTypesById = dataTypes.ToDictionary(x => x.Id);
            var contentTypes = this.contentTypeService.GetAll().ToList();
            var contentTypesByKey = contentTypes.ToDictionary(x => x.Key);
            var templates = (await this.GetTemplates()).ToList();
            var templateKeysById = templates.ToDictionary(x => x.Id, x => x.Udi);

            foreach (var contentType in contentTypes.OrderBy(x => x.Name))
            {
                var sourceType = contentType.IsElement ? "Element Type" : "Document Type";

                foreach (var template in contentType.AllowedTemplates ?? [])
                {
                    edges.Add(this.CreateEdge(sourceType, contentType.Name, contentType.Alias, contentType.Key, "allows template", "Template", template.Name, template.Alias, template.Key, template.Id == contentType.DefaultTemplate?.Id ? "Default template" : null));
                }

                foreach (var composition in contentType.ContentTypeComposition ?? [])
                {
                    edges.Add(this.CreateEdge(sourceType, contentType.Name, contentType.Alias, contentType.Key, "composes", composition.IsElement ? "Element Type" : "Document Type", composition.Name, composition.Alias, composition.Key));
                }

                foreach (var propertyType in contentType.PropertyTypes ?? [])
                {
                    this.AddPropertyDataTypeEdge(edges, sourceType, contentType, propertyType, dataTypesById, contentTypesByKey, false);
                }

                var nativePropertyIds = contentType.PropertyTypes?.Select(x => x.Id).ToHashSet() ?? [];
                foreach (var propertyType in contentType.CompositionPropertyTypes?.Where(x => !nativePropertyIds.Contains(x.Id)) ?? [])
                {
                    this.AddPropertyDataTypeEdge(edges, sourceType, contentType, propertyType, dataTypesById, contentTypesByKey, true);
                }
            }

            foreach (var mediaType in this.mediaTypeService.GetAll().OrderBy(x => x.Name))
            {
                this.AddContentTypePropertyEdges(edges, "Media Type", mediaType, dataTypesById, contentTypesByKey);
            }

            foreach (var memberType in this.memberTypeService.GetAll().OrderBy(x => x.Name))
            {
                this.AddContentTypePropertyEdges(edges, "Member Type", memberType, dataTypesById, contentTypesByKey);
            }

            foreach (var template in templates)
            {
                foreach (var partial in template.Partials ?? [])
                {
                    var partialKey = this.StableKey("Partial", partial.Path ?? partial.Name);
                    edges.Add(this.CreateEdge("Template", template.Name, template.Alias, template.Udi, "uses partial", "Partial", partial.Name, partial.Path, partialKey, partial.Path));
                }
            }

            if (includeContentNodeTemplateEdges)
            {
                foreach (var contentType in contentTypes.Where(x => !x.IsElement).OrderBy(x => x.Name))
                {
                    foreach (var content in this.contentService.GetPagedOfType(contentType.Id, 0, int.MaxValue, out _, null, Ordering.By("Name")))
                    {
                        if (!content.TemplateId.HasValue || !templateKeysById.TryGetValue(content.TemplateId.Value, out var templateKey))
                        {
                            continue;
                        }

                        var template = templates.FirstOrDefault(x => x.Udi == templateKey);
                        if (template is null)
                        {
                            continue;
                        }

                        edges.Add(this.CreateEdge("Content Node", content.Name, content.ContentType.Alias, content.Key, "uses template", "Template", template.Name, template.Alias, template.Udi, $"{content.ContentType.Name} ({content.Id})"));
                    }
                }
            }

            return edges
                .GroupBy(x => $"{x.SourceType}|{x.SourceKey}|{x.Relation}|{x.TargetType}|{x.TargetKey}|{x.Context}")
                .Select(x => x.First())
                .OrderBy(x => x.SourceType)
                .ThenBy(x => x.SourceName)
                .ThenBy(x => x.Relation)
                .ThenBy(x => x.TargetName)
                .ToList();
        }

        public async Task<IEnumerable<ReferenceEdge>> GetUsedBy(string targetType, string targetKey)
        {
            return (await this.GetReferenceGraphForLookup(targetType, targetKey))
                .Where(x => x.TargetType.InvariantEquals(targetType) &&
                    (x.TargetKey.InvariantEquals(targetKey) || x.TargetAlias.InvariantEquals(targetKey) || x.TargetName.InvariantEquals(targetKey)));
        }

        public async Task<IEnumerable<ReferenceEdge>> GetUses(string sourceType, string sourceKey)
        {
            return (await this.GetReferenceGraphForLookup(sourceType, sourceKey))
                .Where(x => x.SourceType.InvariantEquals(sourceType) &&
                    (x.SourceKey.InvariantEquals(sourceKey) || x.SourceAlias.InvariantEquals(sourceKey) || x.SourceName.InvariantEquals(sourceKey)));
        }

        private async Task<IReadOnlyList<ReferenceEdge>> GetReferenceGraphForLookup(string entityType, string entityKey)
        {
            if (entityType.InvariantEquals("Content Node"))
            {
                return await this.GetReferenceGraphCached();
            }

            if (entityType.InvariantEquals("Template") && int.TryParse(entityKey, out _))
            {
                return await this.GetReferenceGraphCached();
            }

            return await this.GetSchemaReferenceGraphCached();
        }

        public async Task<IEnumerable<ConfigurationDriftFinding>> GetConfigurationDriftFindings()
            => await this.GetCachedAsync(ConfigurationDriftCacheKey, TimeSpan.FromMinutes(2), this.BuildConfigurationDriftFindings);

        private async Task<IReadOnlyList<ConfigurationDriftFinding>> BuildConfigurationDriftFindings()
        {
            var findings = new List<ConfigurationDriftFinding>();
            var dataTypes = (await this.dataTypeService.GetAllAsync()).ToList();
            var contentTypes = this.contentTypeService.GetAll().Cast<IContentTypeComposition>().ToList();
            contentTypes.AddRange(this.mediaTypeService.GetAll());
            contentTypes.AddRange(this.memberTypeService.GetAll());
            var dataTypesById = dataTypes.ToDictionary(x => x.Id);

            this.AddDataTypeDriftFindings(findings, dataTypes);
            this.AddPropertyAliasDriftFindings(findings, contentTypes, dataTypesById);
            this.AddContentTypeDriftFindings(findings, contentTypes, dataTypesById);

            return findings
                .GroupBy(x => $"{x.Category}|{x.EntityType}|{x.EntityKey}|{x.Summary}")
                .Select(x => x.First())
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Category)
                .ThenBy(x => x.EntityName)
                .ToList();
        }

        private void AddContentTypePropertyEdges(
            ICollection<ReferenceEdge> edges,
            string sourceType,
            IContentTypeComposition contentType,
            IReadOnlyDictionary<int, IDataType> dataTypesById,
            IReadOnlyDictionary<Guid, IContentType> contentTypesByKey)
        {
            foreach (var propertyType in contentType.PropertyTypes ?? [])
            {
                this.AddPropertyDataTypeEdge(edges, sourceType, contentType, propertyType, dataTypesById, contentTypesByKey, false);
            }

            var nativePropertyIds = contentType.PropertyTypes?.Select(x => x.Id).ToHashSet() ?? [];
            foreach (var propertyType in contentType.CompositionPropertyTypes?.Where(x => !nativePropertyIds.Contains(x.Id)) ?? [])
            {
                this.AddPropertyDataTypeEdge(edges, sourceType, contentType, propertyType, dataTypesById, contentTypesByKey, true);
            }
        }

        private void AddPropertyDataTypeEdge(
            ICollection<ReferenceEdge> edges,
            string sourceType,
            IContentTypeComposition contentType,
            IPropertyType propertyType,
            IReadOnlyDictionary<int, IDataType> dataTypesById,
            IReadOnlyDictionary<Guid, IContentType> contentTypesByKey,
            bool inherited)
        {
            if (!dataTypesById.TryGetValue(propertyType.DataTypeId, out var dataType))
            {
                return;
            }

            var context = $"{propertyType.Name} ({propertyType.Alias})" + (inherited ? " inherited" : string.Empty);
            edges.Add(this.CreateEdge(sourceType, contentType.Name, contentType.Alias, contentType.Key, "uses data type", "Data Type", dataType.Name, dataType.EditorAlias, dataType.Key, context));
            this.AddBlockConfigurationEdges(edges, dataType, dataTypesById, contentTypesByKey, context, [], []);
        }

        private void AddBlockConfigurationEdges(
            ICollection<ReferenceEdge> edges,
            IDataType dataType,
            IReadOnlyDictionary<int, IDataType> dataTypesById,
            IReadOnlyDictionary<Guid, IContentType> contentTypesByKey,
            string context,
            HashSet<int> visitedDataTypeIds,
            HashSet<Guid> visitedContentTypeKeys)
        {
            if (!visitedDataTypeIds.Add(dataType.Id))
            {
                return;
            }

            foreach (var elementTypeKey in this.GetConfiguredBlockElementTypeKeys(dataType.ConfigurationObject))
            {
                if (!contentTypesByKey.TryGetValue(elementTypeKey, out var elementType))
                {
                    edges.Add(this.CreateEdge("Data Type", dataType.Name, dataType.EditorAlias, dataType.Key, "configures missing block type", "Element Type", "Missing element type", elementTypeKey.ToString(), elementTypeKey, context));
                    continue;
                }

                edges.Add(this.CreateEdge("Data Type", dataType.Name, dataType.EditorAlias, dataType.Key, "configures block type", elementType.IsElement ? "Element Type" : "Document Type", elementType.Name, elementType.Alias, elementType.Key, context));

                if (!visitedContentTypeKeys.Add(elementTypeKey))
                {
                    continue;
                }

                foreach (var propertyType in elementType.PropertyTypes.Concat(elementType.CompositionPropertyTypes))
                {
                    if (!dataTypesById.TryGetValue(propertyType.DataTypeId, out var nestedDataType))
                    {
                        continue;
                    }

                    var nestedContext = $"{elementType.Name}.{propertyType.Name} ({propertyType.Alias})";
                    edges.Add(this.CreateEdge(elementType.IsElement ? "Element Type" : "Document Type", elementType.Name, elementType.Alias, elementType.Key, "uses nested data type", "Data Type", nestedDataType.Name, nestedDataType.EditorAlias, nestedDataType.Key, nestedContext));
                    this.AddBlockConfigurationEdges(edges, nestedDataType, dataTypesById, contentTypesByKey, nestedContext, visitedDataTypeIds, visitedContentTypeKeys);
                }
            }
        }

        private ReferenceEdge CreateEdge(string sourceType, string sourceName, string sourceAlias, Guid sourceKey, string relation, string targetType, string targetName, string targetAlias, Guid targetKey, string context = null)
        {
            return new ReferenceEdge
            {
                SourceType = sourceType,
                SourceName = sourceName,
                SourceAlias = sourceAlias,
                SourceKey = sourceKey.ToString(),
                Relation = relation,
                TargetType = targetType,
                TargetName = targetName,
                TargetAlias = targetAlias,
                TargetKey = targetKey.ToString(),
                Context = context
            };
        }

        private Guid StableKey(string type, string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{type}:{value}".ToLowerInvariant());
            return new Guid(System.Security.Cryptography.MD5.HashData(bytes));
        }

        private void AddDataTypeDriftFindings(ICollection<ConfigurationDriftFinding> findings, IEnumerable<IDataType> dataTypes)
        {
            foreach (var group in dataTypes.Where(x => !string.IsNullOrWhiteSpace(x.EditorAlias)).GroupBy(x => x.EditorAlias).Where(x => x.Count() > 1))
            {
                foreach (var nameGroup in group.GroupBy(x => this.NameStem(x.Name)).Where(x => x.Count() > 1))
                {
                    var configs = nameGroup
                        .Select(x => new { DataType = x, Config = this.NormalizeConfiguration(x.ConfigurationData), Database = x.DatabaseType.ToString() })
                        .ToList();

                    if (configs.Select(x => x.Config).Distinct().Count() <= 1 && configs.Select(x => x.Database).Distinct().Count() <= 1)
                    {
                        continue;
                    }

                    foreach (var item in configs)
                    {
                        findings.Add(this.CreateDriftFinding(
                            "Medium",
                            "Data Type Configuration",
                            "Data Type",
                            item.DataType.Name,
                            item.DataType.EditorAlias,
                            item.DataType.Key.ToString(),
                            configs.Where(x => x.DataType.Key != item.DataType.Key).Select(x => x.DataType.Name),
                            $"Similar {item.DataType.EditorAlias} data types use different configuration.",
                            this.DiffDataTypeFields(item, configs),
                            "Review whether these data types should share the same editor settings, validation, storage and block configuration."));
                    }
                }
            }
        }

        private IEnumerable<string> DiffDataTypeFields(dynamic item, IEnumerable<dynamic> group)
        {
            if (group.Select(x => (string)x.Config).Distinct().Count() > 1)
            {
                yield return "Configuration";
            }

            if (group.Select(x => (string)x.Database).Distinct().Count() > 1)
            {
                yield return "Database type";
            }
        }

        private void AddPropertyAliasDriftFindings(ICollection<ConfigurationDriftFinding> findings, IEnumerable<IContentTypeComposition> contentTypes, IReadOnlyDictionary<int, IDataType> dataTypesById)
        {
            var rows = contentTypes
                .SelectMany(ct => ct.PropertyTypes.Concat(ct.CompositionPropertyTypes).Select(p => new { ContentType = ct, Property = p }))
                .Where(x => !string.IsNullOrWhiteSpace(x.Property.Alias))
                .Where(x => !this.IsIgnoredAlias(x.Property.Alias))
                .ToList();

            foreach (var group in rows.GroupBy(x => x.Property.Alias).Where(x => x.Count() > 1))
            {
                var signatures = group.Select(x => this.PropertySignature(x.Property, dataTypesById)).Distinct().ToList();
                if (signatures.Count <= 1)
                {
                    continue;
                }

                foreach (var row in group)
                {
                    var differingFields = this.DiffPropertyFields(row.Property, group.Select(x => x.Property), dataTypesById).ToList();
                    findings.Add(this.CreateDriftFinding(
                        GetPropertyAliasDriftSeverity(differingFields),
                        "Property Alias Drift",
                        this.GetCompositionTypeName(row.ContentType),
                        row.ContentType.Name,
                        row.ContentType.Alias,
                        row.ContentType.Key.ToString(),
                        group.Where(x => x.ContentType.Key != row.ContentType.Key).Select(x => $"{x.ContentType.Name}.{x.Property.Alias}"),
                        $"Property alias '{row.Property.Alias}' is configured differently across content models.",
                        differingFields,
                        GetPropertyAliasDriftRecommendation(differingFields)));
                }
            }
        }

        private void AddContentTypeDriftFindings(ICollection<ConfigurationDriftFinding> findings, IEnumerable<IContentTypeComposition> contentTypes, IReadOnlyDictionary<int, IDataType> dataTypesById)
        {
            var comparable = contentTypes
                .Where(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes).Any(p => !this.IsIgnoredAlias(p.Alias)))
                .ToList();
            foreach (var group in comparable.GroupBy(x => this.NameStem(x.Name)).Where(x => x.Count() > 1))
            {
                var propertySets = group
                    .Select(x => new
                    {
                        ContentType = x,
                        Signature = string.Join("|", x.PropertyTypes.Concat(x.CompositionPropertyTypes).Where(p => !this.IsIgnoredAlias(p.Alias)).Select(p => this.PropertySignature(p, dataTypesById)).OrderBy(x => x))
                    })
                    .ToList();

                if (propertySets.Select(x => x.Signature).Distinct().Count() <= 1)
                {
                    continue;
                }

                foreach (var item in propertySets)
                {
                    findings.Add(this.CreateDriftFinding(
                        "Low",
                        "Content Type Drift",
                        this.GetCompositionTypeName(item.ContentType),
                        item.ContentType.Name,
                        item.ContentType.Alias,
                        item.ContentType.Key.ToString(),
                        propertySets.Where(x => x.ContentType.Key != item.ContentType.Key).Select(x => x.ContentType.Name),
                        "Similarly named content models have different property sets or property configuration.",
                        ["Properties", "Compositions"],
                        "Confirm the schema drift is intentional or align shared properties and compositions."));
                }
            }
        }

        private string PropertySignature(IPropertyType propertyType, IReadOnlyDictionary<int, IDataType> dataTypesById)
        {
            dataTypesById.TryGetValue(propertyType.DataTypeId, out var dataType);
            return string.Join(":", propertyType.Alias, propertyType.Name, propertyType.DataTypeId, dataType?.EditorAlias, propertyType.Mandatory, propertyType.ValidationRegExp, propertyType.Variations, propertyType.ValueStorageType);
        }

        private bool IsIgnoredAlias(string alias)
        {
            return !string.IsNullOrWhiteSpace(alias)
                && this.godModeConfig.AliasesToIgnore.Any(x => alias.InvariantEquals(x));
        }

        private IEnumerable<string> DiffPropertyFields(IPropertyType propertyType, IEnumerable<IPropertyType> properties, IReadOnlyDictionary<int, IDataType> dataTypesById)
        {
            if (properties.Select(x => x.Name ?? string.Empty).Distinct().Count() > 1)
            {
                yield return "Name";
            }

            if (properties.Select(x => x.DataTypeId).Distinct().Count() > 1)
            {
                yield return "Data type";
            }

            if (properties.Select(x => dataTypesById.TryGetValue(x.DataTypeId, out var dataType) ? dataType.EditorAlias : string.Empty).Distinct().Count() > 1)
            {
                yield return "Editor";
            }

            if (properties.Select(x => x.Mandatory).Distinct().Count() > 1)
            {
                yield return "Mandatory";
            }

            if (properties.Select(x => x.ValidationRegExp ?? string.Empty).Distinct().Count() > 1)
            {
                yield return "Validation";
            }

            if (properties.Select(x => x.Variations).Distinct().Count() > 1)
            {
                yield return "Variations";
            }

            if (properties.Select(x => x.ValueStorageType).Distinct().Count() > 1)
            {
                yield return "Storage";
            }
        }

        private static string GetPropertyAliasDriftSeverity(IReadOnlyCollection<string> differingFields)
        {
            if (differingFields.Any(x => x is "Data type" or "Editor" or "Storage" or "Validation"))
            {
                return "Medium";
            }

            return "Low";
        }

        private static string GetPropertyAliasDriftRecommendation(IReadOnlyCollection<string> differingFields)
        {
            if (differingFields.Any(x => x is "Data type" or "Editor" or "Storage" or "Validation"))
            {
                return "Review whether the shared alias represents the same value everywhere. Align the configuration, or rename aliases that intentionally mean different things.";
            }

            return "Review whether the difference is intentional. Mandatory, name and variation differences are often normal across content models.";
        }

        private ConfigurationDriftFinding CreateDriftFinding(string severity, string category, string entityType, string entityName, string entityAlias, string entityKey, IEnumerable<string> comparedWith, string summary, IEnumerable<string> differingFields, string recommendation)
        {
            return new ConfigurationDriftFinding
            {
                Severity = severity,
                Score = severity switch
                {
                    "High" => 80,
                    "Medium" => 50,
                    "Low" => 20,
                    _ => 10
                },
                Category = category,
                EntityType = entityType,
                EntityName = entityName,
                EntityAlias = entityAlias,
                EntityKey = entityKey,
                ComparedWith = comparedWith.Distinct().OrderBy(x => x).ToList(),
                Summary = summary,
                DifferingFields = differingFields.Distinct().OrderBy(x => x).ToList(),
                Recommendation = recommendation
            };
        }

        private string NormalizeConfiguration(object value)
        {
            return value is null
                ? string.Empty
                : JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
        }

        private string NameStem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray();

            return new string(chars).TrimEnd("copy".ToCharArray());
        }

        private string GetCompositionTypeName(IContentTypeComposition contentType)
        {
            return contentType switch
            {
                IMediaType => "Media Type",
                IMemberType => "Member Type",
                IContentType ct when ct.IsElement => "Element Type",
                IContentType => "Document Type",
                _ => "Content Type"
            };
        }

        private void CollectNestedDataTypeIdsFromDataType(
            IDataType dataType,
            IReadOnlyDictionary<int, IDataType> dataTypesById,
            IReadOnlyDictionary<Guid, IContentType> contentTypesByKey,
            HashSet<int> nestedDataTypeIds,
            HashSet<int> visitedDataTypeIds,
            HashSet<Guid> visitedContentTypeKeys)
        {
            if (!visitedDataTypeIds.Add(dataType.Id))
            {
                return;
            }

            foreach (var elementTypeKey in this.GetConfiguredBlockElementTypeKeys(dataType.ConfigurationObject))
            {
                if (!contentTypesByKey.TryGetValue(elementTypeKey, out var elementType) || !visitedContentTypeKeys.Add(elementTypeKey))
                {
                    continue;
                }

                foreach (var propertyType in elementType.PropertyTypes.Concat(elementType.CompositionPropertyTypes))
                {
                    nestedDataTypeIds.Add(propertyType.DataTypeId);

                    if (dataTypesById.TryGetValue(propertyType.DataTypeId, out var nestedDataType))
                    {
                        this.CollectNestedDataTypeIdsFromDataType(nestedDataType, dataTypesById, contentTypesByKey, nestedDataTypeIds, visitedDataTypeIds, visitedContentTypeKeys);
                    }
                }
            }
        }

        private IEnumerable<Guid> GetConfiguredBlockElementTypeKeys(object configuration)
        {
            return configuration switch
            {
                BlockListConfiguration blockList => blockList.Blocks?.SelectMany(x => this.GetElementTypeKeys(x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                BlockGridConfiguration blockGrid => blockGrid.Blocks?.SelectMany(x => this.GetElementTypeKeys(x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                RichTextConfiguration richText => richText.Blocks?.SelectMany(x => this.GetElementTypeKeys(x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                SingleBlockConfiguration singleBlock => singleBlock.Blocks?.SelectMany(x => this.GetElementTypeKeys(x.ContentElementTypeKey, x.SettingsElementTypeKey)) ?? [],
                _ => []
            };
        }

        private IEnumerable<Guid> GetElementTypeKeys(Guid? contentElementTypeKey, Guid? settingsElementTypeKey)
        {
            if (contentElementTypeKey.HasValue)
            {
                yield return contentElementTypeKey.Value;
            }

            if (settingsElementTypeKey.HasValue)
            {
                yield return settingsElementTypeKey.Value;
            }
        }

        /// <summary>
        /// Gets all templates
        /// </summary>
        public async Task<IEnumerable<TemplateModel>> GetTemplates()
            => await this.GetCachedAsync(TemplatesCacheKey, TimeSpan.FromMinutes(2), this.BuildTemplates);

        private async Task<IReadOnlyList<TemplateModel>> BuildTemplates()
        {
            var templateModels = new List<TemplateModel>();
            var templates = (await this.templateService.GetAllAsync()).ToList();
            var templatesById = templates.ToDictionary(x => x.Id);

            foreach (var template in templates)
            {
                var templatePathIds = template.Path.Split(',').Select(x => Convert.ToInt32(x)).ToList();
                var model = new TemplateModel(template)
                {
                    IsMaster = template.IsMasterTemplate,
                    MasterAlias = template.MasterTemplateAlias,
                    Partials = PartialHelper.GetPartialInfo(template.Content, template.Id, template.Alias),
                    ViewComponents = ViewComponentHelper.GetViewComponentInfo(template.Content, template.Id, template.Alias),
                    Assets = ViewUsageHelper.GetAssetInfo(template.Content, template.Id, template.Alias, this.webHostEnvironment.WebRootPath),
                    Sections = ViewUsageHelper.GetSectionInfo(template.Content, template.Id, template.Alias),
                    Forms = ViewUsageHelper.GetFormInfo(template.Content, template.Id, template.Alias),
                    TagHelpers = ViewUsageHelper.GetTagHelperInfo(template.Content, template.Id, template.Alias),
                    UmbracoUsages = ViewUsageHelper.GetUmbracoUsageInfo(template.Content, template.Id, template.Alias),
                    Path = template.Path,
                    VirtualPath = template.VirtualPath,
                    Layout = LayoutHelper.GetTemplateInfo(template),
                    HasCorrectMaster = true,
                    Parents = templatePathIds.Where(templatesById.ContainsKey).Select(id => new TemplateModel(templatesById[id]))
                };

                if (!string.IsNullOrEmpty(model.Layout) && model.Layout != "null")
                {
                    if (!model.Layout.InvariantEquals(template.MasterTemplateAlias))
                    {
                        model.HasCorrectMaster = false;
                    }
                }

                templateModels.Add(model);
            }

            return templateModels;
        }

        /// <summary>
        /// Attemps to set the correct master template for templates that have a layout but this isn't set in the DB
        /// </summary>
        /// <returns>A count of those that are fixed</returns>
        public Task<int> FixTemplateMasters()
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// Gets all media
        /// </summary>
        public Page<MediaMap> GetMediaPaged(long page = 1, int pageSize = 3, string name = null, int? id = null, int? mediaTypeId = null, long? minSizeBytes = null, string orderBy = "Id", string orderByDir = "ASC")
        {
            IQuery<IMedia> criteria = new Query<IMedia>(this.scopeProvider.SqlContext);

            if (id.HasValue)
            {
                criteria = criteria.Where(m => m.Id == id.Value);
            }

            if (mediaTypeId.HasValue)
            {
                criteria = criteria.Where(m => m.ContentTypeId == mediaTypeId);
            }

            var order = new Ordering(orderBy, orderByDir == "ASC" ? Direction.Ascending : Direction.Descending);

            if (!string.IsNullOrWhiteSpace(name))
            {
                return this.GetMediaPagedBySearch(page, pageSize, name, minSizeBytes.GetValueOrDefault(), criteria, order);
            }

            if (minSizeBytes.GetValueOrDefault() > 0)
            {
                return this.GetMediaPagedBySize(page, pageSize, minSizeBytes.Value, criteria, order);
            }

            var media = this.mediaService.GetPagedDescendants(-1, page - 1, pageSize, out long totalRecords, filter: criteria, ordering: order)
                .Select(ToMediaMap)
                .ToList();

            var paged = new Page<MediaMap>()
            {
                CurrentPage = page,
                Items = media,
                ItemsPerPage = pageSize,
                TotalItems = totalRecords,
                TotalPages = (long)Math.Ceiling(totalRecords / (decimal)pageSize)
            };

            return paged;
        }

        private Page<MediaMap> GetMediaPagedBySearch(long page, int pageSize, string search, long minSizeBytes, IQuery<IMedia> criteria, Ordering order)
        {
            const int scanPageSize = 500;
            var matching = new List<MediaMap>();
            var scanPage = 0L;
            long totalRecords;

            do
            {
                var batch = this.mediaService.GetPagedDescendants(-1, scanPage, scanPageSize, out totalRecords, filter: criteria, ordering: order)
                    .Select(ToMediaMap)
                    .Where(media => MediaMatchesSearch(media, search))
                    .Where(media => minSizeBytes <= 0 || media.Size >= minSizeBytes);

                matching.AddRange(batch);
                scanPage++;
            }
            while (scanPage * scanPageSize < totalRecords);

            var totalItems = matching.Count;
            var items = matching
                .Skip((int)((page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return new Page<MediaMap>
            {
                CurrentPage = page,
                Items = items,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = (long)Math.Ceiling(totalItems / (decimal)pageSize)
            };
        }

        private Page<MediaMap> GetMediaPagedBySize(long page, int pageSize, long minSizeBytes, IQuery<IMedia> criteria, Ordering order)
        {
            const int scanPageSize = 500;
            var matching = new List<MediaMap>();
            var scanPage = 0L;
            long totalRecords;

            do
            {
                var batch = this.mediaService.GetPagedDescendants(-1, scanPage, scanPageSize, out totalRecords, filter: criteria, ordering: order)
                    .Select(ToMediaMap)
                    .Where(media => media.Size >= minSizeBytes);

                matching.AddRange(batch);
                scanPage++;
            }
            while (scanPage * scanPageSize < totalRecords);

            var totalItems = matching.Count;
            var items = matching
                .Skip((int)((page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return new Page<MediaMap>
            {
                CurrentPage = page,
                Items = items,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                TotalPages = (long)Math.Ceiling(totalItems / (decimal)pageSize)
            };
        }

        private static MediaMap ToMediaMap(IMedia media)
        {
            var extension = FileTypeHelper.GetExtensionFromMedia(media);

            return new MediaMap
            {
                Id = media.Id,
                Name = media.Name,
                Alias = media.ContentType.Name,
                MediaTypeAlias = media.ContentType.Alias,
                MediaTypeIcon = media.ContentType.Icon,
                Ext = extension,
                Type = FileTypeHelper.GetFileTypeName(extension, media),
                Size = FileTypeHelper.GetFileSize(media),
                Udi = media.Key,
                CreateDate = media.CreateDate,
                UpdateDate = media.UpdateDate == default ? media.CreateDate : media.UpdateDate,
                Path = media.Path
            };
        }

        private static bool MediaMatchesSearch(MediaMap media, string search)
        {
            var trimmedSearch = search.Trim();

            if (string.IsNullOrWhiteSpace(trimmedSearch))
            {
                return true;
            }

            if (int.TryParse(trimmedSearch, out var id) && media.Id == id)
            {
                return true;
            }

            return Contains(media.Name, trimmedSearch)
                || Contains(media.Udi.ToString(), trimmedSearch)
                || Contains(media.MediaTypeAlias, trimmedSearch)
                || Contains(media.Alias, trimmedSearch);
        }

        private static bool Contains(string? value, string search)
            => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

        public async Task<ContentMediaDetail?> GetContentDetail(int id)
        {
            var content = this.contentService.GetById(id);
            if (content is null)
            {
                return null;
            }

            return new ContentMediaDetail
            {
                Kind = "Content",
                Id = content.Id,
                Key = content.Key,
                Name = content.Name ?? string.Empty,
                ContentTypeName = content.ContentType.Name ?? string.Empty,
                ContentTypeAlias = content.ContentType.Alias,
                Path = content.Path,
                Ancestors = this.ContentAncestors(content.Path, content.Id),
                ParentId = content.ParentId,
                Level = content.Level,
                Trashed = content.Trashed,
                CreateDate = content.CreateDate,
                UpdateDate = content.UpdateDate == default ? content.CreateDate : content.UpdateDate,
                State = new ContentStateDetail
                {
                    Published = content.Published,
                    Edited = content.Edited,
                    TemplateId = content.TemplateId,
                    PublishedVersionId = content.PublishedVersionId,
                    PublishDate = content.PublishDate,
                    AvailableCultures = content.AvailableCultures ?? [],
                    PublishedCultures = content.PublishedCultures ?? [],
                    EditedCultures = content.EditedCultures ?? []
                },
                Properties = this.PropertySummaries(content.Properties),
                IncomingRelations = this.RelationSummaries(id, incoming: true),
                OutgoingRelations = this.RelationSummaries(id, incoming: false),
                UsedBy = await this.GetUsedBy("Content Node", content.Key.ToString()),
                Uses = await this.GetUses("Content Node", content.Key.ToString()),
                AuditTrail = await this.AuditSummaries(id)
            };
        }

        public async Task<ContentMediaDetail?> GetMediaDetail(int id)
        {
            var media = this.mediaService.GetById(id);
            if (media is null)
            {
                return null;
            }

            var extension = FileTypeHelper.GetExtensionFromMedia(media);

            return new ContentMediaDetail
            {
                Kind = "Media",
                Id = media.Id,
                Key = media.Key,
                Name = media.Name ?? string.Empty,
                ContentTypeName = media.ContentType.Name ?? string.Empty,
                ContentTypeAlias = media.ContentType.Alias,
                Path = media.Path,
                Ancestors = this.MediaAncestors(media.Path, media.Id),
                ParentId = media.ParentId,
                Level = media.Level,
                Trashed = media.Trashed,
                CreateDate = media.CreateDate,
                UpdateDate = media.UpdateDate == default ? media.CreateDate : media.UpdateDate,
                MediaFile = new MediaFileDetail
                {
                    Extension = extension,
                    FileType = FileTypeHelper.GetFileTypeName(extension, media),
                    Size = FileTypeHelper.GetFileSize(media)
                },
                Properties = this.PropertySummaries(media.Properties),
                IncomingRelations = this.RelationSummaries(id, incoming: true),
                OutgoingRelations = this.RelationSummaries(id, incoming: false),
                UsedBy = await this.GetUsedBy("Media", media.Key.ToString()),
                Uses = await this.GetUses("Media", media.Key.ToString()),
                AuditTrail = await this.AuditSummaries(id)
            };
        }

        private IEnumerable<PropertyValueSummary> PropertySummaries(IPropertyCollection properties)
            => properties.Select(property => new PropertyValueSummary
            {
                Alias = property.Alias,
                Name = property.PropertyType.Name ?? property.Alias,
                EditorAlias = property.PropertyType.PropertyEditorAlias,
                StorageType = property.ValueStorageType.ToString(),
                Mandatory = property.PropertyType.Mandatory,
                Variations = property.PropertyType.Variations.ToString(),
                ValueCount = property.Values.Count,
                HasEditedValue = property.Values.Any(value => !this.IsEmptyPropertyValue(value.EditedValue)),
                HasPublishedValue = property.Values.Any(value => !this.IsEmptyPropertyValue(value.PublishedValue)),
                Cultures = property.Values
                    .Select(value => value.Culture)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .Distinct()
                    .OrderBy(x => x)
            }).OrderBy(x => x.Alias);

        private IEnumerable<AncestorPathItem> ContentAncestors(string path, int currentId)
        {
            var pathIds = ParsePathIds(path).ToList();
            var items = this.contentService.GetByIds(pathIds.Where(id => id > 0)).ToDictionary(item => item.Id);

            return pathIds.Select(id =>
            {
                if (id == -1)
                {
                    return RootAncestor(id, currentId);
                }

                if (!items.TryGetValue(id, out var item))
                {
                    return MissingAncestor(id, currentId);
                }

                return new AncestorPathItem
                {
                    Id = item.Id,
                    Key = item.Key,
                    Name = item.Name ?? $"#{item.Id}",
                    Alias = item.ContentType.Alias,
                    Level = item.Level,
                    IsCurrent = item.Id == currentId
                };
            });
        }

        private IEnumerable<AncestorPathItem> MediaAncestors(string path, int currentId)
        {
            var pathIds = ParsePathIds(path).ToList();
            var items = this.mediaService.GetByIds(pathIds.Where(id => id > 0)).ToDictionary(item => item.Id);

            return pathIds.Select(id =>
            {
                if (id == -1)
                {
                    return RootAncestor(id, currentId);
                }

                if (!items.TryGetValue(id, out var item))
                {
                    return MissingAncestor(id, currentId);
                }

                return new AncestorPathItem
                {
                    Id = item.Id,
                    Key = item.Key,
                    Name = item.Name ?? $"#{item.Id}",
                    Alias = item.ContentType.Alias,
                    Level = item.Level,
                    IsCurrent = item.Id == currentId
                };
            });
        }

        private static IEnumerable<int> ParsePathIds(string path)
            => (path ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value);

        private static AncestorPathItem RootAncestor(int id, int currentId)
            => new()
            {
                Id = id,
                Name = "Root",
                Alias = "root",
                Level = 0,
                IsRoot = true,
                IsCurrent = id == currentId
            };

        private static AncestorPathItem MissingAncestor(int id, int currentId)
            => new()
            {
                Id = id,
                Name = $"Missing item #{id}",
                Alias = "missing",
                IsCurrent = id == currentId
            };

        private bool IsEmptyPropertyValue(object? value)
            => value is null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue));

        private IEnumerable<RelationSummary> RelationSummaries(int id, bool incoming)
        {
            var relations = incoming ? this.relationService.GetByChildId(id) : this.relationService.GetByParentId(id);

            return relations
                .Take(25)
                .Select(relation =>
                {
                    var related = incoming
                        ? this.relationService.GetParentEntityFromRelation(relation)
                        : this.relationService.GetChildEntityFromRelation(relation);

                    return new RelationSummary
                    {
                        Direction = incoming ? "Incoming" : "Outgoing",
                        RelationTypeAlias = relation.RelationType.Alias,
                        RelationTypeName = relation.RelationType.Name,
                        RelatedId = related?.Id ?? (incoming ? relation.ParentId : relation.ChildId),
                        RelatedKey = related?.Key ?? Guid.Empty,
                        RelatedName = related?.Name ?? string.Empty,
                        RelatedPath = related?.Path ?? string.Empty,
                        Comment = relation.Comment ?? string.Empty
                    };
                })
                .OrderBy(x => x.RelationTypeAlias)
                .ThenBy(x => x.RelatedName);
        }

        private async Task<IEnumerable<AuditSummary>> AuditSummaries(int id)
        {
            var items = (await this.auditService.GetItemsByEntityAsync(id, 0, 10, Direction.Descending, [], null))
                .Items
                .ToList();
            var userNames = this.UserNamesById(items.Select(item => item.UserId));

            return items.Select(item => new AuditSummary
            {
                AuditType = item.AuditType.ToString(),
                EntityType = item.EntityType ?? string.Empty,
                UserId = item.UserId,
                UserName = userNames.TryGetValue(item.UserId, out var userName) ? userName : UserLabel(item.UserId),
                Comment = item.Comment ?? string.Empty,
                Parameters = item.Parameters ?? string.Empty
            });
        }

        private Dictionary<int, string> UserNamesById(IEnumerable<int> userIds)
        {
            var ids = userIds.Where(id => id > 0).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return [];
            }

            using var scope = this.scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.Fetch<UserLookup>("SELECT id, userName FROM umbracoUser WHERE id IN (@0)", ids)
                .Where(user => !string.IsNullOrWhiteSpace(user.UserName))
                .ToDictionary(user => user.Id, user => user.UserName);
        }

        private static string UserLabel(int userId)
            => userId <= 0 ? "System" : $"User #{userId}";

        private class UserLookup
        {
            public int Id { get; set; }

            public string UserName { get; set; } = string.Empty;
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

        /// <summary>
        /// Gets all tags for all cultures and maps them to their related content
        /// </summary>
        public async Task<IEnumerable<TagMapping>> GetTagMapping()
            => await this.GetCachedAsync(TagMappingCacheKey, TimeSpan.FromMinutes(1), this.BuildTagMapping);

        private async Task<IReadOnlyList<TagMapping>> BuildTagMapping()
        {
            var cultures = (await this.languageService.GetAllAsync()).Select(x => x.IsoCode).ToList(); // get all cultures

            cultures.Add(null); // plus invariant

            var tagMap = new List<TagMapping>();

            foreach (var culture in cultures)
            {
                var contentTags = this.tagService.GetAllContentTags(culture: culture);

                foreach (var tag in contentTags.OrderBy(t => t.Text))
                {
                    var taggedContent = this.contentService.GetByIds(this.tagService.GetTaggedContentByTag(tag.Text, culture: culture).Select(x => x.EntityId)).Select(c => new ContentTags()
                    {
                        Type = "Content",
                        Alias = c.ContentType.Alias,
                        Icon = c.ContentType.Icon,
                        Id = c.Id,
                        Name = c.Name,
                        Udi = c.Key,
                        Tags = this.tagService.GetTagsForEntity(c.Id, culture: culture).Select(x => new Models.Tag()
                        {
                            Id = x.Id,
                            Group = x.Group,
                            NodeCount = x.NodeCount,
                            Text = x.Text,
                            Culture = culture
                        })
                    });

                    tagMap.Add(new TagMapping()
                    {
                        Key = tag.Text,
                        Content = taggedContent,
                        Culture = culture,
                        Tag = new Models.Tag()
                        {
                            Group = tag.Group,
                            Id = tag.Id,
                            NodeCount = tag.NodeCount,
                            Text = tag.Text,
                            Culture = culture
                        }
                    });
                }

                var mediaTags = this.tagService.GetAllMediaTags(culture: culture);

                foreach (var tag in mediaTags.OrderBy(t => t.Text))
                {
                    var taggedContent = this.mediaService.GetByIds(this.tagService.GetTaggedMediaByTag(tag.Text, culture: culture).Select(x => x.EntityId)).Select(c => new ContentTags()
                    {
                        Type = "Media",
                        Alias = c.ContentType.Alias,
                        Icon = c.ContentType.Icon,
                        Id = c.Id,
                        Name = c.Name,
                        Udi = c.Key,
                        Tags = this.tagService.GetTagsForEntity(c.Id, culture: culture).Select(x => new Models.Tag()
                        {
                            Id = x.Id,
                            Group = x.Group,
                            NodeCount = x.NodeCount,
                            Text = x.Text,
                            Culture = culture
                        })
                    });

                    tagMap.Add(new TagMapping()
                    {
                        Key = tag.Text,
                        Content = taggedContent,
                        Culture = culture,
                        Tag = new Models.Tag()
                        {
                            Group = tag.Group,
                            Id = tag.Id,
                            NodeCount = tag.NodeCount,
                            Text = tag.Text,
                            Culture = culture
                        }
                    });
                }
            }

            return tagMap;
        }

        private async Task<IReadOnlyList<T>> GetCachedAsync<T>(string cacheKey, TimeSpan cacheDuration, Func<Task<IReadOnlyList<T>>> factory)
        {
            if (this.memoryCache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
            {
                return cached;
            }

            var built = await factory();
            this.memoryCache.Set(cacheKey, built, cacheDuration);
            return built;
        }

        private void ClearDiagnosticCaches()
        {
            this.memoryCache.Remove(ReferenceGraphCacheKey);
            this.memoryCache.Remove(SchemaReferenceGraphCacheKey);
            this.memoryCache.Remove(DataTypesStatusCacheKey);
            this.memoryCache.Remove(TemplatesCacheKey);
            this.memoryCache.Remove(ConfigurationDriftCacheKey);
            this.memoryCache.Remove(TagMappingCacheKey);
        }

        /// <summary>
        /// Used to copy a data type since this is missing in core
        /// </summary>
        /// <param name="id">The ID of the datatype being copied</param>
        /// <returns>A response</returns>
        public async Task<ServerResponse> CopyDataType(int id)
        {
            var dataTypeKey = this.idKeyMap.GetKeyForId(id, UmbracoObjectTypes.DataType);
            var dt = dataTypeKey.Success ? await this.dataTypeService.GetAsync(dataTypeKey.Result) : null;

            if (dt == null)
            {
                return new ServerResponse($"No datatype with Id of {id} was found", ServerResponseType.Error);
            }

            try
            {
                var copy = new DataType(dt.Editor, this.serializer, dt.ParentId)
                {
                    ConfigurationData = dt.ConfigurationData,
                    CreateDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow,
                    DatabaseType = dt.DatabaseType,
                    Name = dt.Name + " (Copy)"
                };

                var result = await this.dataTypeService.CreateAsync(copy, Constants.Security.SuperUserKey);

                if (result.Success is false)
                {
                    return new ServerResponse($"Could not create '{copy.Name}'", ServerResponseType.Error);
                }

                this.ClearDiagnosticCaches();
                return new ServerResponse($"Created '{copy.Name}' successfully", ServerResponseType.Success);
            }
            catch (Exception ex)
            {
                return new ServerResponse(ex.Message, ServerResponseType.Error);
            }
        }
    }
}
