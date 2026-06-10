using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    /// <summary>
    /// Some general functions used in the Utilities page
    /// </summary>
    public class UtilitiesService : IUtilitiesService
    {
        private static readonly DateTime StartedAt = DateTime.UtcNow;

        private readonly IWebHostEnvironment env;
        private readonly IOptions<ImagingCacheSettings> imageCacheSettings;
        private readonly AppCaches caches;
        private readonly ILogger<UtilitiesService> logger;
        private readonly IUmbracoContextFactory umbracoContextFactory;
        private readonly IMemoryCache memoryCache;
        private readonly IPublishedContentCache publishedContentCache;
        private readonly IDocumentNavigationQueryService documentNavigation;
        private readonly IConfiguration configuration;
        private readonly IUmbracoDatabaseService databaseService;
        private readonly IUmbracoVersion umbracoVersion;
        private readonly IServer webServer;
        private readonly WebRoutingSettings webRoutingSettings;
        private readonly NuCacheSettings nuCacheSettings;

        public UtilitiesService(IWebHostEnvironment env, IOptions<ImagingCacheSettings> imageCacheSettings, IConfiguration configuration, AppCaches caches, ILogger<UtilitiesService> logger, IUmbracoContextFactory umbracoContextFactory, IMemoryCache memoryCache, IPublishedContentCache publishedContentCache, IDocumentNavigationQueryService documentNavigation, IUmbracoDatabaseService databaseService, IUmbracoVersion umbracoVersion, IServer webServer, IOptions<WebRoutingSettings> webRoutingSettings, IOptions<NuCacheSettings> nuCacheSettings)
        {
            this.env = env;
            this.imageCacheSettings = imageCacheSettings;
            this.configuration = configuration;
            this.caches = caches;
            this.logger = logger;
            this.umbracoContextFactory = umbracoContextFactory;
            this.memoryCache = memoryCache;
            this.publishedContentCache = publishedContentCache;
            this.documentNavigation = documentNavigation;
            this.databaseService = databaseService;
            this.umbracoVersion = umbracoVersion;
            this.webServer = webServer;
            this.webRoutingSettings = webRoutingSettings.Value;
            this.nuCacheSettings = nuCacheSettings.Value;
        }

        /// <summary>
        /// Clears one or all of the Umbraco caches
        /// </summary>
        /// <param name="cache">The cache to clear</param>
        public ServerResponse ClearUmbracoCacheFor(string cache)
        {
            try
            {
                var cacheType = cache?.Trim();
                var clearAll = string.Equals(cacheType, "all", StringComparison.OrdinalIgnoreCase);

                if (string.Equals(cacheType, "Request", StringComparison.OrdinalIgnoreCase) || clearAll)
                {
                    caches.RequestCache.Clear();
                }

                if (string.Equals(cacheType, "Runtime", StringComparison.OrdinalIgnoreCase) || clearAll)
                {
                    caches.RuntimeCache.Clear();
                }

                if (string.Equals(cacheType, "Isolated", StringComparison.OrdinalIgnoreCase) || clearAll)
                {
                    caches.IsolatedCaches.ClearAllCaches();
                }

                if (string.Equals(cacheType, "Partial", StringComparison.OrdinalIgnoreCase) || clearAll)
                {
                    caches.ClearPartialViewCache();
                }

                if (string.Equals(cacheType, "Other", StringComparison.OrdinalIgnoreCase) || clearAll)
                {
                    if (this.memoryCache != null)
                    {
                        ClearMemoryCache(this.memoryCache);
                    }
                }

                if (!clearAll
                    && !string.Equals(cacheType, "Request", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(cacheType, "Runtime", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(cacheType, "Isolated", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(cacheType, "Partial", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(cacheType, "Other", StringComparison.OrdinalIgnoreCase))
                {
                    return new ServerResponse(cache + " Is not a valid cache type", ServerResponseType.Warning);
                }

                if (clearAll)
                {
                    return new ServerResponse("All Caches were successfully cleared", ServerResponseType.Success);
                }
                else
                {
                    return new ServerResponse("The " + cacheType + " Cache was successfully cleared", ServerResponseType.Success);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing Umbraco cache");
                return new ServerResponse("Error clearing cache: " + ex.Message, ServerResponseType.Error);
            }
        }

        /// <summary>
        /// Delets the media cache folder and all cached image crops
        /// </summary>
        public async Task<ServerResponse> ClearMediaFileCacheAsync()
        {
            var cacheFolder = imageCacheSettings.Value.CacheFolder;
            var folderPath = GetConfiguredContentRootPath(cacheFolder);

            if (!Directory.Exists(folderPath))
            {
                return new ServerResponse($"The media cache folder could be not be found at the path {folderPath}", ServerResponseType.Warning);
            }

            bool deleted = await IOHelper.DeleteDirectoryRecursivelyWithRetriesAsync(folderPath);

            if (deleted)
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                    return new ServerResponse($"Deleted media cache folder {cacheFolder}", ServerResponseType.Success);
                }
                catch (IOException ex)
                {
                    logger.LogError(ex, "Couldn't create media cache folder at {folder}", folderPath);
                    return new ServerResponse($"Deleted media cache folder {cacheFolder} but couldn't recreate the directory", ServerResponseType.Warning);
                }
            }
            else
            {
                return new ServerResponse($"Unable to delete media cache folder {cacheFolder}", ServerResponseType.Error);
            }
        }

        public UtilityDiagnostics GetDiagnostics()
        {
            var assembly = typeof(UtilitiesService).Assembly;
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var appPluginsRoot = Path.Combine(webRoot, "App_Plugins", "DiploGodMode");
            var mediaCacheFolder = GetConfiguredContentRootPath(imageCacheSettings.Value.CacheFolder);
            var packageManifest = CreateAssetCheck("Package manifest", "/App_Plugins/DiploGodMode/umbraco-package.json", "App_Plugins/DiploGodMode/umbraco-package.json", Path.Combine(appPluginsRoot, "umbraco-package.json"), env.WebRootFileProvider);
            var entryPoint = CreateAssetCheck("Entry point", "/App_Plugins/DiploGodMode/index.js", "App_Plugins/DiploGodMode/index.js", Path.Combine(appPluginsRoot, "index.js"), env.WebRootFileProvider);
            var manifestBundle = CreateAssetCheck("Manifest bundle", "/App_Plugins/DiploGodMode/index2.js", "App_Plugins/DiploGodMode/index2.js", Path.Combine(appPluginsRoot, "index2.js"), env.WebRootFileProvider);
            var packageAssetsPath = packageManifest.Path is not "" ? Path.GetDirectoryName(packageManifest.Path) : appPluginsRoot;
            var appDataTempFolder = CreateFolderSize("App_Data temp", Path.Combine(env.ContentRootPath, "App_Data", "TEMP"));
            var umbracoTempFolder = GetConfiguredContentRootPath("~/umbraco/Data/TEMP");
            var folders = new List<FolderSizeInfo>
            {
                CreateFolderSize("Umbraco temp", umbracoTempFolder),
                CreateFolderSize("Media cache", mediaCacheFolder),
                CreateFolderSize("GodMode assets", packageAssetsPath ?? appPluginsRoot)
            };
            var cacheFolders = new List<FolderSizeInfo>
            {
                CreateFolderSize("Umbraco temp cache", umbracoTempFolder)
            };

            if (appDataTempFolder.Exists && appDataTempFolder.FileCount > 0)
            {
                folders.Add(appDataTempFolder);
                cacheFolders.Add(new FolderSizeInfo
                {
                    Label = "App_Data temp cache",
                    Path = appDataTempFolder.Path,
                    Exists = appDataTempFolder.Exists,
                    Size = appDataTempFolder.Size,
                    FileCount = appDataTempFolder.FileCount
                });
            }

            var process = Process.GetCurrentProcess();

            return new UtilityDiagnostics
            {
                App = new AppInfo
                {
                    UmbracoVersion = umbracoVersion.Version.ToString(),
                    UmbracoSemanticVersion = umbracoVersion.SemanticVersion.ToSemanticString(),
                    DotNetVersion = RuntimeInformation.FrameworkDescription,
                    RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                    OperatingSystem = RuntimeInformation.OSDescription,
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    WebServer = webServer.GetType().FullName ?? webServer.GetType().Name,
                    ApplicationMainUrl = webRoutingSettings.UmbracoApplicationUrl?.ToString()
                        ?? configuration["Umbraco:CMS:WebRouting:UmbracoApplicationUrl"]
                        ?? configuration["Umbraco:CMS:WebRouting:ApplicationMainUrl"]
                        ?? string.Empty,
                    DebugMode = env.EnvironmentName.InvariantEquals("Development") || GetBoolConfigurationValue("Umbraco:CMS:Debug:DebugMode"),
                    EnvironmentName = env.EnvironmentName,
                    MachineName = Environment.MachineName,
                    ContentRootPath = env.ContentRootPath,
                    WebRootPath = webRoot,
                    ProcessId = Environment.ProcessId,
                    StartedAt = StartedAt,
                    Uptime = FormatDuration(DateTime.UtcNow - StartedAt),
                    GodModeVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                        ?? assembly.GetName().Version?.ToString()
                        ?? string.Empty
                },
                Assets =
                [
                    packageManifest,
                    entryPoint,
                    manifestBundle
                ],
                Folders = folders,
                Cache = new CacheStatus
                {
                    PublishedContentCacheType = publishedContentCache.GetType().FullName ?? publishedContentCache.GetType().Name,
                    NuCacheSerializerType = nuCacheSettings.NuCacheSerializerType.ToString(),
                    Settings = GetCacheSettings(),
                    Folders = cacheFolders,
                    DatabaseRows = databaseService.GetCacheHealthRows()
                },
                ServerStats = CreateServerStats(process),
                Database = databaseService.GetDatabaseHealthRows()
            };
        }

        /// <summary>
        /// Fetches all the URLs on the site for a given culture
        /// </summary>
        /// <param name="culture">The culture</param>
        /// <returns></returns>
        public IEnumerable<string> GetAllUrls(string culture)
        {
            using var ctx = umbracoContextFactory.EnsureUmbracoContext();

            if (!documentNavigation.TryGetRootKeys(out var rootKeys))
            {
                return [];
            }

            var results = new List<string>();

            foreach (var rootKey in rootKeys)
            {
                if (!documentNavigation.TryGetDescendantsKeysOrSelfKeys(rootKey, out var keys))
                {
                    continue;
                }

                foreach (var key in keys)
                {
                    var content = publishedContentCache.GetByIdAsync(key).GetAwaiter().GetResult();

                    if (content?.TemplateId > 0)
                    {
                        results.Add(content.Url(culture: culture, mode: UrlMode.Absolute));
                    }
                }
            }

            return results;
        }

        private void ClearMemoryCache(IMemoryCache memoryCache)
        {
            PropertyInfo prop = memoryCache.GetType().GetProperty("EntriesCollection", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop is null)
                return;

            try
            {
                object innerCache = prop.GetValue(memoryCache);

                if (innerCache != null)
                {
                    MethodInfo clearMethod = innerCache.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                    clearMethod.Invoke(innerCache, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Couldn't clear IMemoryCache in GodMode");
            }
        }

        private static AssetCheck CreateAssetCheck(string label, string url, string relativePath, string fallbackPath, IFileProvider fileProvider)
        {
            var file = fileProvider.GetFileInfo(relativePath);
            var path = file.PhysicalPath ?? fallbackPath;
            var exists = file.Exists || File.Exists(path);

            return new AssetCheck
            {
                Label = label,
                Url = url,
                Path = path,
                Exists = exists,
                Size = file.Exists ? file.Length : exists ? new FileInfo(path).Length : 0
            };
        }

        private static FolderSizeInfo CreateFolderSize(string label, string path)
        {
            if (!Directory.Exists(path))
            {
                return new FolderSizeInfo { Label = label, Path = path, Exists = false };
            }

            long size = 0;
            var count = 0;

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                        count++;
                    }
                    catch
                    {
                        // Some runtime files may be locked or disappear during enumeration.
                    }
                }
            }
            catch
            {
                // Surface partial/unavailable folder state without failing the whole diagnostics call.
            }

            return new FolderSizeInfo { Label = label, Path = path, Exists = true, Size = size, FileCount = count };
        }

        private static string FormatDuration(TimeSpan duration)
            => duration.TotalDays >= 1
                ? $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m"
                : $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";

        private static ServerStats CreateServerStats(Process process)
        {
            var gcMemory = GC.GetGCMemoryInfo();

            return new ServerStats
            {
                Memory = new MemoryStats
                {
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateMemoryBytes = process.PrivateMemorySize64,
                    ManagedHeapBytes = GC.GetTotalMemory(false),
                    TotalAvailableMemoryBytes = gcMemory.TotalAvailableMemoryBytes,
                    TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false)
                },
                Disks = DriveInfo.GetDrives()
                    .Where(drive => drive.IsReady && drive.TotalSize > 0)
                    .Select(drive =>
                    {
                        var used = drive.TotalSize - drive.AvailableFreeSpace;

                        return new DiskStats
                        {
                            Name = drive.Name,
                            Format = drive.DriveFormat,
                            TotalBytes = drive.TotalSize,
                            FreeBytes = drive.AvailableFreeSpace,
                            UsedBytes = used,
                            UsedPercentage = Math.Round(used / (double)drive.TotalSize * 100, 1)
                        };
                    })
                    .ToList(),
                ProcessorCount = Environment.ProcessorCount,
                ThreadCount = process.Threads.Count,
                HandleCount = SafeGet(() => process.HandleCount)
            };
        }

        private static int SafeGet(Func<int> valueFactory)
        {
            try
            {
                return valueFactory();
            }
            catch
            {
                return 0;
            }
        }

        private IEnumerable<CacheSettingInfo> GetCacheSettings()
        {
            return
            [
                CreateCacheSetting("Content type keys", "Umbraco:CMS:Cache:ContentTypeKeys"),
                CreateCacheSetting("Document breadth-first seed count", "Umbraco:CMS:Cache:DocumentBreadthFirstSeedCount"),
                CreateCacheSetting("Media breadth-first seed count", "Umbraco:CMS:Cache:MediaBreadthFirstSeedCount"),
                CreateCacheSetting("Document seed batch size", "Umbraco:CMS:Cache:DocumentSeedBatchSize"),
                CreateCacheSetting("Media seed batch size", "Umbraco:CMS:Cache:MediaSeedBatchSize"),
                CreateCacheSetting("Document local duration", "Umbraco:CMS:Cache:Entry:Document:LocalCacheDuration"),
                CreateCacheSetting("Document remote duration", "Umbraco:CMS:Cache:Entry:Document:RemoteCacheDuration"),
                CreateCacheSetting("Document seed duration", "Umbraco:CMS:Cache:Entry:Document:SeedCacheDuration"),
                CreateCacheSetting("Media local duration", "Umbraco:CMS:Cache:Entry:Media:LocalCacheDuration"),
                CreateCacheSetting("Media remote duration", "Umbraco:CMS:Cache:Entry:Media:RemoteCacheDuration"),
                CreateCacheSetting("Media seed duration", "Umbraco:CMS:Cache:Entry:Media:SeedCacheDuration"),
                CreateCacheSetting("Legacy UsePagedSqlQuery", "Umbraco:CMS:NuCache:UsePagedSqlQuery"),
                CreateCacheSetting("Legacy SQL page size", "Umbraco:CMS:NuCache:SqlPageSize"),
                CreateCacheSetting("Legacy serializer type", "Umbraco:CMS:NuCache:NuCacheSerializerType")
            ];
        }

        private CacheSettingInfo CreateCacheSetting(string label, string path)
        {
            var section = configuration.GetSection(path);
            var children = section.GetChildren().Select(x => x.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var value = children.Count != 0
                ? string.Join(", ", children)
                : configuration[path];

            return new CacheSettingInfo
            {
                Label = label,
                Path = path,
                Value = string.IsNullOrWhiteSpace(value) ? "Default" : value
            };
        }

        private bool GetBoolConfigurationValue(string path)
            => bool.TryParse(configuration[path], out var value) && value;

        private string GetConfiguredContentRootPath(string configuredPath)
            => IOHelper.ResolveConfiguredContentRootPath(env.ContentRootPath, configuredPath);
    }
}
