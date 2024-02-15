using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Trees;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Custom Umbraco tree for GodMode under the Developer section of Umbraco
    /// </summary>
    [Tree(Constants.Applications.Settings, treeAlias: GodModeSettings.TreeAlias, TreeTitle = "God Mode", TreeGroup = Constants.Trees.Groups.ThirdParty, SortOrder = 12)]
    [PluginController(GodModeSettings.PluginAreaName)]
    public class GodModeTreeController : TreeController
    {
        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;
        private readonly IOptions<GodModeConfig> godModeConfig;
        private static readonly string baseUrl = $"{Constants.Applications.Settings}/{GodModeSettings.TreeAlias}/";

        public const string ReflectionTree = "reflectionTree";


        public GodModeTreeController(ILocalizedTextService localizedTextService, UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection, IMenuItemCollectionFactory menuItemCollectionFactory, IEventAggregator eventAggregator, IOptions<GodModeConfig> godModeConfig) : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            this._menuItemCollectionFactory = menuItemCollectionFactory;
            this.godModeConfig = godModeConfig;
        }

        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            if (id != Constants.System.RootString && id != ReflectionTree)
            {
                throw new InvalidOperationException("Id not Root or Reflection Tree");
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

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            var menu = this._menuItemCollectionFactory.Create();

            if (id == Constants.System.Root.ToInvariantString())
            {
                menu.Items.Add(new RefreshNode(this.LocalizedTextService, true));
            }

            return menu;
        }

        /// <summary>
        /// Enables the root node to have it's own view
        /// </summary>
        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var rootResult = base.CreateRootNode(queryStrings);

            if (rootResult.Result is not null)
            {
                return rootResult;
            }

            var root = rootResult.Value;

            root.RoutePath = string.Format("{0}/{1}/{2}", Constants.Applications.Settings, GodModeSettings.TreeAlias, "intro");
            root.Icon = "icon-sience"; // yes, it's really spelt like that!
            root.HasChildren = true;
            root.MenuUrl = null;

            return root;
        }

        private TreeNodeCollection PopulateTreeNodes(string parentId, FormCollection qs)
        {
            // path is PluginController name + area name + template name eg. /App_Plugins/DiploGodMode/GodModeTree/
            // The first part of the name eg. docTypeBrowser is the Id - this is used by Angular navigationService to identify the node

            var tree = new TreeNodeCollection();

            TryAndCreateTreeNode(tree, "docTypeBrowser", parentId, qs, "DocType Browser", "icon-item-arrangement", false, baseUrl + "docTypeBrowser/browse");

            TryAndCreateTreeNode(tree, "templateBrowser", parentId, qs, "Template Browser", "icon-newspaper-alt", false, baseUrl + "templateBrowser/browse");

            TryAndCreateTreeNode(tree, "partialBrowser", parentId, qs, "Partial Browser", "icon-article", false, baseUrl + "partialBrowser/browse");

            TryAndCreateTreeNode(tree, "dataTypeBrowser", parentId, qs, "DataType Browser", "icon-autofill", false, baseUrl + "dataTypeBrowser/browse");

            TryAndCreateTreeNode(tree, "contentBrowser", parentId, qs, "Content Browser", "icon-umb-content", false, baseUrl + "contentBrowser/browse");

            TryAndCreateTreeNode(tree, "usageBrowser", parentId, qs, "Usage Browser", "icon-chart-curve", false, baseUrl + "usageBrowser/browse");

            TryAndCreateTreeNode(tree, "mediaBrowser", parentId, qs, "Media Browser", "icon-picture", false, baseUrl + "mediaBrowser/browse");

            TryAndCreateTreeNode(tree, "memberBrowser", parentId, qs, "Member Browser", "icon-umb-members", false, baseUrl + "memberBrowser/browse");

            TryAndCreateTreeNode(tree, "tagBrowser", parentId, qs, "Tag Browser", "icon-tags", false, baseUrl + "tagBrowser/browse");

            TryAndCreateTreeNode(tree, ReflectionTree, parentId, qs, "Types", "icon-folder", true, baseUrl + "typesIntro");

            TryAndCreateTreeNode(tree, "serviceBrowser", parentId, qs, "Services", "icon-console", false, baseUrl + "serviceBrowser/browse");

            TryAndCreateTreeNode(tree, "diagnosticBrowser", parentId, qs, "Diagnostics", "icon-settings", false, baseUrl + "diagnosticBrowser/umbraco");

            TryAndCreateTreeNode(tree, "utilityBrowser", parentId, qs, "Utilities", "icon-wrench", false, baseUrl + "utilityBrowser/umbraco");

            return tree;
        }

        private TreeNodeCollection PopulateReflectionTree(string parentId, FormCollection qs)
        {
            var tree = new TreeNodeCollection()
            {
                CreateTreeNode("reflectionBrowserSurface", parentId, qs, "Surface Controllers", "icon-planet", false, baseUrl + "reflectionBrowser/surface"),

                CreateTreeNode("reflectionBrowserApi", parentId, qs, "API Controllers", "icon-rocket", false, baseUrl + "reflectionBrowser/api"),

                CreateTreeNode("reflectionBrowserRender", parentId, qs, "Render Controllers", "icon-satellite-dish", false, baseUrl + "reflectionBrowser/render"),

                CreateTreeNode("reflectionBrowserModels", parentId, qs, "Content Models", "icon-binarycode", false, baseUrl + "reflectionBrowser/models"),

                CreateTreeNode("reflectionBrowserComposers", parentId, qs, "Composers", "icon-music", false, baseUrl + "reflectionBrowser/composers"),

                CreateTreeNode("reflectionBrowserConverters", parentId, qs, "Value Converters", "icon-wand", false, baseUrl + "reflectionBrowser/converters"),

                CreateTreeNode("reflectionBrowserViewComponents", parentId, qs, "View Components", "icon-code", false, baseUrl + "reflectionBrowser/components"),

                CreateTreeNode("reflectionBrowserTagHelpers", parentId, qs, "Tag Helpers", "icon-tags", false, baseUrl + "reflectionBrowser/taghelpers"),

                CreateTreeNode("reflectionBrowserContentFinders", parentId, qs, "Content Finders", "icon-directions-alt", false, baseUrl + "reflectionBrowser/finders"),

                CreateTreeNode("reflectionBrowserUrlProviders", parentId, qs, "URL Providers", "icon-link", false, baseUrl + "reflectionBrowser/urlproviders"),

                CreateTreeNode("typeBrowser", parentId, qs, "Interface Browser", "icon-molecular-network", false, baseUrl + "typeBrowser/browse")
            };

            return tree;
        }

        private void TryAndCreateTreeNode(TreeNodeCollection tree, string id, string parentId, FormCollection qs, string title, string icon, bool hasChildren, string routePath)
        {
            if (godModeConfig.Value.FeaturesToHide.InvariantContains(id) || godModeConfig.Value.FeaturesToHide.InvariantContains(title))
            {
                return;
            }

            tree.Add(CreateTreeNode(id, parentId, qs, title, icon, hasChildren, routePath));
        }
    }
}