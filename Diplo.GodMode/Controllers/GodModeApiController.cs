using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using NPoco;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Persistence;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// API Controller for returning JSON to the GodMode views in /App_Plugins/
    /// </summary>
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class GodModeApiController : UmbracoAuthorizedJsonController
    {
        private readonly IUmbracoDataService dataService;
        private readonly IUmbracoDatabaseService dataBaseService;
        private readonly IDiagnosticService diagnosticService;
        private readonly AppCaches caches;

        public GodModeApiController(IUmbracoDataService dataService, IUmbracoDatabaseService dataBaseService, IDiagnosticService diagnosticService, AppCaches caches)
        {
            this.dataService = dataService;
            this.dataBaseService = dataBaseService;
            this.diagnosticService = diagnosticService;
            this.caches = caches;
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
            return ReflectionHelper.GetTypeMapFrom(typeof(IRenderMvcController));
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
        /// Clears the internal Umbraco cache's
        /// </summary>
        /// <param name="cache">The cache name to clear</param>
        [HttpPost]
        public ServerResponse ClearUmbracoCache(string cache)
        {
            return ClearCacheFor(cache);
        }

        /// <summary>
        /// Restarts the ASP.NET application pool
        /// </summary>
        [HttpPost]
        public ServerResponse RestartAppPool()
        {
            try
            {
                UmbracoApplication.Restart();
                return new ServerResponse("Restarting the application pool - hold tight...", ServerResponseType.Success);
            }
            catch (Exception ex)
            {
                return new ServerResponse("Error restarting the application pool: " + ex.Message, ServerResponseType.Error);
            }
        }

        [HttpPost]
        public ServerResponse BumpClientDependencyVersion()
        {
            try
            {
                var path = HttpContext.Current.Server.MapPath("~/config/clientdependency.config");
                var xmlDocument = new XmlDocument { PreserveWhitespace = true };
                xmlDocument.Load(path);

                var node = xmlDocument.SelectSingleNode("//clientDependency/@version");
                if (node != null)
                {
                    if (int.TryParse(node.Value, out int version))
                    {
                        node.Value = (version + 1).ToString();
                    }

                    xmlDocument.Save(path);
                    return new ServerResponse("Successfully bumped Client Dependency version - hold tight...", ServerResponseType.Success);
                }
                else
                {
                    return new ServerResponse("Error bumping Client Dependency version: No version found in clientdependency.config", ServerResponseType.Error);
                }
            }
            catch (Exception ex)
            {
                return new ServerResponse("Error bumping Client Dependency version: " + ex.Message, ServerResponseType.Error);
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


        private ServerResponse ClearCacheFor(string cache)
        {
            try
            {
                if (cache == "Request" || cache == "all")
                {
                    caches.RequestCache.Clear();
                }
                else if (cache == "Runtime" || cache == "all")
                {
                    caches.RuntimeCache.Clear();
                }
                else if (cache == "Isolated" || cache == "all")
                {
                    caches.IsolatedCaches.ClearAllCaches();
                }
                else if (cache == "Partial" || cache == "all")
                {
                    caches.ClearPartialViewCache();
                }
                else
                {
                    return new ServerResponse(cache + " Is not a valid cache type", ServerResponseType.Warning);
                }

                if (cache == "all")
                {
                    return new ServerResponse("All Caches were successfully cleared", ServerResponseType.Success);
                }
                else
                {
                    return new ServerResponse("The " + cache + " Cache was successfully cleared", ServerResponseType.Success);
                }
            }
            catch (Exception ex)
            {
                return new ServerResponse("Error clearing cache: " + ex.Message, ServerResponseType.Error);
            }
        }
    }
}