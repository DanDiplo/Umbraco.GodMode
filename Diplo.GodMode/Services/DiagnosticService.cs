using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Examine.LuceneEngine.Providers;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Class for retieving diagnostic and setting information
    /// </summary>
    public class DiagnosticService
    {
        private UmbracoHelper umbHelper;

        private UmbracoContext umbContext;

        private HttpContextBase httpContext;

        private static readonly string[] ServerVarsToSkip = new string[] { "ALL_HTTP", "ALL_RAW", "HTTP_COOKIE", "HTTP_X_XSRF_TOKEN" };

        public DiagnosticService(UmbracoHelper umbHelper)
        {
            this.umbHelper = umbHelper;
            this.umbContext = umbHelper.UmbracoContext;
            this.httpContext = this.umbContext.HttpContext;
        }

        public IEnumerable<DiagnosticGroup> GetDiagnosticGroups()
        {
            List<DiagnosticGroup> groups = new List<DiagnosticGroup>();
            int id = 0;

            var sections = new List<DiagnosticSection>();
            var group = new DiagnosticGroup(id++, "Umbraco Configuration");

            var section = new DiagnosticSection("Umbraco Version");
            section.Diagnostics.Add(new Diagnostic("Current Version", UmbracoVersion.Current));
            section.Diagnostics.Add(new Diagnostic("Assembly Version", UmbracoVersion.AssemblyVersion));
            section.Diagnostics.Add(new Diagnostic("Current Comment", UmbracoVersion.CurrentComment));
            section.Diagnostics.Add(new Diagnostic("Semantic Version", UmbracoVersion.GetSemanticVersion().ToSemanticString()));
            sections.Add(section);

            section = new DiagnosticSection("Umbraco Settings");
            section.Diagnostics.Add(new Diagnostic("Debug Mode?", umbraco.GlobalSettings.DebugMode));
            section.Diagnostics.Add(new Diagnostic("Trust Level", umbraco.GlobalSettings.ApplicationTrustLevel));
            section.Diagnostics.Add(new Diagnostic("XML Content File", umbraco.GlobalSettings.ContentXML));
            section.Diagnostics.Add(new Diagnostic("Storage Directory", umbraco.GlobalSettings.StorageDirectory));
            section.Diagnostics.Add(new Diagnostic("Default UI Language", umbraco.GlobalSettings.DefaultUILanguage));
            section.Diagnostics.Add(new Diagnostic("Profile URL", umbraco.GlobalSettings.ProfileUrl));
            section.Diagnostics.Add(new Diagnostic("Update Check Period", umbraco.GlobalSettings.VersionCheckPeriod));
            section.Diagnostics.Add(new Diagnostic("Path to Root", umbraco.GlobalSettings.FullpathToRoot));
            section.AddDiagnostics(ConfigurationManager.AppSettings, false, key => key.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase));
            sections.Add(section);

            section = new DiagnosticSection("Content Settings");
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
                section.Diagnostics.Add(new Diagnostic(server.ServerName, String.Format("AppID: {0}, Address: {1}", server.AppId, server.ServerAddress)));
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

            UmbracoDataService dataService = new UmbracoDataService(umbHelper);
            var servers = dataService.GetRegistredServers();

            if (servers != null && servers.Any())
            {
                section = new DiagnosticSection("Server Registration");

                foreach (var server in servers)
                {
                    section.Diagnostics.Add(new Diagnostic(server.ComputerName, server.ToDiagnostic()));
                }

                sections.Add(section);
            }

            var migrations = dataService.GetMigrations();

            if (migrations != null && migrations.Any()) 
            {
                section = new DiagnosticSection("Migration History");

                foreach (var migration in migrations)
                {
                    section.Diagnostics.Add(new Diagnostic(migration.Name, migration.ToDiagnostic()));
                }

                sections.Add(section);
            }

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

            section = new DiagnosticSection("Registered Content Finders");
            foreach (var item in ContentFinderResolver.Current.Finders)
            {
                var t = item.GetType();
                section.Diagnostics.Add(new Diagnostic(t.Name, t.GetFullNameWithAssembly()));
            }
            sections.Add(section);

            section = new DiagnosticSection("Registered URL Resolvers");
            foreach (var item in UrlProviderResolver.Current.Providers)
            {
                var t = item.GetType();
                section.Diagnostics.Add(new Diagnostic(t.Name, t.GetFullNameWithAssembly()));
            }
            sections.Add(section);

            var br = UmbracoConfig.For.BaseRestExtensions();
            section = new DiagnosticSection("Base Rest Extensions");
            section.Diagnostics.Add(new Diagnostic("Enabled?", br.Enabled));
            foreach (var item in br.Items)
            {
                section.Diagnostics.Add(new Diagnostic(item.Alias, item.Type));
            }
            sections.Add(section);

            try
            {
                var mvc = Assembly.Load(new AssemblyName("Examine"));
                var name = mvc.GetName();

                section = new DiagnosticSection("Examine Settings");
                section.Diagnostics.Add(new Diagnostic("Assembly Version", name.Version));
                section.Diagnostics.Add(new Diagnostic("Full Name", mvc.FullName));

                if (Examine.ExamineManager.Instance != null)
                {
                    foreach (var provider in Examine.ExamineManager.Instance.IndexProviderCollection)
                    {
                        var p = provider as ProviderBase;
                        if (p != null)
                            section.Diagnostics.Add(new Diagnostic(p.Name, p.GetType().FullName));
                        var l = provider as LuceneIndexer;
                        if (l != null)
                            section.Diagnostics.Add(new Diagnostic(l.IndexSetName, l.LuceneIndexFolder));
                    }

                    foreach (ProviderBase p in Examine.ExamineManager.Instance.SearchProviderCollection)
                    {
                        section.Diagnostics.Add(new Diagnostic(p.Name, p.GetType().FullName));
                    }

                    section.Diagnostics.Add(new Diagnostic("Default Searcher", Examine.ExamineManager.Instance.DefaultSearchProvider.Name));
                }

                var types = ReflectionHelper.GetTypesAssignableFrom(typeof(UmbracoExamine.LocalStorage.ILocalStorageDirectory));
                section.Diagnostics.Add(new Diagnostic("Storage Handlers", String.Join(", ", types.Select(t => t.Name))));

                sections.Add(section);
            }
            catch
            {
                // deliberate
            }

            var domains = umbContext.Application.Services.DomainService.GetAll(true);

            if (domains != null && domains.Any())
            {
                section = new DiagnosticSection("Registered Domains");
                foreach (var d in domains)
                {
                    section.Diagnostics.Add(new Diagnostic(d.LanguageIsoCode, d.DomainName + (d.IsWildcard ? " (Wildcard)" : String.Empty)));
                }
                sections.Add(section);
            }

            section = new DiagnosticSection("Supported Cultures");

            var cultures = umbContext.Application.Services.TextService.GetSupportedCultures();

            foreach (var c in cultures)
            {
                section.Diagnostics.Add(new Diagnostic(c.ThreeLetterISOLanguageName + " (" + c.TwoLetterISOLanguageName + ")", c.DisplayName));
            }

            sections.Add(section);

            group.Add(sections);
            groups.Add(group);

            group = new DiagnosticGroup(id++, "Server Configuration");
            sections = new List<DiagnosticSection>();

            section = new DiagnosticSection("Server Settings");
            section.Diagnostics.Add(new Diagnostic("Machine Name", Environment.MachineName));
            section.Diagnostics.Add(new Diagnostic("OS Version", Environment.OSVersion));
            section.Diagnostics.Add(new Diagnostic("64 Bit OS?", Environment.Is64BitOperatingSystem));
            section.Diagnostics.Add(new Diagnostic("Processor Count", Environment.ProcessorCount));
            section.Diagnostics.Add(new Diagnostic("Network Domain", Environment.UserDomainName));
            section.Diagnostics.Add(new Diagnostic("ASP.NET Version", Environment.Version));

            if (httpContext != null && httpContext.Request != null)
            {
                var request = umbContext.HttpContext.Request;

                if (request != null)
                {
                    section.Diagnostics.Add(new Diagnostic("Web Server", request["SERVER_SOFTWARE"]));
                    section.Diagnostics.Add(new Diagnostic("Host", request["HTTP_HOST"]));
                    section.Diagnostics.Add(new Diagnostic("App Pool ID", request["APP_POOL_ID"]));
                }
            }

            section.Diagnostics.Add(new Diagnostic("Current Directory", Environment.CurrentDirectory));
            section.Diagnostics.Add(new Diagnostic("64 Bit Process?", Environment.Is64BitProcess));
            section.Diagnostics.Add(new Diagnostic("Framework Bits", IntPtr.Size * 8));
            section.Diagnostics.Add(new Diagnostic("Process Physical Memory", String.Format("{0:n} MB", Environment.WorkingSet / 1048576)));

            try
            {
                object currentProcess = typeof(Process).GetMethod("GetCurrentProcess", Type.EmptyTypes).Invoke(null, null);
                if (currentProcess != null)
                {
                    object processModule = typeof(Process).GetProperty("MainModule").GetValue(currentProcess, null);
                    section.Diagnostics.Add(new Diagnostic("Current Process", (string)typeof(ProcessModule).GetProperty("ModuleName").GetValue(processModule, null)));
                }
            }
            catch
            {
                // deliberate
            }

            section.Diagnostics.Add(new Diagnostic("Current Culture", System.Threading.Thread.CurrentThread.CurrentCulture));
            section.Diagnostics.Add(new Diagnostic("Current Thread State", System.Threading.Thread.CurrentThread.ThreadState));

            sections.Add(section);

            section = new DiagnosticSection("Application Settings");

            section.Diagnostics.Add(new Diagnostic("Application ID", HostingEnvironment.ApplicationID));
            section.Diagnostics.Add(new Diagnostic("Site Name", HostingEnvironment.SiteName));
            section.Diagnostics.Add(new Diagnostic("Development Env?", HostingEnvironment.IsDevelopmentEnvironment));
            section.Diagnostics.Add(new Diagnostic("On UNC Share?", HttpRuntime.IsOnUNCShare));
            section.Diagnostics.Add(new Diagnostic("Bin Directory", HttpRuntime.BinDirectory));
            section.Diagnostics.Add(new Diagnostic("Code Gen Dir", HttpRuntime.CodegenDir));
            section.Diagnostics.Add(new Diagnostic("Target Framework", HttpRuntime.TargetFramework));
            section.Diagnostics.Add(new Diagnostic("App Domain ID", HttpRuntime.AppDomainId));
            section.Diagnostics.Add(new Diagnostic("App Domain Path", HttpRuntime.AppDomainAppPath));

            if (HostingEnvironment.Cache != null)
            {
                section.Diagnostics.Add(new Diagnostic("Cached Items", HostingEnvironment.Cache.Count.ToString()));
                section.Diagnostics.Add(new Diagnostic("Cache Memory Limit ", HostingEnvironment.Cache.EffectivePercentagePhysicalMemoryLimit + "%"));
            }


            sections.Add(section);

            if (httpContext != null && httpContext.Request != null)
            {
                section = new DiagnosticSection("Server Variables");
                section.AddDiagnostics(httpContext.Request.ServerVariables, true, key => !ServerVarsToSkip.Contains(key));
            }
            sections.Add(section);

            section = new DiagnosticSection("Site Diagnostics");

            sections.Add(section);

            section = new DiagnosticSection("Database Settings");
            var dbc = umbContext.Application.DatabaseContext;
            section.Diagnostics.Add(new Diagnostic("Database Provider", dbc.DatabaseProvider));
            section.Diagnostics.Add(new Diagnostic("Connection String", Regex.Replace(dbc.ConnectionString, @"password(\W*)=(.+)(;|$)", "*******", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase)));
            sections.Add(section);

            section = new DiagnosticSection("None Umbraco App Settings");
            section.AddDiagnostics(ConfigurationManager.AppSettings, false, key => !key.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase));
            sections.Add(section);

            group.Add(sections);
            groups.Add(group);

            group = new DiagnosticGroup(id++, "MVC Configuration");
            sections = new List<DiagnosticSection>();

            try
            {
                var mvc = Assembly.Load(new AssemblyName("System.Web.Mvc"));
                var name = mvc.GetName();

                section = new DiagnosticSection("MVC Version");
                section.Diagnostics.Add(new Diagnostic("MVC Version", name.Version));
                section.Diagnostics.Add(new Diagnostic("Full Name", mvc.FullName));
                section.Diagnostics.Add(new Diagnostic("Architecture", name.ProcessorArchitecture));

                sections.Add(section);
            }
            catch
            {
                // deliberate
            }

            section = new DiagnosticSection("MVC Routes");
            section.Diagnostics.AddRange(RouteTable.Routes.Select(r => (Route)r).Select(r => new Diagnostic(r.RouteHandler.GetType().Name, r.Url)));
            sections.Add(section);

            section = new DiagnosticSection("MVC Action Filters");
            section.AddDiagnosticsFrom(typeof(System.Web.Mvc.IActionFilter));
            sections.Add(section);

            section = new DiagnosticSection("MVC Authorization Filters");
            section.AddDiagnosticsFrom(typeof(System.Web.Mvc.IAuthorizationFilter));
            sections.Add(section);

            section = new DiagnosticSection("MVC Model Binders");
            section.AddDiagnosticsFrom(typeof(System.Web.Mvc.IModelBinder));
            sections.Add(section);

            section = new DiagnosticSection("MVC Controller Factories");
            section.AddDiagnosticsFrom(typeof(System.Web.Mvc.IControllerFactory));
            sections.Add(section);

            section = new DiagnosticSection("MVC Controllers");
            section.AddDiagnosticsFrom(typeof(IController));
            sections.Add(section);

            group.Add(sections);
            groups.Add(group);

            return groups;
        }
    }
}