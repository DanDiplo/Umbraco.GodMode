using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Trees;

namespace Diplo.MediaDownload
{
    /// <summary>
    /// Extends the datatype tree by adding the Copy link to the Actions menu
    /// </summary>
    public class DataTypeTreeComponent : IComponent
    {
        public void Initialize()
        {
            TreeControllerBase.MenuRendering += TreeControllerBase_MenuRendering;
        }

        public void Terminate()
        {
            TreeControllerBase.MenuRendering -= TreeControllerBase_MenuRendering;
        }

        /// <summary>
        /// Called when the tree controllers are rendering
        /// </summary>
        /// <param name="sender">The base tree controller</param>
        /// <param name="e">The tree rendering event</param>
        private void TreeControllerBase_MenuRendering(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            if (sender.TreeAlias == Constants.Trees.DataTypes)
            {
                var menuItem = new MenuItem("copy", "Copy");
                menuItem.LaunchDialogView("/App_Plugins/DiploGodMode/backoffice/actions/CopyDataType.html", "Copy");
                menuItem.Icon = "documents";

                e.Menu.Items.Add(menuItem);
            }
        }
    }

    /// <summary>
    /// Adds our <see cref="DataTypeTreeComponent"/> to the Umbraco components so it's discovered
    /// </summary>
    public class MediaTreeComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<DataTypeTreeComponent>();
        }
    }
}
