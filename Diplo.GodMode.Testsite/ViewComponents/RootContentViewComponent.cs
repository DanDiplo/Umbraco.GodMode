using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Diplo.GodMode.Testsite.ViewComponents
{
    [ViewComponent(Name = "RootContentList")]
    public class RootContentViewComponent : ViewComponent
    {
        private readonly IPublishedContentQuery publishedContentQuery;

        public RootContentViewComponent(IPublishedContentQuery publishedContentQuery)
        {
            this.publishedContentQuery = publishedContentQuery;
        }

        public async Task<IViewComponentResult> InvokeAsync(int maxItems = 3)
        {
            var items = GetRootContent().Take(maxItems);
            return View(items);
        }

        public IEnumerable<IPublishedContent> GetRootContent()
        {
            return publishedContentQuery.ContentAtRoot().SelectMany(x => x.Children());
        }
    }
}
