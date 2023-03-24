using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smidge.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Diplo.GodMode.Services
{
    public class UtilitiesService : IUtilitiesService
    {
        private readonly IWebHostEnvironment env;
        private readonly IOptions<ImagingCacheSettings> imageCacheSettings;
        private readonly AppCaches caches;
        private readonly ILogger<UtilitiesService> logger;
        private readonly ILocalizationService localizationService;
        private readonly IUmbracoContextFactory umbracoContextFactory;

        public UtilitiesService(IWebHostEnvironment env, IOptions<ImagingCacheSettings> imageCacheSettings, AppCaches caches, ILogger<UtilitiesService> logger, ILocalizationService localizationService, IUmbracoContextFactory umbracoContextFactory)
        {
            this.env = env;
            this.imageCacheSettings = imageCacheSettings;
            this.caches = caches;
            this.logger = logger;
            this.localizationService = localizationService;
            this.umbracoContextFactory = umbracoContextFactory;
        }

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

        public IEnumerable<string> GetAllUrls(string culture)
        {
            using (var ctx = umbracoContextFactory.EnsureUmbracoContext())
            {
                return ctx.UmbracoContext.Content.GetAtRoot(culture).SelectMany(x => x.DescendantsOrSelf(culture)).Where(p => p.TemplateId > 0).Select(p => p.Url(culture: culture, mode: UrlMode.Absolute));
            }
        }
    }
}
