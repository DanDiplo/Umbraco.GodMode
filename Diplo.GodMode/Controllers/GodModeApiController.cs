using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Umbraco.Core;
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

        /// <summary>
        /// Gets diagnostics and settings info
        /// </summary>
        public IEnumerable<DiagnosticGroup> GetEnvironmentDiagnostics()
        {
            DiagnosticService service = new DiagnosticService(Umbraco);
            return service.GetDiagnosticGroups();
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
            return ReflectionHelper.GetAssemblies(a => !a.IsDynamic && a.GetTypes().Any(t => t.IsInterface && !t.IsGenericTypeDefinition && t.IsPublic)).Select(a => new NameValue(a.GetName().Name, a.FullName)).OrderBy(x => x.Name);
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
    }
}
