using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Core = Umbraco.Core;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// API Controller for returning JSON to the GodMode views in /App_Plugins/
    /// </summary>
    [UmbracoApplicationAuthorize(Constants.Applications.Developer)]
    public class GodModeApiController : UmbracoAuthorizedJsonController
    {
        UmbracoDataService dataService;

        public GodModeApiController()
        {
            dataService = new UmbracoDataService(Umbraco);
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
        /// Gets all media (this can potentially return a lot - add pagination??)
        /// </summary>
        public IEnumerable<MediaMap> GetMedia()
        {
            return dataService.GetMedia();
        }

        /// <summary>
        /// Gets all Surface Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetSurfaceControllers()
        {
            return ReflectionHelper.GetTypeMapFrom(typeof(SurfaceController));
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
        /// Gets a type mapping of types assignable from a type passed as a string
        /// </summary>
        public IEnumerable<TypeMap> GetTypesAssignableFrom(string baseType)
        {
            return ReflectionHelper.GetTypeMapFrom(Type.GetType(baseType));
        }

        public IEnumerable<NameValue> GetTypesToBrowse()
        {
            List<Type> types = new List<Type>()
            {
                typeof(umbraco.interfaces.IAction),
                typeof(umbraco.interfaces.ICacheRefresher),
                typeof(umbraco.interfaces.INotFoundHandler),
                typeof(umbraco.interfaces.IApplication),
                typeof(Umbraco.Core.IApplicationEventHandler),
                typeof(Umbraco.Core.IBootManager),
                typeof(Umbraco.Core.Cache.ICacheProvider),
                typeof(Umbraco.Core.Cache.IRuntimeCacheProvider),
                typeof(Umbraco.Core.Configuration.IUmbracoConfigurationSection),
                typeof(Umbraco.Core.Dictionary.ICultureDictionary),
                typeof(Umbraco.Core.IDisposeOnRequestEnd),
                typeof(Umbraco.Core.IO.IFileSystem),
                typeof(Umbraco.Core.Logging.ILogger),
                typeof(Umbraco.Core.Media.IEmbedProvider),
                typeof(Umbraco.Core.Models.IFile),
                typeof(Umbraco.Core.Models.IContentBase),
                typeof(Umbraco.Core.Models.IDeepCloneable),
                typeof(Umbraco.Core.Models.IPublishedContent),
                typeof(Umbraco.Core.Models.IPublishedProperty),
                typeof(Umbraco.Core.Models.EntityBase.IAggregateRoot),
                typeof(Umbraco.Core.Models.EntityBase.ICanBeDirty),
                typeof(Umbraco.Core.Models.EntityBase.IEntity),
                typeof(Umbraco.Core.Models.EntityBase.IRememberBeingDirty),
                typeof(Umbraco.Core.Models.EntityBase.IUmbracoEntity),
                typeof(Umbraco.Core.Models.EntityBase.IValueObject),
                typeof(Umbraco.Core.Models.Identity.IIdentityUserLogin),
                typeof(Umbraco.Core.Models.Mapping.IMapperConfiguration),
                typeof(Umbraco.Core.Models.Membership.IMembershipUser),
                typeof(Umbraco.Core.Models.PublishedContent.IPublishedContentModelFactory),
                typeof(Umbraco.Core.Persistence.Migrations.IMigration),
                typeof(Umbraco.Core.Persistence.Repositories.IRepository),
                typeof(Umbraco.Core.Persistence.SqlSyntax.ISqlSyntaxProvider),
                typeof(Umbraco.Core.Persistence.UnitOfWork.IUnitOfWork),
                typeof(Umbraco.Core.Profiling.IProfiler),
                typeof(Umbraco.Core.PropertyEditors.IPropertyValueConverter),
                typeof(Umbraco.Core.PropertyEditors.IValueEditor),
                typeof(Umbraco.Core.Publishing.IPublishingStrategy),
                typeof(Umbraco.Core.Security.IUmbracoMemberTypeMembershipProvider),
                typeof(Umbraco.Core.Services.IService),
                typeof(Umbraco.Core.Strings.IUrlSegmentProvider),
                typeof(Umbraco.Core.Sync.IServerMessenger),
                typeof(Umbraco.Core.Sync.IServerRegistrar),
                typeof(Umbraco.Web.Routing.IContentFinder),
                typeof(Umbraco.Web.Routing.IUrlProvider),
                typeof(Umbraco.Web.Trees.ISearchableTree),
                typeof(Umbraco.Web.UI.IAssignedApp),
                typeof(Umbraco.Web.UI.Pages.BasePage),
                typeof(Umbraco.Web.UI.Pages.UmbracoEnsuredPage),
                typeof(Umbraco.Web.WebApi.UmbracoApiController),
                typeof(Umbraco.Web.WebApi.UmbracoAuthorizedApiController),
                typeof(umbraco.BusinessLogic.Interfaces.ILog),
                typeof(umbraco.DataLayer.ISqlHelper),
                typeof(umbraco.MacroEngines.IRazorDataTypeModel),
                typeof(IComparable),
                typeof(IConvertible),
                typeof(IRenderController),
                typeof(Examine.IIndexer),
                typeof(Examine.ISearcher),
                typeof(Examine.SearchCriteria.IQuery),
                typeof(Examine.IIndexField),
                typeof(IFilteredControllerFactory),
                typeof(UmbracoExamine.BaseUmbracoIndexer)
            };

            //var types = ReflectionHelper.GetLoadableUmbracoTypes().Where(t => t.IsInterface && !t.IsGenericType).OrderBy(t => t.Name);

            return types.Select(t => new NameValue(t.Name, t.GetFullNameWithAssembly())).OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets diagnostics and settings info
        /// </summary>
        public IEnumerable<DiagnosticGroup> GetEnvironmentDiagnostics()
        {
            DiagnosticService service = new DiagnosticService(Umbraco);
            return service.GetDiagnosticGroups();
        }

        public IEnumerable<NameValue> GetUmbracoAssemblies()
        {
            return ReflectionHelper.GetUmbracoAssemblies().Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        public IEnumerable<NameValue> GetNonMsAssemblies()
        {
            return ReflectionHelper.GetAssemblies().Where(a => !a.IsDynamic && !a.FullName.StartsWith("Microsoft.") && !a.FullName.StartsWith("System")).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        public IEnumerable<NameValue> GetAssemblies()
        {
            return ReflectionHelper.GetAssemblies(a => !a.IsDynamic).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        public IEnumerable<NameValue> GetAssembliesWithInterfaces()
        {
            return ReflectionHelper.GetAssemblies(a => !a.IsDynamic && a.GetTypes().Any(t => t.IsInterface && !t.IsGenericTypeDefinition && t.IsPublic)).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
        }

        public IEnumerable<TypeMap> GetInterfacesFrom(string assembly)
        {
            return ReflectionHelper.GetNonGenericInterfaces(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>();
        }


        public IEnumerable<TypeMap> GetTypesFrom(string assembly)
        {
            return ReflectionHelper.GetNonGenericTypes(Assembly.Load(assembly)).OrderBy(i => i.Name) ?? Enumerable.Empty<TypeMap>();
        }
    }
}
