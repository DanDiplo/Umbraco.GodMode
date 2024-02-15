using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Smidge;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Features;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Class for retieving diagnostic and setting information
    /// </summary>
    public class DiagnosticService : IDiagnosticService
    {
        private readonly IRuntimeState runtimeState;
        private readonly IUmbracoVersion version;
        private readonly IServiceProvider factory;
        private readonly IOptions<NuCacheSettings> nuCacheSettings;
        private readonly IOptions<IndexCreatorSettings> indexSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUmbracoDatabaseFactory databaseFactory;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IUmbracoDatabaseService databaseService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment;
        private readonly ISmidgeConfig smidgeConfig;
        private readonly UmbracoFeatures features;
        private readonly IConfiguration configuration;
        private readonly IServer webServer;
        private readonly IOptions<GodModeConfig> godModeConfig;
        private HttpContext httpContext;

        public DiagnosticService(IRuntimeState runtimeState, IUmbracoVersion umbracoVersion, IUmbracoDatabaseService databaseService, 
            IServiceProvider factory, IOptions<NuCacheSettings> nuCacheSettings, IOptions<IndexCreatorSettings> indexSettings, 
            IHttpContextAccessor httpContextAccessor, IUmbracoDatabaseFactory databaseFactory, IHostingEnvironment hostingEnvironment, 
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment, Smidge.ISmidgeConfig smidgeConfig, 
            UmbracoFeatures features, IConfiguration configuration, IServer webServer, IOptions<GodModeConfig> godModeConfig)
        {
            this.runtimeState = runtimeState;
            this.version = umbracoVersion;
            this.factory = factory;
            this.nuCacheSettings = nuCacheSettings;
            this.indexSettings = indexSettings;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseFactory = databaseFactory;
            this.hostingEnvironment = hostingEnvironment;
            this.databaseService = databaseService;
            this.webHostEnvironment = webHostEnvironment;
            this.smidgeConfig = smidgeConfig;
            this.features = features;
            this.configuration = configuration;
            this.webServer = webServer;
            this.godModeConfig = godModeConfig;
        }

        public IEnumerable<DiagnosticGroup> GetDiagnosticGroups()
        {
            var groups = new List<DiagnosticGroup>();

            int id = 0;

            /* Umbraco Config */

            var group = new DiagnosticGroup(id++, "Umbraco Configuration");
            var sections = new List<DiagnosticSection>();

            var section = new DiagnosticSection("Umbraco Version");

            section.Diagnostics.Add(new Diagnostic("Version", version.Version));
            section.Diagnostics.Add(new Diagnostic("Semantic Version", version.SemanticVersion));
            section.Diagnostics.Add(new Diagnostic("Assembly Version", version.AssemblyVersion));
            section.Diagnostics.Add(new Diagnostic("Assembly File Version", version.AssemblyFileVersion));

            if (!string.IsNullOrEmpty(version.Comment))
            {
                section.Diagnostics.Add(new Diagnostic("Version Comment", version.Comment));
            }

            section.Diagnostics.Add(new Diagnostic("Runtime Level", runtimeState.Level));
            section.Diagnostics.Add(new Diagnostic("Current Migration State", runtimeState.CurrentMigrationState));
            section.Diagnostics.Add(new Diagnostic("Final Migration State", runtimeState.FinalMigrationState));

            sections.Add(section);

            var globalSettings = factory.GetRequiredService<IOptions<GlobalSettings>>();

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Global Settings", globalSettings.Value, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Database Settings", databaseFactory, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("SMTP Settings", globalSettings.Value.Smtp, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<HostingSettings>("Hosting Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Environment Settings", hostingEnvironment));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<SecuritySettings>("Security Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<RequestHandlerSettings>("Request Handler Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<WebRoutingSettings>("Web Routing Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ModelsBuilderSettings>("ModelsBuilder Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentNotificationSettings>("Notification Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Index Creator Settings", indexSettings.Value, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<RuntimeMinificationSettings>("Minification Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ImagingCacheSettings>("Imaging Cache Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ImagingResizeSettings>("Imaging Resize Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentImagingSettings>("Content Imaging Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<LoggingSettings>("Logging Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("NuCache Settings", nuCacheSettings.Value, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<UserPasswordConfigurationSettings>("User Password Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentVersionCleanupPolicySettings>("Content Version Cleanup Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<CoreDebugSettings>("Core Debug Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<DataTypesSettings>("Data Type Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<HelpPageSettings>("Help Page Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<InstallDefaultDataSettings>("Install Default Data Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<MemberPasswordConfigurationSettings>("Member Password Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<UnattendedSettings>("Unattended Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<RichTextEditorSettings>("RichText Editor Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentSettings>("Content Settings", factory));

            var healthCheckSettings = factory.GetRequiredService<IOptions<HealthChecksSettings>>();
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Health Check Settings", healthCheckSettings.Value.Notification, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentDashboardSettings>("Content Dashboard Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentNotificationSettings>("Content Notification Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ExceptionFilterSettings>("Exception Filter Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<KeepAliveSettings>("Keep Alive Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<Smidge.Options.CacheControlOptions>("Smidge Cache Control", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<BasicAuthSettings>("Basic Auth Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionPropertiesFrom("Disabled Features", features.Disabled, new string[] { "Controllers" }));

            group.Add(sections);
            groups.Add(group);

            /* Server Config */

            group = new DiagnosticGroup(id++, "Server Configuration");
            sections = new List<DiagnosticSection>();

            section = new DiagnosticSection("Server Settings");
            section.Diagnostics.Add(new Diagnostic("Machine Name", Environment.MachineName));
            section.Diagnostics.Add(new Diagnostic("OS Version", Environment.OSVersion));
            section.Diagnostics.Add(new Diagnostic("64 Bit OS?", Environment.Is64BitOperatingSystem));
            section.Diagnostics.Add(new Diagnostic("Processor Count", Environment.ProcessorCount));
            section.Diagnostics.Add(new Diagnostic("Network Domain", Environment.UserDomainName));
            section.Diagnostics.Add(new Diagnostic("ASP.NET Version", Environment.Version));

            section.Diagnostics.Add(new Diagnostic("Current Directory", Environment.CurrentDirectory));
            section.Diagnostics.Add(new Diagnostic("64 Bit Process?", Environment.Is64BitProcess));
            section.Diagnostics.Add(new Diagnostic("Framework Bits", IntPtr.Size * 8));
            section.Diagnostics.Add(new Diagnostic("Process Physical Memory", string.Format("{0:n} MB", Environment.WorkingSet / 1048576)));
            section.Diagnostics.Add(new Diagnostic("System Up Time", TimeSpan.FromTicks(Environment.TickCount)));
            section.Diagnostics.Add(new Diagnostic("CLI", Environment.CommandLine));
            section.Diagnostics.Add(new Diagnostic("System Directory", Environment.SystemDirectory));

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

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Web Host Environment", webHostEnvironment));

            sections.Add(new DiagnosticSection($"Web Server ({webServer.GetType().FullName}) Features", webServer.Features.Select(x => new Diagnostic(x.Key.ToString(), x.Value.ToString()))));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<CookieOptions>("Cookie Options", factory));

            section = new DiagnosticSection("Environment Variables");
            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                section.Diagnostics.Add(new Diagnostic(kv.Key.ToString(), kv.Value));
            }

            sections.Add(section);
            group.Add(sections);
            groups.Add(group);

            // Environment

            group = new DiagnosticGroup(id++, "Environment Config");
            sections = EnvironmentConfigHelper.GetEnvironmentDiagnostics(configuration);
            group.Add(sections);
            groups.Add(group);


            // HTTP Context

            var context = this.httpContext ?? httpContextAccessor.HttpContext;

            if (context != null)
            {
                group = new DiagnosticGroup(id++, "HTTP Context");
                sections = new List<DiagnosticSection>
                {
                    DiagnosticSection.AddDiagnosticSectionFrom("HTTP Connection", context.Connection),
                    DiagnosticSection.AddDiagnosticSectionFrom("HTTP Features", context.Features)
                };

                if (context.Items != null)
                {
                    section = new DiagnosticSection("HTTP Context Items");

                    foreach (var item in context.Items)
                    {
                        section.Diagnostics.Add(new Diagnostic(item.Key.ToString(), item.Value));
                    }

                    sections.Add(section);
                }

                sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Host", context.Request.Host));

                group.Add(sections);
                groups.Add(group);
            }

            // Constants

            group = new DiagnosticGroup(id++, "Umbraco Constants");
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
                DiagnosticSection.AddDiagnosticSectionFromConstant("Web", typeof(Constants.Web)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("System Directories", typeof(Constants.SystemDirectories)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Authorization Policies", typeof(AuthorizationPolicies)),
                DiagnosticSection.AddDiagnosticSectionFromConstant("Cache Keys", typeof(CacheKeys))
            };

            group.Add(sections);
            groups.Add(group);

            // Database

            group = new DiagnosticGroup(id++, "Database Values");
            sections = new List<DiagnosticSection>();

            try
            {
                IEnumerable<UmbracoKeyValue> migrations = this.databaseService.GetKeyValues();

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

            try
            {
                var servers = this.databaseService.GetRegistredServers();

                if (servers != null && servers.Any())
                {
                    section = new DiagnosticSection("Registered Servers");

                    foreach (var server in servers)
                    {
                        section.Diagnostics.Add(new Diagnostic($"{server.Id}: {server.ComputerName}", server.ToDiagnostic()));
                    }

                    sections.Add(section);
                }
            }
            catch
            {
                // deliberate
            }

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Database Messenger Settings", globalSettings.Value.DatabaseServerMessenger, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Database Registrar Settings", globalSettings.Value.DatabaseServerRegistrar, false));

            sections.Add(new DiagnosticSection("Distributed Services", new List<Diagnostic>()
            {
                new Diagnostic("Disable Election For Single Server?", globalSettings.Value.DisableElectionForSingleServer),
                new Diagnostic("Distribted Locking Mechanism", globalSettings.Value.DistributedLockingMechanism),
                new Diagnostic("Distribted Read Lock Timeout", globalSettings.Value.DistributedLockingReadLockDefaultTimeout),
                new Diagnostic("Distribted Write Lock Timeout", globalSettings.Value.DistributedLockingWriteLockDefaultTimeout)
            }));

            group.Add(sections);
            groups.Add(group);


            // Infrastructure

            group = new DiagnosticGroup(id++, "Umbraco Infrastructure");
            sections = new List<DiagnosticSection>();

            section = new DiagnosticSection("Background Tasks");
            section.AddDiagnosticsFrom(typeof(Umbraco.Cms.Infrastructure.HostedServices.RecurringHostedServiceBase));
            sections.Add(section);

            section = new DiagnosticSection("Sections");
            section.AddDiagnosticsFrom(typeof(Umbraco.Cms.Core.Sections.ISection));
            sections.Add(section);

            section = new DiagnosticSection("Dashboards");
            section.AddDiagnosticsFrom(typeof(Umbraco.Cms.Core.Dashboards.IDashboard));
            sections.Add(section);

            section = new DiagnosticSection("Middleware");
            section.AddDiagnosticsFrom(typeof(IMiddleware));
            sections.Add(section);

            section = new DiagnosticSection("Notifications");
            section.AddDiagnosticsFrom(typeof(Umbraco.Cms.Core.Notifications.INotification));
            sections.Add(section);

            section = new DiagnosticSection("Telemetry");
            section.AddDiagnosticsFrom(typeof(Umbraco.Cms.Core.Telemetry.ITelemetryService));
            sections.Add(section);

            group.Add(sections);
            groups.Add(group);

            // MVC Config

            group = new DiagnosticGroup(id++, "MVC Configuration");
            sections = new List<DiagnosticSection>();

            var mvcAssembly = typeof(Controller).Assembly;

            if (mvcAssembly != null)
            {
                section = new DiagnosticSection("MVC Version");
                section.Diagnostics.Add(new Diagnostic("Assembly", mvcAssembly.GetName().Version));
                section.Diagnostics.Add(new Diagnostic("Version", mvcAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion));
                section.Diagnostics.Add(new Diagnostic("File Version", mvcAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version));
                section.Diagnostics.Add(new Diagnostic("Full Name", mvcAssembly.FullName));
                section.Diagnostics.Add(new Diagnostic("Location", mvcAssembly.Location));
                section.Diagnostics.Add(new Diagnostic("Compatibility", mvcAssembly.GetName().VersionCompatibility));
                sections.Add(section);
            }

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<MvcOptions>("MVC Options", factory));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions>("HTML Helper Options", factory));
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<CookieTempDataProviderOptions>("Cookie TempData Options", factory));

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
            section.AddDiagnosticsFrom(typeof(Microsoft.AspNetCore.Mvc.ControllerBase));
            sections.Add(section);

            group.Add(sections);
            groups.Add(group);

            /* Redact & Remove */

            RedactGroups(groups);

            /* Return! */

            return groups;
        }

        public void SetContext(HttpContext httpContext) => this.httpContext = httpContext;

        private void RedactGroups(List<DiagnosticGroup> groups)
        {
            groups.RemoveAll(g => godModeConfig.Value.Diagnostics.GroupsToHide.InvariantContains(g.Title));

            foreach (var g in groups)
            {
                g.Sections.RemoveAll(s => godModeConfig.Value.Diagnostics.SectionsToHide.InvariantContains(s.Heading));

                foreach (var s in g.Sections)
                {
                    if (g.Title == "Environment Config")
                    {
                        foreach (var d in s.Diagnostics)
                        {
                            if (godModeConfig.Value.Diagnostics.KeysToRedact.InvariantContains(d.Key))
                            {
                                d.Value = StringHelper.RedactString(d.Value);
                            }
                        }
                    }
                    else
                    {
                        foreach (var d in s.Diagnostics)
                        {
                            var keyName = s.Heading + ":" + d.Key;

                            if (godModeConfig.Value.Diagnostics.KeysToRedact.InvariantContains(keyName))
                            {
                                d.Value = StringHelper.RedactString(d.Value);
                            }
                        }
                    }
                }
            }
        }
    }
}