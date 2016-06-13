using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
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
        /// <summary>
        /// Gets a mapping of content types (doc types)
        /// </summary>
        public IEnumerable<ContentTypeMap> GetContentTypeMap()
        {
            var cts = UmbracoContext.Application.Services.ContentTypeService;

            List<ContentTypeMap> mapping = new List<ContentTypeMap>();

            foreach (var ct in cts.GetAllContentTypes().OrderBy(x => x.Name))
            {
                ContentTypeMap map = new ContentTypeMap();

                map.Alias = ct.Alias;
                map.Icon = ct.Icon;
                map.Name = ct.Name;
                map.Id = ct.Id;
                map.Description = ct.Description;
                map.Templates = ct.AllowedTemplates.
                Select(x => new TemplateMap()
                {
                    Alias = x.Alias,
                    Id = x.Id,
                    Name = x.Name,
                    Path = x.VirtualPath,
                    IsDefault = ct.DefaultTemplate.Id == x.Id
                });

                map.Properties = ct.PropertyTypes.Select(p => new PropertyTypeMap(p));
                map.CompositionProperties = ct.CompositionPropertyTypes.Where(p => !ct.PropertyTypes.Select(x => x.Id).Contains(p.Id)).Select(pt => new PropertyTypeMap(pt));
                map.Compositions = ct.ContentTypeComposition.
                Select(x => new ContentTypeData()
                {
                    Alias = x.Alias,
                    Description = x.Description,
                    Id = x.Id,
                    Icon = x.Icon,
                    Name = x.Name
                });

                map.AllProperties = map.Properties.Concat(map.CompositionProperties);
                map.HasCompositions = ct.ContentTypeComposition.Any();
                map.HasTemplates = ct.AllowedTemplates.Any();
                map.IsListView = ct.IsContainer;
                map.AllowedAtRoot = ct.AllowedAsRoot;
                map.PropertyGroups = ct.PropertyGroups.Select(x => x.Name);

                mapping.Add(map);
            }

            return mapping;
        }

        /// <summary>
        /// Gets all property groups
        /// </summary>
        public IEnumerable<string> GetPropertyGroups()
        {
            var cts = UmbracoContext.Application.Services.ContentTypeService;
            return cts.GetAllContentTypes().
                SelectMany(x => x.PropertyGroups.Select(p => p.Name)).
                Distinct().OrderBy(p => p);
        }

        /// <summary>
        /// Gets all compositions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ContentTypeData> GetCompositions()
        {
            var cts = UmbracoContext.Application.Services.ContentTypeService;
            return cts.GetAllContentTypes().
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
            var dts = UmbracoContext.Application.Services.DataTypeService;
            return dts.GetAllDataTypeDefinitions().
                Select(x => new DataTypeMap() { Id = x.Id, Name = x.Name }).
                OrderBy(x => x.Name);
        }

        /// <summary>
        /// Gets all property editors
        /// </summary>
        public IEnumerable<DataTypeMap> GetPropertyEditors()
        {
            var dts = UmbracoContext.Application.Services.DataTypeService;
            return dts.GetAllDataTypeDefinitions().
                Select(x => new DataTypeMap() { Id = x.Id, Alias = x.PropertyEditorAlias }).
                DistinctBy(p => p.Alias).
                OrderBy(p => p.Alias);
        }

        /// <summary>
        /// Gets all data types, including the status of whether they are being used
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DataTypeMap> GetDataTypesStatus()
        {
            var cts = UmbracoContext.Application.Services.ContentTypeService;
            var dts = UmbracoContext.Application.Services.DataTypeService;

            var dataTypes = dts.GetAllDataTypeDefinitions();
            var contentTypes = cts.GetAllContentTypes();
            var usedPropertyTypes = contentTypes.SelectMany(x => x.PropertyTypes.Concat(x.CompositionPropertyTypes));
            var usedIds = dataTypes.Where(x => usedPropertyTypes.Select(y => y.DataTypeDefinitionId).Contains(x.Id)).Select(d => d.Id);

            return dts.GetAllDataTypeDefinitions().Select(x => new DataTypeMap()
            {
                Id = x.Id,
                Name = x.Name,
                Alias = x.PropertyEditorAlias,
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
            var fs = UmbracoContext.Application.Services.FileService;
            var templateModels = new List<TemplateModel>();
            var templates = fs.GetTemplates();

            foreach (var template in templates)
            {
                TemplateModel model = new TemplateModel(template)
                {
                    IsMaster = template.IsMasterTemplate,
                    MasterAlias = template.MasterTemplateAlias,
                    Partials = PartialHelper.GetPartialInfo(template.Content, template.Id, template.Alias),
                    Path = template.Path,
                    Parents = templates.Where(t => template.Path.Split(',').Select(x => Convert.ToInt32(x)).Contains(t.Id)).Select(t => new TemplateModel(t)).Reverse()
                };

                templateModels.Add(model);
            }

            return templateModels;
        }

        /// <summary>
        /// Gets all media (this can potentially return a lot - add pagination??)
        /// </summary>
        public IEnumerable<MediaMap> GetMedia()
        {
            var ms = UmbracoContext.Application.Services.MediaService;
            List<MediaMap> mediaMap = new List<MediaMap>();
            var rootMedia = ms.GetRootMedia();
            string tempExt;

            foreach (var m in rootMedia)
            {
                mediaMap.AddRange(ms.GetDescendants(m).
                    Select(x => new MediaMap()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Alias = x.ContentType.Name,
                        Ext = tempExt = FileTypeHelper.GetExtensionFromMedia(x),
                        Type = FileTypeHelper.GetFileTypeName(tempExt),
                        Size = FileTypeHelper.GetFileSize(x)
                    }
                ));
            }

            return mediaMap;
        }

        /// <summary>
        /// Gets all Surface Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetSurfaceControllers()
        {
            return GetReflectionTypeMapFrom(typeof(SurfaceController));
        }

        /// <summary>
        /// Gets all API Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetApiControllers()
        {
            return GetReflectionTypeMapFrom(typeof(UmbracoApiControllerBase));
        }

        /// <summary>
        /// Gets all Render MVC Controllers
        /// </summary>
        public IEnumerable<TypeMap> GetRenderMvcControllers()
        {
            return GetReflectionTypeMapFrom(typeof(IRenderMvcController));
        }

        /// <summary>
        /// Gets all PublishedContent Models
        /// </summary>
        public IEnumerable<TypeMap> GetPublishedContentModels()
        {
            return GetReflectionTypeMapFrom(typeof(PublishedContentModel));
        }

        /// <summary>
        /// Gets all property value converters
        /// </summary>
        public IEnumerable<TypeMap> GetPropertyValueConverters()
        {
            return GetReflectionTypeMapFrom(typeof(IPropertyValueConverter));
        }

        private static IEnumerable<TypeMap> GetReflectionTypeMapFrom(Type myType)
        {
            return ReflectionHelper.GetTypesAssignableFrom(myType).Select(t => new TypeMap(t));
        }

        public IEnumerable<DiagnosticSection> GetEnvironmentDiagnostics()
        {
            List<DiagnosticSection> sections = new List<DiagnosticSection>();

            DiagnosticSection section = new DiagnosticSection("Server Settings");
            section.Diagnostics.Add(new Diagnostic("Machine Name", Environment.MachineName));
            section.Diagnostics.Add(new Diagnostic("OS Version", Environment.OSVersion));
            section.Diagnostics.Add(new Diagnostic("Processor Count", Environment.ProcessorCount));
            section.Diagnostics.Add(new Diagnostic("Network Domain", Environment.UserDomainName));

            section.Diagnostics.Add(new Diagnostic("ASP.NET Version", Environment.Version));

            if (UmbracoContext.HttpContext != null && UmbracoContext.HttpContext.Request != null)
            {
                var serverVars = UmbracoContext.HttpContext.Request;

                if (serverVars != null)
                {
                    section.Diagnostics.Add(new Diagnostic("Web Server", serverVars["SERVER_SOFTWARE"]));
                    section.Diagnostics.Add(new Diagnostic("Host", serverVars["HTTP_HOST"]));
                    section.Diagnostics.Add(new Diagnostic("App Pool ID", serverVars["APP_POOL_ID"]));
                }
            }
            sections.Add(section);

            section = new DiagnosticSection("Converters");
            section.AddDiagnosticsFrom(typeof(IPropertyValueConverter));
            sections.Add(section);

            section = new DiagnosticSection("Database Settings");
            var dbc = UmbracoContext.Application.DatabaseContext;
            section.Diagnostics.Add(new Diagnostic("Database Provider", dbc.DatabaseProvider));
            section.Diagnostics.Add(new Diagnostic("Connection String", Regex.Replace(dbc.ConnectionString,  @"password(\W*)=(.+)(;|$)", "*******", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase)));
            sections.Add(section);

            section = new DiagnosticSection("App Settings");
            section.AddDiagnostics(ConfigurationManager.AppSettings);
            sections.Add(section);

            section = new DiagnosticSection("Umbraco Version");
            section.Diagnostics.Add(new Diagnostic("Current Version", Core.Configuration.UmbracoVersion.Current));
            section.Diagnostics.Add(new Diagnostic("Assembly Version", Core.Configuration.UmbracoVersion.AssemblyVersion));
            section.Diagnostics.Add(new Diagnostic("Current Comment", Core.Configuration.UmbracoVersion.CurrentComment));
            section.Diagnostics.Add(new Diagnostic("Semantic Version", Core.Configuration.UmbracoVersion.GetSemanticVersion().ToSemanticString()));
            sections.Add(section);

            section = new DiagnosticSection("Content");
            var cs = UmbracoConfig.For.UmbracoSettings().Content;
            section.Diagnostics.Add(new Diagnostic("Clone XML Content?", cs.CloneXmlContent));
            section.Diagnostics.Add(new Diagnostic("Continously Update XML Disk Cache?", cs.ContinouslyUpdateXmlDiskCache));
            section.Diagnostics.Add(new Diagnostic("Default Doc Type Property", cs.DefaultDocumentTypeProperty));
            section.Diagnostics.Add(new Diagnostic("Disallowed Upload Files", cs.DisallowedUploadFiles));
            section.Diagnostics.Add(new Diagnostic("Enable Inherited Doc Types?", cs.EnableInheritedDocumentTypes));
            section.Diagnostics.Add(new Diagnostic("Error 404 Page Ids", cs.Error404Collection.Select(x => x.ContentId.ToString())));
            section.Diagnostics.Add(new Diagnostic("Ensure Unique Naming?", cs.EnsureUniqueNaming));
            section.Diagnostics.Add(new Diagnostic("Force Safe Aliases?", cs.ForceSafeAliases));
            section.Diagnostics.Add(new Diagnostic("Global Preview Storage Enabled?", cs.GlobalPreviewStorageEnabled));
            section.Diagnostics.Add(new Diagnostic("Image AutoFill Aliases", cs.ImageAutoFillProperties.Select(x => x.Alias)));
            section.Diagnostics.Add(new Diagnostic("Image File Types", cs.ImageFileTypes));
            section.Diagnostics.Add(new Diagnostic("Image Tage Allowed Attributes", cs.ImageTagAllowedAttributes));
            section.Diagnostics.Add(new Diagnostic("Macro Error Behaviour", cs.MacroErrorBehaviour));
            section.Diagnostics.Add(new Diagnostic("Notification Email Address", cs.NotificationEmailAddress));
            section.Diagnostics.Add(new Diagnostic("Property Context Help Option", cs.PropertyContextHelpOption));
            section.Diagnostics.Add(new Diagnostic("Resolve URLs From TextString?", cs.ResolveUrlsFromTextString));
            section.Diagnostics.Add(new Diagnostic("Script Editor Disable?", cs.ScriptEditorDisable));
            section.Diagnostics.Add(new Diagnostic("Script Folder Path", cs.ScriptFolderPath));
            section.Diagnostics.Add(new Diagnostic("Script File Types", cs.ScriptFileTypes));
            section.Diagnostics.Add(new Diagnostic("Tidy Char Encoding", cs.TidyCharEncoding));
            section.Diagnostics.Add(new Diagnostic("Tidy Editor Content?", cs.TidyEditorContent));
            section.Diagnostics.Add(new Diagnostic("Umbraco Library Cache Duration", cs.UmbracoLibraryCacheDuration));
            section.Diagnostics.Add(new Diagnostic("Upload Allows Directories?", cs.UploadAllowDirectories));
            section.Diagnostics.Add(new Diagnostic("Use Legacy XML Schema?", cs.UseLegacyXmlSchema));
            section.Diagnostics.Add(new Diagnostic("XML Cache Enabled?", cs.XmlCacheEnabled));
            section.Diagnostics.Add(new Diagnostic("XML Content Check for Disk Changes?", cs.XmlContentCheckForDiskChanges));
            sections.Add(section);

            var dc = UmbracoConfig.For.UmbracoSettings().DistributedCall;
            section = new DiagnosticSection("Distributed Calls");
            section.Diagnostics.Add(new Diagnostic("Enabled?", dc.Enabled));
            section.Diagnostics.Add(new Diagnostic("User Id", dc.UserId));
            foreach (var server in dc.Servers)
            {
                section.Diagnostics.Add(new Diagnostic(server.ServerName, String.Format("AppID: {1}, Address: {1}", server.AppId, server.ServerAddress)));
            }
            sections.Add(section);

            var dev = UmbracoConfig.For.UmbracoSettings().Developer;
            section = new DiagnosticSection("Developer");
            section.Diagnostics.Add(new Diagnostic("App_Code File Extensions", dev.AppCodeFileExtensions.Select(x => x.Extension)));
            sections.Add(section);

            var lc = UmbracoConfig.For.UmbracoSettings().Logging;
            section = new DiagnosticSection("Logging");
            section.Diagnostics.Add(new Diagnostic("Enable Logging?", lc.EnableLogging));
            section.Diagnostics.Add(new Diagnostic("Enable Async Logging?", lc.EnableAsyncLogging));
            section.Diagnostics.Add(new Diagnostic("Auto Clean Logs?", lc.AutoCleanLogs));
            section.Diagnostics.Add(new Diagnostic("Cleaning Miliseconds", lc.CleaningMiliseconds));
            section.Diagnostics.Add(new Diagnostic("Disabled Log Types", lc.DisabledLogTypes.Select(x => x.LogTypeAlias)));
            section.Diagnostics.Add(new Diagnostic("External Logger Configured?", lc.ExternalLoggerIsConfigured));
            if (lc.ExternalLoggerIsConfigured)
            {
                section.Diagnostics.Add(new Diagnostic("External Logger Assembly", lc.ExternalLoggerAssembly));
                section.Diagnostics.Add(new Diagnostic("External Logger Type", lc.ExternalLoggerType));
                section.Diagnostics.Add(new Diagnostic("External Logger Enabled Audit Trail", lc.ExternalLoggerEnableAuditTrail));
            }
            sections.Add(section);

            var pack = UmbracoConfig.For.UmbracoSettings().PackageRepositories;
            section = new DiagnosticSection("Package Repositories");
            foreach (var repo in pack.Repositories)
            {
                section.Diagnostics.Add(new Diagnostic(repo.Name, repo.RepositoryUrl));
            }
            sections.Add(section);

            var pro = UmbracoConfig.For.UmbracoSettings().Providers;
            section = new DiagnosticSection("Providers");
            section.Diagnostics.Add(new Diagnostic("Default BackOffice User Provider", pro.DefaultBackOfficeUserProvider));
            sections.Add(section);

            var rh = UmbracoConfig.For.UmbracoSettings().RequestHandler;
            section = new DiagnosticSection("Request Handler");
            section.Diagnostics.Add(new Diagnostic("Add Trailing Slash?", rh.AddTrailingSlash));
            section.Diagnostics.Add(new Diagnostic("Char Replacements?", rh.CharCollection.Select(c => String.Format("{0} {1}", c.Replacement, c.Char))));
            section.Diagnostics.Add(new Diagnostic("Convert URLs to ASCII?", rh.ConvertUrlsToAscii));
            section.Diagnostics.Add(new Diagnostic("Remove Double Slashes?", rh.RemoveDoubleDashes));
            section.Diagnostics.Add(new Diagnostic("Use Domain Prefixes?", rh.UseDomainPrefixes));
            sections.Add(section);

            var st = UmbracoConfig.For.UmbracoSettings().ScheduledTasks;
            section = new DiagnosticSection("Scheduled Tasks");
            section.Diagnostics.Add(new Diagnostic("Base URL", st.BaseUrl));

            foreach (var task in st.Tasks)
            {
                section.Diagnostics.Add(new Diagnostic(task.Alias, String.Format("{0} ({1} secs)", task.Url, task.Interval)));
            }
            sections.Add(section);

            var sec = UmbracoConfig.For.UmbracoSettings().Security;
            section = new DiagnosticSection("Security");
            section.Diagnostics.Add(new Diagnostic("Auth Cookie Domain", sec.AuthCookieDomain));
            section.Diagnostics.Add(new Diagnostic("Auth Cookie Name", sec.AuthCookieName));
            section.Diagnostics.Add(new Diagnostic("Hide Disabled Users?", sec.HideDisabledUsersInBackoffice));
            section.Diagnostics.Add(new Diagnostic("Keep User Logged In?", sec.KeepUserLoggedIn));
            sections.Add(section);

            var temps = UmbracoConfig.For.UmbracoSettings().Templates;
            section = new DiagnosticSection("Templates");
            section.Diagnostics.Add(new Diagnostic("Default Rendering Engine", temps.DefaultRenderingEngine));
            section.Diagnostics.Add(new Diagnostic("Enabled Skin Support?", temps.EnableSkinSupport));
            section.Diagnostics.Add(new Diagnostic("Use ASP.NET Master Pages?", temps.UseAspNetMasterPages));
            sections.Add(section);

            var wr = UmbracoConfig.For.UmbracoSettings().WebRouting;
            section = new DiagnosticSection("Web Routing");
            section.Diagnostics.Add(new Diagnostic("Disable Alternative Templates?", wr.DisableAlternativeTemplates));
            section.Diagnostics.Add(new Diagnostic("Disable Find Content By Id Path?", wr.DisableFindContentByIdPath));
            section.Diagnostics.Add(new Diagnostic("Internal Redirect Preserves Template?", wr.InternalRedirectPreservesTemplate));
            section.Diagnostics.Add(new Diagnostic("Try Skip IIS Custom Errors?", wr.TrySkipIisCustomErrors));
            section.Diagnostics.Add(new Diagnostic("Umbraco Application URL", wr.UmbracoApplicationUrl));
            section.Diagnostics.Add(new Diagnostic("URL Provider Mode", wr.UrlProviderMode));
            sections.Add(section);

            var br = UmbracoConfig.For.BaseRestExtensions();
            section = new DiagnosticSection("Base Rest Extensions");
            section.Diagnostics.Add(new Diagnostic("Enabled?", br.Enabled));
            foreach (var item in br.Items)
            {
                section.Diagnostics.Add(new Diagnostic(item.Alias, item.Type));
            }
            sections.Add(section);

            return sections;
        }
    }
}
