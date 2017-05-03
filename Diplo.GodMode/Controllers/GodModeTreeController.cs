using System;
using System.Net;
using System.Net.Http.Formatting;
using System.Web.Http;
using umbraco.BusinessLogic.Actions;
using Umbraco.Core;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Custom Umbraco tree for GodMode under the Developer section of Umbraco
    /// </summary>
    [Tree(Constants.Applications.Developer, "godModeTree", "God Mode", sortOrder: 10)]
    [PluginController("GodMode")]
    public class GodModeTreeController : TreeController
    {
        /// <summary>
        /// The method called to render the contents of the tree structure
        /// </summary>
        /// <param name="id">The parent Id</param>
        /// <param name="queryStrings">All of the query string parameters passed from jsTree</param>
        /// <returns></returns>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        /// <remarks>
        /// We are allowing an arbitrary number of query strings to be pased in so that developers are able to persist custom data from the front-end
        /// to the back end to be used in the query for model data.
        /// </remarks>
        protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
        {
            if (id != Constants.System.Root.ToInvariantString())
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return PopulateTreeNodes(id, queryStrings);
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
                menu.Items.Add<RefreshNode, ActionRefresh>("Reload", true);
            }

            return menu;
        }

        private TreeNodeCollection PopulateTreeNodes(string parentId, FormDataCollection qs)
        {
            const string baseUrl = "developer/godModeTree/";

            TreeNodeCollection tree = new TreeNodeCollection();

            tree.Add(CreateTreeNode("docTypeBrowser", parentId, qs, "DocType Browser", "icon-item-arrangement", false, baseUrl + "docTypeBrowser/browse"));

            tree.Add(CreateTreeNode("templateBrowser", parentId, qs, "Template Browser", "icon-newspaper-alt", false, baseUrl + "templateBrowser/browse"));

            tree.Add(CreateTreeNode("partialBrowser", parentId, qs, "Partial Browser", "icon-article", false, baseUrl + "partialBrowser/browse"));

            tree.Add(CreateTreeNode("dataTypeBrowser", parentId, qs, "DataType Browser", "icon-autofill", false, baseUrl + "dataTypeBrowser/browse"));

            tree.Add(CreateTreeNode("contentBrowser", parentId, qs, "Content Browser", "icon-umb-content", false, baseUrl + "contentBrowser/browse"));

            tree.Add(CreateTreeNode("mediaBrowser", parentId, qs, "Media Browser", "icon-picture", false, baseUrl + "mediaBrowser/browse"));

            tree.Add(CreateTreeNode("reflectionBrowser", parentId, qs, "Surface Controllers", "icon-planet", false, baseUrl + "reflectionBrowser/surface"));

            tree.Add(CreateTreeNode("reflectionBrowser", parentId, qs, "API Controllers", "icon-rocket", false, baseUrl + "reflectionBrowser/api"));

            tree.Add(CreateTreeNode("reflectionBrowser", parentId, qs, "RenderMvc Controllers", "icon-satellite-dish", false, baseUrl + "reflectionBrowser/render"));

            tree.Add(CreateTreeNode("reflectionBrowser", parentId, qs, "Content Models", "icon-binarycode", false, baseUrl + "reflectionBrowser/models"));

            tree.Add(CreateTreeNode("converterBrowser", parentId, qs, "Value Converters", "icon-wand", false, baseUrl + "reflectionBrowser/converters"));

            tree.Add(CreateTreeNode("typeBrowser", parentId, qs, "Interface Browser", "icon-molecular-network", false, baseUrl + "typeBrowser/browse"));

            tree.Add(CreateTreeNode("diagnosticBrowser", parentId, qs, "Diagnostics", "icon-settings", false, baseUrl + "diagnosticBrowser/umbraco"));

            tree.Add(CreateTreeNode("utilityBrowser", parentId, qs, "Utilities", "icon-wrench", false, baseUrl + "utilityBrowser/umbraco"));

            return tree;
        }
    }
}