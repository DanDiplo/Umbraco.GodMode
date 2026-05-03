using System.Reflection;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
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
        private readonly IWebHostEnvironment env;
        private readonly IOptions<ImagingCacheSettings> imageCacheSettings;
        private readonly AppCaches caches;
        private readonly ILogger<UtilitiesService> logger;
        private readonly IUmbracoContextFactory umbracoContextFactory;
        private readonly IMemoryCache memoryCache;
        private readonly IPublishedContentCache publishedContentCache;
        private readonly IDocumentNavigationQueryService documentNavigation;

        public UtilitiesService(IWebHostEnvironment env, IOptions<ImagingCacheSettings> imageCacheSettings, AppCaches caches, ILogger<UtilitiesService> logger, IUmbracoContextFactory umbracoContextFactory, IMemoryCache memoryCache, IPublishedContentCache publishedContentCache, IDocumentNavigationQueryService documentNavigation)
        {
            this.env = env;
            this.imageCacheSettings = imageCacheSettings;
            this.caches = caches;
            this.logger = logger;
            this.umbracoContextFactory = umbracoContextFactory;
            this.memoryCache = memoryCache;
            this.publishedContentCache = publishedContentCache;
            this.documentNavigation = documentNavigation;
        }

        /// <summary>
        /// Clears one or all of the Umbraco caches
        /// </summary>
        /// <param name="cache">The cache to clear</param>
        public ServerResponse ClearUmbracoCacheFor(string cache)
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
                else if (cache == "Other" || cache == "all")
                {
                    if (this.memoryCache != null)
                    {
                        ClearMemoryCache(this.memoryCache);
                    }
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

            var folderPath = env.MapPathContentRoot(imageCacheSettings.Value.CacheFolder);

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
    }
}
