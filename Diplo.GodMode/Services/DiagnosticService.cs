using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;

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
        private readonly IOptions<UserPasswordConfigurationSettings> passwordConfiguration;
        private readonly IOptions<NuCacheSettings> nuCacheSettings;
        private readonly IOptions<IndexCreatorSettings> indexSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUmbracoDatabaseFactory databaseFactory;
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IUmbracoDatabaseService databaseService;

        private HttpContext httpContext;

        public DiagnosticService(IRuntimeState runtimeState, IUmbracoVersion umbracoVersion, IUmbracoDatabaseService databaseService, IServiceProvider factory, IOptions<UserPasswordConfigurationSettings> passwordConfiguration, IOptions<NuCacheSettings> nuCacheSettings, IOptions<IndexCreatorSettings> indexSettings, IHttpContextAccessor httpContextAccessor, IUmbracoDatabaseFactory databaseFactory, IHostingEnvironment hostingEnvironment)
        {
            this.runtimeState = runtimeState;
            this.version = umbracoVersion;
            this.factory = factory;
            this.passwordConfiguration = passwordConfiguration;
            this.nuCacheSettings = nuCacheSettings;
            this.indexSettings = indexSettings;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseFactory = databaseFactory;
            this.hostingEnvironment = hostingEnvironment;
            this.databaseService = databaseService;
        }

        public IEnumerable<DiagnosticGroup> GetDiagnosticGroups()
        {
            var groups = new List<DiagnosticGroup>();

            int id = 0;

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

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("User Password Settings", passwordConfiguration.Value, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<MemberPasswordConfigurationSettings>("Member Password Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<UnattendedSettings>("Unattended Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<TypeFinderSettings>("Type Finder Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<RichTextEditorSettings>("RichText Editor Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentSettings>("Content Settings", factory));

            var healthCheckSettings = factory.GetRequiredService<IOptions<HealthChecksSettings>>();
            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("Health Check Settings", healthCheckSettings.Value.Notification, false));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentDashboardSettings>("Content Dashboard Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ActiveDirectorySettings>("Active Directory Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ContentNotificationSettings>("Content Notification Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<ExceptionFilterSettings>("Exception Filter Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<KeepAliveSettings>("Keep Alive Settings", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<BasicAuthSettings>("Basic Auth Settings", factory));

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

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<CookieOptions>("Cookie Options", factory));

            section = new DiagnosticSection("Environment Variables");
            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                section.Diagnostics.Add(new Diagnostic(kv.Key.ToString(), kv.Value));
            }

            sections.Add(section);
            group.Add(sections);
            groups.Add(group);

            var context = this.httpContext ?? httpContextAccessor.HttpContext;

            if (context != null)
            {
                group = new DiagnosticGroup(id++, "HTTP Context");
                sections = new List<DiagnosticSection>();

                sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("HTTP Connection", context.Connection));
                sections.Add(DiagnosticSection.AddDiagnosticSectionFrom("HTTP Features", context.Features));

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
                DiagnosticSection.AddDiagnosticSectionFromConstant("Authorization Policies", typeof(AuthorizationPolicies))
            };

            group.Add(sections);
            groups.Add(group);

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

            group.Add(sections);
            groups.Add(group);

            group = new DiagnosticGroup(id++, "MVC Configuration");
            sections = new List<DiagnosticSection>();

            var mvcAssembly = typeof(Controller).Assembly;

            if (mvcAssembly != null)
            {
                section = new DiagnosticSection("MVC Version");
                section.Diagnostics.Add(new Diagnostic("Full Name", mvcAssembly.FullName));
                section.Diagnostics.Add(new Diagnostic("Location", mvcAssembly.Location));
                section.Diagnostics.Add(new Diagnostic("Version", mvcAssembly.GetName().Version));
                section.Diagnostics.Add(new Diagnostic("Compatibility", mvcAssembly.GetName().VersionCompatibility));
                sections.Add(section);
            }

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<MvcOptions>("MVC Options", factory));

            sections.Add(DiagnosticSection.AddDiagnosticSectionFrom<MvcOptions>("MVC View Options", factory));

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

            return groups;
        }

        public void SetContext(HttpContext httpContext) => this.httpContext = httpContext;
    }
}