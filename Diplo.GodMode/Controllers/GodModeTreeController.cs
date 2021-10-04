using System.Net;
using System.Net.Http.Formatting;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;
using Umbraco.Web.WebApi.Filters;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Custom Umbraco tree for GodMode under the Developer section of Umbraco
    /// </summary>
    [Tree(Constants.Applications.Settings, treeAlias: GodModeSettings.TreeAlias, TreeTitle = "God Mode", TreeGroup = Constants.Trees.Groups.ThirdParty, SortOrder = 12)]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    [PluginController(GodModeSettings.PluginAreaName)]
    public class GodModeTreeController : TreeController
    {
        private static readonly string baseUrl = $"{Constants.Applications.Settings}/{GodModeSettings.TreeAlias}/";

        public const string ReflectionTree = "reflectionTree";

        /// <summary>
        /// The method called to render the contents of the tree structure
        /// </summary>
        /// <param name="id">The parent Id</param>
        /// <param name="queryStrings">All of the query string parameters passed from jsTree</param>
        /// <returns>The tree nodes</returns>
        /// <exception cref="HttpResponseException">HTTP Not Found</exception>
        /// <remarks>
        /// We are allowing an arbitrary number of query strings to be pased in so that developers are able to persist custom data from the front-end
        /// to the back end to be used in the query for model data.
        /// </remarks>
        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            if (id != Constants.System.RootString && id != ReflectionTree)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var tree = new TreeNodeCollection();

            if (id == Constants.System.RootString)
            {
                tree.AddRange(PopulateTreeNodes(id, queryStrings));
            }

            if (id == ReflectionTree)
            {
                tree.AddRange(PopulateReflectionTree(ReflectionTree, queryStrings));
            }

            return tree;
        }

        /// <summary>
        /// Enables the root node to have it's own view
        /// </summary>
        protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
        {
            var root = base.CreateRootNode(queryStrings);
            root.RoutePath = string.Format("{0}/{1}/{2}", Constants.Applications.Settings, GodModeSettings.TreeAlias, "intro");
            root.Icon = "icon-sience";
            root.HasChildren = true;
            root.MenuUrl = null;

            return root;
        }

        /// <summary>
        /// Returns the menu structure for the node
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="queryStrings">Any querystring params</param>
        /// <returns>The menu item collection</returns>
        protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
        {
            var menu = new MenuItemCollection();

            if (id == Constants.System.Root.ToInvariantString())
            {
                menu.Items.Add(new RefreshNode(Services.TextService, true)); // adds refresh link to right-click
            }

            return menu;
        }

        private TreeNodeCollection PopulateTreeNodes(string parentId, FormDataCollection qs)
        {
            // path is PluginController name + area name + template name eg. /App_Plugins/DiploGodMode/GodModeTree/
            // The first part of the name eg. docTypeBrowser is the Id - this is used by Angular navigationService to identify the node

            var tree = new TreeNodeCollection
            {
                CreateTreeNode("docTypeBrowser", parentId, qs, "DocType Browser", "icon-item-arrangement", false, baseUrl + "docTypeBrowser/browse"),

                CreateTreeNode("templateBrowser", parentId, qs, "Template Browser", "icon-newspaper-alt", false, baseUrl + "templateBrowser/browse"),

                CreateTreeNode("partialBrowser", parentId, qs, "Partial Browser", "icon-article", false, baseUrl + "partialBrowser/browse"),

                CreateTreeNode("dataTypeBrowser", parentId, qs, "DataType Browser", "icon-autofill", false, baseUrl + "dataTypeBrowser/browse"),

                CreateTreeNode("contentBrowser", parentId, qs, "Content Browser", "icon-umb-content", false, baseUrl + "contentBrowser/browse"),

                CreateTreeNode("usageBrowser", parentId, qs, "Usage Browser", "icon-chart-curve", false, baseUrl + "usageBrowser/browse"),

                CreateTreeNode("mediaBrowser", parentId, qs, "Media Browser", "icon-picture", false, baseUrl + "mediaBrowser/browse"),

                CreateTreeNode("memberBrowser", parentId, qs, "Member Browser", "icon-umb-members", false, baseUrl + "memberBrowser/browse"),

                CreateTreeNode("tagBrowser", parentId, qs, "Tag Browser", "icon-tags", false, baseUrl + "tagBrowser/browse"),

                CreateTreeNode(ReflectionTree, parentId, qs, "Types", "icon-folder", true, baseUrl + "typesIntro"),

                CreateTreeNode("diagnosticBrowser", parentId, qs, "Diagnostics", "icon-settings", false, baseUrl + "diagnosticBrowser/umbraco"),

                CreateTreeNode("utilityBrowser", parentId, qs, "Utilities", "icon-wrench", false, baseUrl + "utilityBrowser/umbraco")
            };

            return tree;
        }

        private TreeNodeCollection PopulateReflectionTree(string parentId, FormDataCollection qs)
        {
            var tree = new TreeNodeCollection
            {
                CreateTreeNode("reflectionBrowserSurface", parentId, qs, "Surface Controllers", "icon-planet", false, baseUrl + "reflectionBrowser/surface"),

                CreateTreeNode("reflectionBrowserApi", parentId, qs, "API Controllers", "icon-rocket", false, baseUrl + "reflectionBrowser/api"),

                CreateTreeNode("reflectionBrowserRender", parentId, qs, "RenderMvc Controllers", "icon-satellite-dish", false, baseUrl + "reflectionBrowser/render"),

                CreateTreeNode("reflectionBrowserModels", parentId, qs, "Content Models", "icon-binarycode", false, baseUrl + "reflectionBrowser/models"),

                CreateTreeNode("reflectionBrowserComposers", parentId, qs, "Composers", "icon-music", false, baseUrl + "reflectionBrowser/composers"),

                CreateTreeNode("reflectionBrowserConverters", parentId, qs, "Value Converters", "icon-wand", false, baseUrl + "reflectionBrowser/converters"),

                CreateTreeNode("typeBrowser", parentId, qs, "Interface Browser", "icon-molecular-network", false, baseUrl + "typeBrowser/browse")
            };

            return tree;
        }
    }
}