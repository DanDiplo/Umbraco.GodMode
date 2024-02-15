﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// API Controller for returning JSON to the GodMode views in /App_Plugins/
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    public class GodModeApiController : UmbracoAuthorizedJsonController
    {
        private readonly IUmbracoDataService dataService;
        private readonly IUmbracoDatabaseService dataBaseService;
        private readonly IDiagnosticService diagnosticService;
        private readonly IUtilitiesService utilitiesService;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly NuCacheSettings nuCacheSettings;
        private readonly RegisteredServiceCollection registeredServiceCollection;
        private readonly IOptions<GodModeConfig> godModeConfig;

        public GodModeApiController(IUmbracoDataService dataService, IUmbracoDatabaseService dataBaseService, IDiagnosticService diagnosticService, IUtilitiesService utilitiesService, IHostApplicationLifetime applicationLifetime, IOptions<NuCacheSettings> nuCacheSettings, RegisteredServiceCollection registeredServiceCollection, IOptions<GodModeConfig> godModeConfig)
        {
            this.dataService = dataService;
            this.dataBaseService = dataBaseService;
            this.diagnosticService = diagnosticService;
            this.utilitiesService = utilitiesService;
            this.applicationLifetime = applicationLifetime;
            this.nuCacheSettings = nuCacheSettings.Value;
            this.registeredServiceCollection = registeredServiceCollection;
            this.godModeConfig = godModeConfig;
        }

        /// <summary>
        /// Gets a mapping of content types (doc types)
        /// </summary>
        public IEnumerable<ContentTypeMap> GetContentTypeMap()
        {
            return dataService.GetContentTypeMap();
        }

        /// <summary>
        /// Gets all property groups
        /// </summary>
        public IEnumerable<string> GetPropertyGroups()
        {
            return dataService.GetPropertyGroups();
        }

        /// <summary>
        /// Gets all compositions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ContentTypeData> GetCompositions()
        {
            return dataService.GetCompositions();
        }

        /// <summary>
        /// Gets all data types
        /// </summary>
        public IEnumerable<DataTypeMap> GetDataTypes()
        {
            return dataService.GetDataTypes();
        }

        /// <summary>
        /// Gets all property editors
        /// </summary>
        public IEnumerable<DataTypeMap> GetPropertyEditors()
        {
            return dataService.GetPropertyEditors();
        }

        /// <summary>
        /// Gets all data types, including the status of whether they are being used
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DataTypeMap> GetDataTypesStatus()
        {
            return dataService.GetDataTypesStatus();
        }

        /// <summary>
        /// Gets all templates
        /// </summary>
        public IEnumerable<TemplateModel> GetTemplates()
        {
            return dataService.GetTemplates();
        }

        /// <summary>
        /// Gets all media paged
        /// </summary>
        public Page<MediaMap> GetMedia(long page = 1, int pageSize = 3, string name = null, int? id = null, int? mediaTypeId = null, string orderBy = "Id", string orderByDir = "ASC")
        {
            return dataService.GetMediaPaged(page, pageSize, name, id, mediaTypeId, orderBy, orderByDir);
        }

        public IEnumerable<ItemBase> GetMediaTypes()
        {
            return dataService.GetMediaTypes();
        }

        public IEnumerable<Lang> GetLanguages()
        {
            return dataBaseService.GetLanguages();
        }

        /// <summary>
        /// Gets all content paged
        /// </summary>
        /// <param name="page">The current page</param>
        /// <param name="pageSize">The items per page</param>
        /// <param name="criteria"></param>
        public Page<ContentItem> GetContentPaged(long page = 1, long pageSize = 50, string name = null, string alias = null, int? creatorId = null, string id = null, int? level = null, bool? trashed = null, int? updaterId = null, int? languageId = null, string orderBy = "N.id")
        {
            var criteria = new ContentCriteria
            {
                Name = name,
                Alias = alias,
                CreatorId = creatorId,
                Id = id,
                Level = level,
                Trashed = trashed,
                UpdaterId = updaterId,
                LanguageId = languageId
            };

            return dataBaseService.GetContent(page, pageSize, criteria, orderBy);
        }

        /// <summary>
        /// Gets all content-type aliases
        /// </summary>
        public IEnumerable<string> GetContentTypeAliases()
        {
            return dataBaseService.GetContentTypeAliases();
        }

        public IEnumerable<string> GetStandardContentTypeAliases()
        {
            return dataBaseService.GetContentTypeAliases(isElement: false);
        }

        /// <summary>
        /// Gets all Surface Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetSurfaceControllers()
        {
            var data = ReflectionHelper.GetTypeMapFrom(typeof(SurfaceController));
            return data;
        }

        /// <summary>
        /// Gets all API Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetApiControllers()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(UmbracoApiControllerBase));
        }

        /// <summary>
        /// Gets all Render MVC Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetRenderMvcControllers()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(IRenderController));
        }

        /// <summary>
        /// Gets all PublishedContent Models
        /// </summary>
        public IEnumerable<TypeMap> GetPublishedContentModels()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(PublishedContentModel));
        }

        /// <summary>
        /// Gets all property value converters
        /// </summary>
        public IEnumerable<TypeMap> GetPropertyValueConverters()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(IPropertyValueConverter));
        }

        /// <summary>
        /// Gets all Composers
        /// </summary>
        public IEnumerable<TypeMap> GetComposers()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(IComposer));
        }

        /// <summary>
        /// Gets all View Components
        /// </summary>
        public IEnumerable<TypeMap> GetViewComponents()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(ViewComponent));
        }

        /// <summary>
        /// Gets all Content Finders
        /// </summary>
        public IEnumerable<TypeMap> GetContentFinders()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(IContentFinder));
        }

        /// <summary>
        /// Gets all URL Providers
        /// </summary>
        public IEnumerable<TypeMap> GetUrlProviders()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(IUrlProvider));
        }

        /// <summary>
        /// Gets all Tag Helpers
        /// </summary>
        public IEnumerable<TypeMap> GetTagHelpers()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(ITagHelper)).Where(x => !x.Name.StartsWith("__Generated"));
        }

        /// <summary>
        /// Gets registered services
        /// </summary>
        public IEnumerable<RegisteredService> GetRegisteredServices()
        {
            return this.registeredServiceCollection.Services.Value;
        }

        /// <summary>
        /// Gets a type mapping of types assignable from a type passed as a string
        /// </summary>
        public IEnumerable<TypeMap> GetTypesAssignableFrom(string baseType)
        {
            return ReflectionHelper.GetTypeMapFrom(Type.GetType(baseType));
        }

        /// <summary>
        /// Gets diagnostics and settings info
        /// </summary>
        public IEnumerable<DiagnosticGroup> GetEnvironmentDiagnostics()
        {
            return diagnosticService.GetDiagnosticGroups();
        }

        /// <summary>
        /// Get all assemblies that seem to be Umbraco assemblies
        /// </summary>
        public IEnumerable<NameValue> GetUmbracoAssemblies()
        {
            return ReflectionHelper.GetUmbracoAssemblies().Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        /// <summary>
        /// Get all assemblies that aren't Microsoft ones
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NameValue> GetNonMsAssemblies()
        {
            return ReflectionHelper.GetAssemblies().Where(a => !a.IsDynamic && !a.FullName.StartsWith("Microsoft.") && !a.FullName.StartsWith("System")).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        /// <summary>
        /// Get all assemblies
        /// </summary>
        public IEnumerable<NameValue> GetAssemblies()
        {
            return ReflectionHelper.GetAssemblies(a => !a.IsDynamic).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        /// <summary>
        /// Get all assemblies that contain at least one interface
        /// </summary>
        /// <returns></returns>
        public IEnumerable<NameValue> GetAssembliesWithInterfaces()
        {
            return ReflectionHelper.GetAssemblies(a => !a.IsDynamic && a.GetLoadableTypes().Any(t => t.IsInterface && !t.IsGenericTypeDefinition && t.IsPublic)).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        /// <summary>
        /// Get all interfaces from a named assembly
        /// </summary>
        /// <param name="assembly">The qualified assmebly name</param>
        public IEnumerable<TypeMap> GetInterfacesFrom(string assembly)
        {
            return ReflectionHelper.GetNonGenericInterfaces(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>();
        }

        /// <summary>
        /// Gets all types from a name assembly
        /// </summary>
        /// <param name="assembly">The qualified assmebly name</param>
        public IEnumerable<TypeMap> GetTypesFrom(string assembly)
        {
            return ReflectionHelper.GetNonGenericTypes(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>();
        }

        /// <summary>
        /// Gets the URL of a single page for each unique template on the site
        /// </summary>
        /// <returns>A list of URLs</returns>
        public IEnumerable<string> GetTemplateUrlsToPing()
        {
            return dataBaseService.GetTemplateUrlsToPing();
        }

        /// <summary>
        /// Gets all site URLs for a given culture
        /// </summary>
        /// <returns>A list of URLs</returns>
        public IEnumerable<string> GetUrlsToPing(string culture)
        {
            return utilitiesService.GetAllUrls(culture);
        }

        /// <summary>
        /// Gets the GodMode configuration
        /// </summary>
        /// <returns></returns>
        public GodModeConfig GetConfig()
        {
            return godModeConfig.Value;
        }

        /// <summary>
        /// Attemps to fix template masters
        /// </summary>
        /// <returns>A count</returns>
        [HttpPost]
        public int FixTemplateMasters()
        {
            return dataService.FixTemplateMasters();
        }

        /// <summary>
        /// Gets a list of content types and a count of their usage
        /// </summary>
        /// <param name="id">Optional Id of the content type to filter by</param>
        /// <param name="orderBy">Optional order by parameter</param>
        /// <returns>A list of content usage</returns>
        public IEnumerable<UsageModel> GetContentUsageData(int? id = null, string orderBy = null)
        {
            return dataBaseService.GetContentUsageData(id, orderBy);
        }

        /// <summary>
        /// Gets all tags and the content tagged by the tag
        /// </summary>
        /// <returns>A dictionary of tagliciousness</returns>
        public IEnumerable<TagMapping> GetTagMapping()
        {
            return dataService.GetTagMapping();
        }

        /// <summary>
        /// Clears the internal Umbraco cache's
        /// </summary>
        /// <param name="cache">The cache name to clear</param>
        [HttpPost]
        public ServerResponse ClearUmbracoCache(string cache)
        {
            return utilitiesService.ClearUmbracoCacheFor(cache);
        }

        /// <summary>
        /// Clears the Media Cache
        /// </summary>
        [HttpPost]
        public async Task<ServerResponse> PurgeMediaCache()
        {
            return await utilitiesService.ClearMediaFileCacheAsync();
        }

        /// <summary>
        /// Restarts the application
        /// </summary>
        [HttpPost]
        public ServerResponse RestartAppPool()
        {
            try
            {
                applicationLifetime?.StopApplication();

                return new ServerResponse("Restarting the application - hold tight...", ServerResponseType.Success);
            }
            catch (Exception ex)
            {
                return new ServerResponse("Error restarting the application: " + ex.Message, ServerResponseType.Error);
            }
        }

        public Page<MemberModel> GetMembersPaged(long page = 1, long pageSize = 50, int? groupId = null, string search = null, string orderBy = "MN.text")
        {
            return this.dataBaseService.GetMembers(page, pageSize, groupId, search, orderBy);
        }

        public IEnumerable<MemberGroupModel> GetMemberGroups()
        {
            return this.dataBaseService.GetMemberGroups();
        }

        public NuCacheItem GetNuCacheItem(int id)
        {
            return this.dataBaseService.GetNuCacheItem(id);
        }

        public string GetNuCacheType()
        {
            return this.nuCacheSettings.NuCacheSerializerType.ToString();
        }

        [HttpPost]
        public bool DeleteTag(int id)
        {
            return this.dataBaseService.DeleteTag(id);
        }

        public List<Models.Tag> GetOrphanedTags()
        {
            return this.dataBaseService.GetOrphanedTags();
        }

        [HttpPost]
        public ServerResponse CopyDataType(int id)
        {
            return this.dataService.CopyDataType(id);
        }


    }
}