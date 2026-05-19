using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Features;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Class for retrieving diagnostic and setting information.
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
        private readonly UmbracoFeatures features;
        private readonly IConfiguration configuration;
        private readonly IServer webServer;
        private readonly IOptions<GodModeConfig> godModeConfig;

        private HttpContext httpContext;

        private static readonly string[] ignoreProperties = ["Controllers"];

        public DiagnosticService(
            IRuntimeState runtimeState,
            IUmbracoVersion umbracoVersion,
            IUmbracoDatabaseService databaseService,
            IServiceProvider factory,
            IOptions<NuCacheSettings> nuCacheSettings,
            IOptions<IndexCreatorSettings> indexSettings,
            IHttpContextAccessor httpContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            IHostingEnvironment hostingEnvironment,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment,
            UmbracoFeatures features,
            IConfiguration configuration,
            IServer webServer,
            IOptions<GodModeConfig> godModeConfig)
        {
            this.runtimeState = runtimeState;
            version = umbracoVersion;
            this.factory = factory;
            this.nuCacheSettings = nuCacheSettings;
            this.indexSettings = indexSettings;
            this.httpContextAccessor = httpContextAccessor;
            this.databaseFactory = databaseFactory;
            this.hostingEnvironment = hostingEnvironment;
            this.databaseService = databaseService;
            this.webHostEnvironment = webHostEnvironment;
            this.features = features;
            this.configuration = configuration;
            this.webServer = webServer;
            this.godModeConfig = godModeConfig;
        }

        public IEnumerable<DiagnosticGroup> GetDiagnosticGroups(bool revealRedactedValues = false)
        {
            var groups = new[]
            {
                CreateUmbracoConfigurationGroup(),
                CreateServerConfigurationGroup(),
                CreateEnvironmentConfigurationGroup(),
                CreateRuntimeGroup(),
                CreateHttpContextGroup(),
                CreateDatabaseValuesGroup(),
                CreateApplicationGroup(),
                CreateUmbracoConstantsGroup(),
                CreateMvcConfigurationGroup(),
                CreateInfrastructureGroup(),
                CreateConfigurationSourcesGroup(),
                CreateUmbracoPluginTypesGroup(),
                CreateMappingGroup(),
            }
            .Where(group => group is not null)
            .ToList();

            AssignGroupIds(groups);
            RedactGroups(groups, revealRedactedValues);

            return groups;
        }

        public void SetContext(HttpContext httpContext) => this.httpContext = httpContext;

        private DiagnosticGroup CreateUmbracoConfigurationGroup()
        {
            var globalSettings = factory.GetRequiredService<IOptions<GlobalSettings>>();
            var healthCheckSettings = factory.GetRequiredService<IOptions<HealthChecksSettings>>();

            return new DiagnosticGroup("Umbraco Configuration")
                .Add(CreateUmbracoVersionSection())
                .Add(DiagnosticSection.From("Global Settings", globalSettings.Value, false))
                .Add(DiagnosticSection.From("Database Settings", databaseFactory, false))
                .Add(DiagnosticSection.From("SMTP Settings", globalSettings.Value.Smtp, false))
                .Add(DiagnosticSection.FromOptions<HostingSettings>("Hosting Settings", factory))
                .Add(DiagnosticSection.From("Environment Settings", hostingEnvironment))
                .Add(DiagnosticSection.FromOptions<SecuritySettings>("Security Settings", factory))
                .Add(DiagnosticSection.FromOptions<RequestHandlerSettings>("Request Handler Settings", factory))
                .Add(DiagnosticSection.FromOptions<WebRoutingSettings>("Web Routing Settings", factory))
                .Add(DiagnosticSection.FromOptions<ModelsBuilderSettings>("ModelsBuilder Settings", factory))
                .Add(DiagnosticSection.FromOptions<ContentNotificationSettings>("Notification Settings", factory))
                .Add(DiagnosticSection.From("Index Creator Settings", indexSettings.Value, false))
                .Add(DiagnosticSection.FromOptions<ImagingCacheSettings>("Imaging Cache Settings", factory))
                .Add(DiagnosticSection.FromOptions<ImagingResizeSettings>("Imaging Resize Settings", factory))
                .Add(DiagnosticSection.FromOptions<ContentImagingSettings>("Content Imaging Settings", factory))
                .Add(DiagnosticSection.FromOptions<LoggingSettings>("Logging Settings", factory))
                .Add(DiagnosticSection.From("NuCache Settings", nuCacheSettings.Value, false))
                .Add(DiagnosticSection.FromOptions<UserPasswordConfigurationSettings>("User Password Settings", factory))
                .Add(DiagnosticSection.FromOptions<Umbraco.Cms.Core.Configuration.Models.ContentVersionCleanupPolicySettings>("Content Version Cleanup Settings", factory))
                .Add(DiagnosticSection.FromOptions<CoreDebugSettings>("Core Debug Settings", factory))
                .Add(DiagnosticSection.FromOptions<DataTypesSettings>("Data Type Settings", factory))
                .Add(DiagnosticSection.FromOptions<HelpPageSettings>("Help Page Settings", factory))
                .Add(DiagnosticSection.FromOptions<InstallDefaultDataSettings>("Install Default Data Settings", factory))
                .Add(DiagnosticSection.FromOptions<MemberPasswordConfigurationSettings>("Member Password Settings", factory))
                .Add(DiagnosticSection.FromOptions<UnattendedSettings>("Unattended Settings", factory))
                .Add(DiagnosticSection.FromOptions<ContentSettings>("Content Settings", factory))
                .Add(DiagnosticSection.From("Health Check Settings", healthCheckSettings.Value.Notification, false))
                .Add(DiagnosticSection.FromOptions<ContentNotificationSettings>("Content Notification Settings", factory))
                .Add(DiagnosticSection.FromOptions<ExceptionFilterSettings>("Exception Filter Settings", factory))
                .Add(DiagnosticSection.FromOptions<BasicAuthSettings>("Basic Auth Settings", factory))
                .Add(DiagnosticSection.FromOptions<DeliveryApiSettings>("Delivery API Settings", factory))
                .AddIfNotNull(CreateOptionalOptionsSection(
                    "Dictionary Settings",
                    "Umbraco.Cms.Core.Configuration.Models.DictionarySettings, Umbraco.Cms.Core"))
                .Add(DiagnosticSection.FromOptions<LongRunningOperationsSettings>("Long Running Ops Settings", factory))
                .Add(DiagnosticSection.FromOptions<MediaPropertySettings>("Media Property Settings", factory))
                .Add(DiagnosticSection.FromProperties("Disabled Features", features.Disabled, ignoreProperties))
                .Add(CreateLoadBalancingSection(globalSettings.Value))
                .Add(CreateCacheTypesSection())
                .Add(CreateCurrentUserSection(httpContext));
        }

        private DiagnosticSection CreateUmbracoVersionSection()
        {
            var section = new DiagnosticSection("Umbraco Version")
                .Add("Version", version.Version)
                .Add("Semantic Version", version.SemanticVersion)
                .Add("Assembly Version", version.AssemblyVersion)
                .Add("Assembly File Version", version.AssemblyFileVersion);

            if (!string.IsNullOrEmpty(version.Comment))
            {
                section.Add("Version Comment", version.Comment);
            }

            return section
                .Add("Runtime Level", runtimeState.Level)
                .Add("Current Migration State", runtimeState.CurrentMigrationState)
                .Add("Final Migration State", runtimeState.FinalMigrationState);
        }

        private DiagnosticGroup CreateServerConfigurationGroup()
        {
            return new DiagnosticGroup("Server Configuration")
                .Add(CreateServerSettingsSection())
                .Add(DiagnosticSection.From("Web Host Environment", webHostEnvironment))
                .Add(CreateWebServerFeaturesSection())
                .Add(DiagnosticSection.FromOptions<CookieOptions>("Cookie Options", factory))
                .Add(CreateEnvironmentVariablesSection());
        }

        private static DiagnosticSection CreateServerSettingsSection()
        {
            var section = new DiagnosticSection("Server Settings")
                .Add("Machine Name", Environment.MachineName)
                .Add("OS Version", Environment.OSVersion)
                .Add("64 Bit OS?", Environment.Is64BitOperatingSystem)
                .Add("Processor Count", Environment.ProcessorCount)
                .Add("Network Domain", Environment.UserDomainName)
                .Add("ASP.NET Version", Environment.Version)
                .Add("Current Directory", Environment.CurrentDirectory)
                .Add("64 Bit Process?", Environment.Is64BitProcess)
                .Add("Process Path", Environment.ProcessPath)
                .Add("Framework Bits", IntPtr.Size * 8)
                .Add("Process Physical Memory", $"{Environment.WorkingSet / 1048576:n} MB")
                .Add("System Up Time", TimeSpan.FromTicks(Environment.TickCount))
                .Add("CLI", Environment.CommandLine)
                .Add("System Directory", Environment.SystemDirectory)
                .Add("Logical Drives", string.Join(" ", Environment.GetLogicalDrives()));

            AddCurrentProcessDiagnostic(section);

            return section
                .Add("Current Culture", Thread.CurrentThread.CurrentCulture)
                .Add("Current Thread State", Thread.CurrentThread.ThreadState);
        }

        private static void AddCurrentProcessDiagnostic(DiagnosticSection section)
        {
            try
            {
                using var currentProcess = Process.GetCurrentProcess();

                if (currentProcess.MainModule != null)
                {
                    section.Add("Current Process", currentProcess.MainModule.ModuleName);
                }
            }
            catch
            {
                // Deliberate: MainModule can throw in restricted hosting environments.
            }
        }

        private DiagnosticSection CreateWebServerFeaturesSection()
        {
            return new DiagnosticSection(
                $"Web Server ({webServer.GetType().FullName}) Features",
                webServer.Features.Select(x => new Diagnostic(x.Key.ToString(), x.Value?.ToString() ?? string.Empty)));
        }

        private static DiagnosticSection CreateEnvironmentVariablesSection()
        {
            var section = new DiagnosticSection("Environment Variables");

            foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
            {
                section.Diagnostics.Add(new Diagnostic(kv.Key?.ToString() ?? string.Empty, kv.Value));
            }

            return section;
        }

        private DiagnosticGroup CreateEnvironmentConfigurationGroup()
        {
            return new DiagnosticGroup("Environment Config")
                .Add(EnvironmentConfigHelper.GetEnvironmentDiagnostics(configuration));
        }

        private DiagnosticGroup CreateHttpContextGroup()
        {
            var context = httpContext ?? httpContextAccessor.HttpContext;

            if (context == null)
            {
                return null;
            }

            var group = new DiagnosticGroup("HTTP Context")
                .Add(DiagnosticSection.From("HTTP Connection", context.Connection))
                .Add(DiagnosticSection.From("HTTP Features", context.Features));

            if (context.Items != null)
            {
                group.Add(CreateHttpContextItemsSection(context));
            }

            group.Add(DiagnosticSection.From("Host", context.Request.Host));

            return group;
        }

        private static DiagnosticSection CreateHttpContextItemsSection(HttpContext context)
        {
            var section = new DiagnosticSection("HTTP Context Items");

            foreach (var item in context.Items)
            {
                section.Diagnostics.Add(new Diagnostic(item.Key?.ToString() ?? string.Empty, item.Value));
            }

            return section;
        }

        private static DiagnosticGroup CreateUmbracoConstantsGroup()
        {
            var group = new DiagnosticGroup("Umbraco Constants");

            var constantTypes = typeof(Constants)
                .GetNestedTypes(BindingFlags.Public)
                .OrderBy(t => t.Name)
                .ToArray();

            foreach (var constantType in constantTypes)
            {
                group.Add(DiagnosticSection.FromConstants(
                    ToDiagnosticHeading(constantType.Name),
                    constantType));
            }

            // These are useful Umbraco constants, but they are not nested inside Constants.
            group.Add(
                DiagnosticSection.FromConstants("Authorization Policies", typeof(AuthorizationPolicies)),
                DiagnosticSection.FromConstants("Cache Keys", typeof(CacheKeys)));

            return group;
        }

        private DiagnosticGroup CreateDatabaseValuesGroup()
        {
            var globalSettings = factory.GetRequiredService<IOptions<GlobalSettings>>();

            return new DiagnosticGroup("Database Values")
                .AddIfNotNull(TryCreateKeyValueTableSection())
                .AddIfNotNull(TryCreateRegisteredServersSection())
                .Add(DiagnosticSection.From("Database Messenger Settings", globalSettings.Value.DatabaseServerMessenger, false))
                .Add(DiagnosticSection.From("Database Registrar Settings", globalSettings.Value.DatabaseServerRegistrar, false))
                .Add(CreateDistributedServicesSection(globalSettings.Value));
        }

        private DiagnosticSection TryCreateKeyValueTableSection()
        {
            try
            {
                var migrations = databaseService.GetKeyValues();

                if (migrations == null || !migrations.Any())
                {
                    return null;
                }

                return new DiagnosticSection(
                    "Key Value Table",
                    migrations.Select(migration => new Diagnostic(migration.Value, migration.ToDiagnostic())));
            }
            catch
            {
                // Deliberate: diagnostic output should not fail if the DB query fails.
                return null;
            }
        }

        private DiagnosticSection TryCreateRegisteredServersSection()
        {
            try
            {
                var servers = databaseService.GetRegistredServers();

                if (servers == null || !servers.Any())
                {
                    return null;
                }

                return new DiagnosticSection(
                    "Registered Servers",
                    servers.Select(server => new Diagnostic($"{server.Id}: {server.ComputerName}", server.ToDiagnostic())));
            }
            catch
            {
                // Deliberate: diagnostic output should not fail if the DB query fails.
                return null;
            }
        }

        private static DiagnosticSection CreateDistributedServicesSection(GlobalSettings globalSettings)
        {
            return new DiagnosticSection("Distributed Services")
                .Add("Disable Election For Single Server?", globalSettings.DisableElectionForSingleServer)
                .Add("Distribted Locking Mechanism", globalSettings.DistributedLockingMechanism)
                .Add("Distribted Read Lock Timeout", globalSettings.DistributedLockingReadLockDefaultTimeout)
                .Add("Distribted Write Lock Timeout", globalSettings.DistributedLockingWriteLockDefaultTimeout);
        }

        private static DiagnosticGroup CreateInfrastructureGroup()
        {
            return new DiagnosticGroup("Umbraco Infrastructure")
                .Add(DiagnosticSection.FromAssignableTypes("Background Tasks", typeof(Umbraco.Cms.Infrastructure.HostedServices.RecurringHostedServiceBase)))
                .Add(DiagnosticSection.FromAssignableTypes("Middleware", typeof(IMiddleware)))
                .Add(DiagnosticSection.FromAssignableTypes("Notifications", typeof(Umbraco.Cms.Core.Notifications.INotification)))
                .Add(DiagnosticSection.FromAssignableTypes("Telemetry", typeof(Umbraco.Cms.Core.Telemetry.ITelemetryService)));
        }

        private DiagnosticGroup CreateMvcConfigurationGroup()
        {
            var group = new DiagnosticGroup("MVC Configuration");

            var mvcVersionSection = CreateMvcVersionSection();

            if (mvcVersionSection != null)
            {
                group.Add(mvcVersionSection);
            }

            return group
                .Add(DiagnosticSection.FromOptions<MvcOptions>("MVC Options", factory))
                .Add(DiagnosticSection.FromOptions<Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions>("HTML Helper Options", factory))
                .Add(DiagnosticSection.FromOptions<CookieTempDataProviderOptions>("Cookie TempData Options", factory))
                .Add(DiagnosticSection.FromAssignableTypes("MVC Action Filters", typeof(IActionFilter)))
                .Add(DiagnosticSection.FromAssignableTypes("MVC Authorization Filters", typeof(IAuthorizationFilter)))
                .Add(DiagnosticSection.FromAssignableTypes("MVC Model Binders", typeof(IModelBinder)))
                .Add(DiagnosticSection.FromAssignableTypes("MVC Controller Factories", typeof(IControllerFactory)))
                .Add(DiagnosticSection.FromAssignableTypes("MVC Controllers", typeof(ControllerBase)));
        }

        private static DiagnosticSection CreateMvcVersionSection()
        {
            var mvcAssembly = typeof(Controller).Assembly;

            if (mvcAssembly == null)
            {
                return null;
            }

            return new DiagnosticSection("MVC Version")
                .Add("Assembly", mvcAssembly.GetName().Version)
                .Add("Version", mvcAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
                .Add("File Version", mvcAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                .Add("Full Name", mvcAssembly.FullName)
                .Add("Location", mvcAssembly.Location);
        }

        private static DiagnosticSection CreateApplicationBuildSection()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var executingAssembly = Assembly.GetExecutingAssembly();

            return new DiagnosticSection("Application Build")
                .Add("Entry Assembly", entryAssembly?.FullName)
                .Add("Entry Assembly Location", entryAssembly?.Location)
                .Add("Executing Assembly", executingAssembly.FullName)
                .Add("Executing Assembly Location", executingAssembly.Location)
                .Add("Informational Version", executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
                .Add("File Version", executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                .Add("Build Configuration", GetBuildConfiguration())
                .Add("Base Directory", AppContext.BaseDirectory);
        }

        private static string GetBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
    return "Release";
#endif
        }

        private static DiagnosticSection CreateDotNetRuntimeSection()
        {
            return new DiagnosticSection(".NET Runtime")
                .Add("Framework Description", RuntimeInformation.FrameworkDescription)
                .Add("OS Description", RuntimeInformation.OSDescription)
                .Add("OS Architecture", RuntimeInformation.OSArchitecture)
                .Add("Process Architecture", RuntimeInformation.ProcessArchitecture)
                .Add("Runtime Identifier", RuntimeInformation.RuntimeIdentifier)
                .Add("GC Server Mode", System.Runtime.GCSettings.IsServerGC)
                .Add("GC Latency Mode", System.Runtime.GCSettings.LatencyMode)
                .Add("GC LOH Compaction Mode", System.Runtime.GCSettings.LargeObjectHeapCompactionMode)
                .Add("Total Allocated Bytes", GC.GetTotalAllocatedBytes())
                .Add("Total Memory", GC.GetTotalMemory(false));
        }

        private static DiagnosticSection CreateProcessSection()
        {
            using var process = Process.GetCurrentProcess();

            return new DiagnosticSection("Current Process")
                .Add("Process Name", process.ProcessName)
                .Add("Process ID", process.Id)
                .Add("Start Time", SafeGet(() => process.StartTime))
                .Add("Total Processor Time", process.TotalProcessorTime)
                .Add("Threads", process.Threads.Count)
                .Add("Handle Count", SafeGet(() => process.HandleCount))
                .Add("Working Set", FormatBytes(process.WorkingSet64))
                .Add("Private Memory", FormatBytes(process.PrivateMemorySize64))
                .Add("Paged Memory", FormatBytes(process.PagedMemorySize64))
                .Add("Peak Working Set", FormatBytes(process.PeakWorkingSet64));
        }


        private static DiagnosticSection CreateDiskSpaceSection()
        {
            var section = new DiagnosticSection("Disk Space");

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                section.Add(
                    $"{drive.Name} Available",
                    $"{FormatBytes(drive.AvailableFreeSpace)} free of {FormatBytes(drive.TotalSize)}");
            }

            return section;
        }

        private static DiagnosticSection CreateLoadedUmbracoAssembliesSection()
        {
            var assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.GetName().Name?.StartsWith("Umbraco.", StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(a => a.GetName().Name)
                .Select(a =>
                {
                    var name = a.GetName();

                    return new Diagnostic(
                        name.Name,
                        a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? name.Version?.ToString()
                            ?? "Unknown");
                });

            return new DiagnosticSection("Loaded Umbraco Assemblies", assemblies);
        }

        private static DiagnosticSection CreateDependencySection()
        {
            var section = new DiagnosticSection("Dependencies");

            try
            {
                var depsFile = Directory
                    .GetFiles(AppContext.BaseDirectory, "*.deps.json")
                    .FirstOrDefault();

                if (depsFile == null)
                {
                    return section.Add("Status", "No .deps.json file found");
                }

                using var document = JsonDocument.Parse(System.IO.File.ReadAllText(depsFile));

                if (!document.RootElement.TryGetProperty("libraries", out var libraries))
                {
                    return section.Add("Status", "No libraries section found in .deps.json");
                }

                foreach (var library in libraries.EnumerateObject().OrderBy(x => x.Name))
                {
                    if (library.Name.StartsWith("Umbraco.", StringComparison.OrdinalIgnoreCase) ||
                        library.Name.StartsWith("Diplo.", StringComparison.OrdinalIgnoreCase))
                    {
                        section.Add(library.Name, library.Value.GetProperty("type").GetString());
                    }
                }
            }
            catch (Exception ex)
            {
                section.Add("Error", ex.Message);
            }

            return section;
        }

        private DiagnosticSection CreateLoggingConfigurationSection()
        {
            var section = new DiagnosticSection("Logging Configuration");

            section
                .Add("Serilog:MinimumLevel:Default", configuration["Serilog:MinimumLevel:Default"])
                .Add("Serilog:MinimumLevel:Override:Microsoft", configuration["Serilog:MinimumLevel:Override:Microsoft"])
                .Add("Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime", configuration["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime"])
                .Add("Serilog:MinimumLevel:Override:System", configuration["Serilog:MinimumLevel:Override:System"])
                .Add("Umbraco:CMS:Logging:MaxLogAge", configuration["Umbraco:CMS:Logging:MaxLogAge"]);

            return section;
        }



        private DiagnosticSection CreateConfigurationProvidersSection()
        {
            var section = new DiagnosticSection("Configuration Providers");

            if (configuration is IConfigurationRoot root)
            {
                foreach (var provider in root.Providers)
                {
                    section.Add(provider.ToString(), provider.GetType().FullName);
                }
            }
            else
            {
                section.Add("Status", "Configuration is not an IConfigurationRoot");
            }

            return section;
        }

        private DiagnosticSection CreateLoadBalancingSection(GlobalSettings globalSettings)
        {
            return new DiagnosticSection("Load Balancing")
                .Add("Server Role", runtimeState.ToString())
                .Add("Disable Election For Single Server", globalSettings.DisableElectionForSingleServer)
                .Add("Database Server Registrar", globalSettings.DatabaseServerRegistrar)
                .Add("Database Server Messenger", globalSettings.DatabaseServerMessenger)
                .Add("Distributed Locking Mechanism", globalSettings.DistributedLockingMechanism)
                .Add("Distributed Read Lock Timeout", globalSettings.DistributedLockingReadLockDefaultTimeout)
                .Add("Distributed Write Lock Timeout", globalSettings.DistributedLockingWriteLockDefaultTimeout);
        }

        private DiagnosticSection CreateCacheTypesSection()
        {
            return new DiagnosticSection("Cache Types")
                .Add("App Caches Type", factory.GetService<AppCaches>()?.GetType().FullName)
                .Add("Runtime Cache Type", factory.GetService<AppCaches>()?.RuntimeCache?.GetType().FullName)
                .Add("Request Cache Type", factory.GetService<AppCaches>()?.RequestCache?.GetType().FullName)
                .Add("Isolated Caches Type", factory.GetService<AppCaches>()?.IsolatedCaches?.GetType().FullName);
        }

        private static DiagnosticSection CreateCurrentUserSection(HttpContext context)
        {
            var user = context.User;

            return new DiagnosticSection("Current User")
                .Add("Authenticated", user.Identity?.IsAuthenticated)
                .Add("Authentication Type", user.Identity?.AuthenticationType)
                .Add("Name", user.Identity?.Name)
                .Add("Claims Count", user.Claims.Count());
        }

        private static DiagnosticGroup CreateApplicationGroup()
        {
            return new DiagnosticGroup("Application")
                .Add(CreateApplicationBuildSection())
                .Add(CreateLoadedUmbracoAssembliesSection())
                .Add(CreateDependencySection());
        }

        private static DiagnosticGroup CreateRuntimeGroup()
        {
            return new DiagnosticGroup("Runtime")
                .Add(CreateDotNetRuntimeSection())
                .Add(CreateProcessSection())
                .Add(CreateDiskSpaceSection());
        }

        private DiagnosticGroup CreateConfigurationSourcesGroup()
        {
            return new DiagnosticGroup("Configuration Sources")
                .Add(CreateConfigurationProvidersSection())
                .Add(CreateLoggingConfigurationSection());
        }

        private static DiagnosticGroup CreateUmbracoPluginTypesGroup()
        {
            return new DiagnosticGroup("Umbraco Plugin Types")
                .Add(DiagnosticSection.FromAssignableTypes("Actions", typeof(Umbraco.Cms.Core.Actions.IAction)))
                .Add(DiagnosticSection.FromAssignableTypes("Cache Refreshers", typeof(Umbraco.Cms.Core.Cache.ICacheRefresher)))
                .Add(DiagnosticSection.FromAssignableTypes("Data Editors", typeof(Umbraco.Cms.Core.PropertyEditors.IDataEditor)))
                .Add(DiagnosticSection.FromAssignableTypes("Editor Validators", typeof(Umbraco.Cms.Core.PropertyEditors.IConfigurationEditor)))
                .Add(DiagnosticSection.FromAssignableTypes("Filter Handlers", typeof(Umbraco.Cms.Core.PropertyEditors.IFileExtensionConfigItem)))
                .Add(DiagnosticSection.FromAssignableTypes("Health Checks", typeof(Umbraco.Cms.Core.HealthChecks.HealthCheck)))
                .Add(DiagnosticSection.FromAssignableTypes("Manifest Filters", typeof(Umbraco.Cms.Core.Manifest.IPackageManifestService)))
                .Add(DiagnosticSection.FromAssignableTypes("Property Value Converters", typeof(Umbraco.Cms.Core.PropertyEditors.IPropertyValueConverter)))
                .Add(DiagnosticSection.FromAssignableTypes("Value Validators", typeof(Umbraco.Cms.Core.PropertyEditors.IValueValidator)))
                .AddIfNotNull(CreateOptionalAssignableTypesSection(
                    "Sort Handlers",
                    "Umbraco.Cms.Core.PropertyEditors.IDataValueSortable, Umbraco.Cms.Core"));
        }

        private DiagnosticSection? CreateOptionalOptionsSection(string heading, string assemblyQualifiedTypeName)
        {
            var optionsType = Type.GetType(assemblyQualifiedTypeName);
            if (optionsType == null)
            {
                return null;
            }

            var serviceType = typeof(IOptions<>).MakeGenericType(optionsType);
            var settings = factory.GetService(serviceType);
            var value = settings?.GetType().GetProperty(nameof(IOptions<object>.Value))?.GetValue(settings);

            return value == null ? null : DiagnosticSection.From(heading, value);
        }

        private static DiagnosticSection? CreateOptionalAssignableTypesSection(string heading, string assemblyQualifiedTypeName)
        {
            var type = Type.GetType(assemblyQualifiedTypeName);

            return type == null ? null : DiagnosticSection.FromAssignableTypes(heading, type);
        }

        private static string ToDiagnosticHeading(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var chars = new List<char>(name.Length + 8);

            for (var i = 0; i < name.Length; i++)
            {
                var current = name[i];

                if (i > 0 &&
                    char.IsUpper(current) &&
                    !char.IsUpper(name[i - 1]))
                {
                    chars.Add(' ');
                }

                chars.Add(current);
            }

            return new string(chars.ToArray());
        }

        private static DiagnosticSection CreateUmbracoMapperDefinitionsSection()
        {
            return DiagnosticSection.FromAssignableTypes(
                "Umbraco Mapper Definitions",
                typeof(Umbraco.Cms.Core.Mapping.IMapDefinition));
        }

        private DiagnosticSection CreateUmbracoMapperServiceSection()
        {
            var section = new DiagnosticSection("Umbraco Mapper Service");

            var mapper = factory.GetService<Umbraco.Cms.Core.Mapping.IUmbracoMapper>();

            if (mapper == null)
            {
                return section.Add("IUmbracoMapper", "Not registered");
            }

            return section
                .Add("IUmbracoMapper", "Registered")
                .Add("Implementation Type", mapper.GetType().FullName);
        }

        private DiagnosticGroup CreateMappingGroup()
        {
            return new DiagnosticGroup("Mapping")
                .Add(CreateUmbracoMapperServiceSection())
                .Add(CreateUmbracoMapperDefinitionsSection());
        }

        private static object SafeGet(Func<object> getter)
        {
            try
            {
                return getter();
            }
            catch
            {
                return "Unavailable";
            }
        }

        private static string FormatBytes(long bytes)
        {
            const decimal kb = 1024;
            const decimal mb = kb * 1024;
            const decimal gb = mb * 1024;

            return bytes switch
            {
                >= (long)gb => $"{bytes / gb:n2} GB",
                >= (long)mb => $"{bytes / mb:n2} MB",
                >= (long)kb => $"{bytes / kb:n2} KB",
                _ => $"{bytes:n0} bytes"
            };
        }

        private static void AssignGroupIds(IList<DiagnosticGroup> groups)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                groups[i].SetId(i);
            }
        }

        private void Add()
        {

        }

        private void RedactGroups(List<DiagnosticGroup> groups, bool revealRedactedValues)
        {
            groups.RemoveAll(g => godModeConfig.Value.Diagnostics.GroupsToHide.InvariantContains(g.Title));

            foreach (var g in groups)
            {
                g.Sections.RemoveAll(s => godModeConfig.Value.Diagnostics.SectionsToHide.InvariantContains(s.Heading));

                foreach (var s in g.Sections)
                {
                    if (revealRedactedValues)
                    {
                        continue;
                    }

                    foreach (var d in s.Diagnostics)
                    {
                        var sectionRedactionKey = g.Title == "Environment Config"
                            ? d.Key
                            : $"{s.Heading}:{d.Key}";
                        var groupRedactionKey = g.Title == "Environment Config"
                            ? d.Key
                            : $"{g.Title}:{s.Heading}:{d.Key}";

                        if (ShouldRedact(d.Key, sectionRedactionKey, groupRedactionKey))
                        {
                            d.Value = StringHelper.RedactString(d.Value);
                        }
                    }
                }
            }
        }

        private bool ShouldRedact(string diagnosticKey, params string[] scopedRedactionKeys)
        {
            var config = godModeConfig.Value.Diagnostics;

            if (config.KeysToRedact.InvariantContains(diagnosticKey) ||
                scopedRedactionKeys.Any(key => config.KeysToRedact.InvariantContains(key)))
            {
                return true;
            }

            return config.KeyMatchesToRedact?.Any(match =>
                !string.IsNullOrWhiteSpace(match) &&
                diagnosticKey.Contains(match, StringComparison.OrdinalIgnoreCase)) == true;
        }
    }
}
