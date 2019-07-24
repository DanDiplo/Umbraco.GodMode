using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.Grid;
using Umbraco.Core.Configuration.HealthChecks;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.IO;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Class for retieving diagnostic and setting information
    /// </summary>
    public class DiagnosticService : IDiagnosticService
    {
        private readonly IUmbracoDatabaseService databaseService;
        private readonly HttpContextBase httpContext;
        private readonly IMediaFileSystem mediaFileSystem;
        private readonly IRuntimeState runtimeState;

        private static readonly string[] ServerVarsToSkip = new string[] { "ALL_HTTP", "ALL_RAW", "HTTP_COOKIE", "HTTP_X_XSRF_TOKEN" };

        public DiagnosticService(IUmbracoDatabaseService databaseService, IMediaFileSystem mediaFileSystem, IRuntimeState runtimeState, HttpContextBase httpContext = null)
        {
            this.databaseService = databaseService ?? throw new ArgumentNullException("The database service that was passed to the Diagnostic Service was null");
            this.httpContext = httpContext;
            this.mediaFileSystem = mediaFileSystem;
            this.runtimeState = runtimeState;
        }

        public IEnumerable<DiagnosticGroup> GetDiagnosticGroups()
        {
            var groups = new List<DiagnosticGroup>();
            int id = 0;

            var group = new DiagnosticGroup(id++, "Umbraco Configuration");
            var sections = new List<DiagnosticSection>();

            var section = new DiagnosticSection("Umbraco Version");
            section.Diagnostics.Add(new Diagnostic("Version", runtimeState.Version));
            section.Diagnostics.Add(new Diagnostic("Semantic Version", runtimeState.SemanticVersion.ToSemanticString()));
            section.Diagnostics.Add(new Diagnostic("Application URL", runtimeState.ApplicationUrl.ToString()));
            section.Diagnostics.Add(new Diagnostic("Application Virtual Path", runtimeState.ApplicationVirtualPath));
            section.Diagnostics.Add(new Diagnostic("Debug?", runtimeState.Debug));
            section.Diagnostics.Add(new Diagnostic("Is Main Dom?", runtimeState.IsMainDom));
            section.Diagnostics.Add(new Diagnostic("Runtime Level", runtimeState.Level));
            section.Diagnostics.Add(new Diagnostic("Runtime Reason", runtimeState.Reason));
            section.Diagnostics.Add(new Diagnostic("Server Role", runtimeState.ServerRole));
            section.Diagnostics.Add(new Diagnostic("Current Migration State", runtimeState.CurrentMigrationState));
            section.Diagnostics.Add(new Diagnostic("Final Migration State", runtimeState.FinalMigrationState));

            sections.Add(section);

            section = new DiagnosticSection("Umbraco Settings");
            section.Diagnostics.Add(new Diagnostic("Debug Mode?", GlobalSettings.DebugMode));
            section.AddDiagnostics(ConfigurationManager.AppSettings, false, key => key.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase));
            sections.Add(section);

            var configs = new Umbraco.Core.Configuration.Configs();
            configs.AddCoreConfigs();

            IUmbracoSettingsSection settings = configs.GetConfig<IUmbracoSettingsSection>();

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Content Setting", settings.Content, true));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Request Handler Settings", settings.RequestHandler, true));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Tour Settings", settings.BackOffice.Tours, true));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Web Routing Settings", settings.WebRouting, true));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Security Settings", settings.Security, true));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Logging Settings", settings.Logging, true));

            IHealthChecks health = configs.GetConfig<IHealthChecks>();
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Health Checks", health.NotificationSettings, true));

            IGridConfig grid = configs.GetConfig<IGridConfig>();
            section = new DiagnosticSection("Grid Settings", grid.EditorsConfig.Editors.Select(e => new Diagnostic(e.Name, e.View)));
            sections.Add(section);

            section = new DiagnosticSection("Media File System");
            section.Diagnostics.Add(new Diagnostic("Type", mediaFileSystem.GetType()));
            section.Diagnostics.Add(new Diagnostic("Can Add Physical?", mediaFileSystem.CanAddPhysical));
            section.Diagnostics.Add(new Diagnostic("Full Root Path", mediaFileSystem.GetFullPath("~/")));
            sections.Add(section);

            group.Add(sections);

            groups.Add(group);

            group = new DiagnosticGroup(id++, "Database Values");
            sections = new List<DiagnosticSection>();

            try
            {
                var conn = ConfigurationManager.ConnectionStrings[Constants.System.UmbracoConnectionName];

                if (conn != null)
                {
                    section = new DiagnosticSection("Database Settings");
                    section.Diagnostics.Add(new Diagnostic("Provider", conn.ProviderName));
                    section.Diagnostics.Add(new Diagnostic("Name", conn.Name));
                    section.Diagnostics.Add(new Diagnostic("Connection String", Regex.Replace(conn.ConnectionString, @"password(\W*)=(.+)(;|$)", "*******", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase)));
                    sections.Add(section);
                }
            }
            catch
            {
                // deliberate
            }

            try
            {
                IEnumerable<ServerModel> servers = databaseService.GetRegistredServers();

                if (servers != null && servers.Any())
                {
                    section = new DiagnosticSection("Server Registration");

                    foreach (ServerModel server in servers)
                    {
                        section.Diagnostics.Add(new Diagnostic(server.ComputerName, server.ToDiagnostic()));
                    }

                    sections.Add(section);
                }
            }
            catch
            {
                // deliberate 
            }

            try
            {
                IEnumerable<UmbracoKeyValue> migrations = databaseService.GetKeyValues();

                if (migrations != null && migrations.Any())
                {
                    section = new DiagnosticSection("Key Value Table");

                    foreach (UmbracoKeyValue migration in migrations)
                    {
                        section.Diagnostics.Add(new Diagnostic(migration.Value, migration.ToDiagnostic()));
                    }

                    sections.Add(section);
                }
            }
            catch
            {
                // deliberate
            }

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
                HttpRequestBase request = httpContext.Request;

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
            section.Diagnostics.Add(new Diagnostic("Process Physical Memory", string.Format("{0:n} MB", Environment.WorkingSet / 1048576)));
            section.Diagnostics.Add(new Diagnostic("System Up Time", TimeSpan.FromTicks(Environment.TickCount)));

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

            section.Diagnostics.Add(new Diagnostic("Max Requests per CPU", HostingEnvironment.MaxConcurrentRequestsPerCPU));
            section.Diagnostics.Add(new Diagnostic("Max Threads per CPU", HostingEnvironment.MaxConcurrentThreadsPerCPU));
            section.Diagnostics.Add(new Diagnostic("Current Server Time", DateTime.Now));

            sections.Add(section);

            if (httpContext != null && httpContext.Request != null)
            {
                section = new DiagnosticSection("Server Variables");
                section.AddDiagnostics(httpContext.Request.ServerVariables, true, key => !ServerVarsToSkip.Contains(key));
            }

            sections.Add(section);

            section = new DiagnosticSection("None Umbraco App Settings");
            section.AddDiagnostics(ConfigurationManager.AppSettings, false, key => !key.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase));
            sections.Add(section);

            try
            {
                section = new DiagnosticSection("Mail Settings");
                var smtp = new System.Net.Mail.SmtpClient();
                section.Diagnostics.Add(new Diagnostic("SMTP Host", smtp.Host));
                section.Diagnostics.Add(new Diagnostic("SMTP Delivery Method", smtp.DeliveryMethod));
                section.Diagnostics.Add(new Diagnostic("SMTP Port", smtp.Port));
                section.Diagnostics.Add(new Diagnostic("SMTP Service Point", smtp.ServicePoint.ConnectionName));
                section.Diagnostics.Add(new Diagnostic("SMTP Delivery Format", smtp.DeliveryFormat));
                sections.Add(section);
            }
            catch
            {
                // deliberate
            }

            group.Add(sections);
            groups.Add(group);

            group = new DiagnosticGroup(id++, "Umrbaco Constants");
            sections = new List<DiagnosticSection>
            {
                DiagnosticSection.AddDiagnosticSectionFromConstant("Applications", typeof(Constants.Applications)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Composing", typeof(Constants.Composing)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Conventions", typeof(Constants.Conventions)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Icons", typeof(Constants.Icons)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Object Types", typeof(Constants.ObjectTypes)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Property Editors", typeof(Constants.PropertyEditors)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Property Type Groups", typeof(Constants.PropertyTypeGroups)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Security", typeof(Constants.Security)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("System", typeof(Constants.System)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Trees", typeof(Constants.Trees)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("UDI Entity Type", typeof(Constants.UdiEntityType)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Umbraco Indexes", typeof(Constants.UmbracoIndexes)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Web", typeof(Constants.Web))
            };

            group.Add(sections);
            groups.Add(group);

            group = new DiagnosticGroup(id++, "MVC Configuration");
            sections = new List<DiagnosticSection>();

            try
            {
                var mvc = Assembly.Load(new AssemblyName("System.Web.Mvc"));
                AssemblyName name = mvc.GetName();

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
            section.AddDiagnosticsFrom(typeof(IActionFilter));
            sections.Add(section);

            section = new DiagnosticSection("MVC Authorization Filters");
            section.AddDiagnosticsFrom(typeof(IAuthorizationFilter));
            sections.Add(section);

            section = new DiagnosticSection("MVC Model Binders");
            section.AddDiagnosticsFrom(typeof(IModelBinder));
            sections.Add(section);

            section = new DiagnosticSection("MVC Controller Factories");
            section.AddDiagnosticsFrom(typeof(IControllerFactory));
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