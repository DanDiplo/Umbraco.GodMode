using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Diplo.GodMode.Menus
{
    /// <summary>
    /// Used to add a menu item when the tree renders
    /// </summary>
    public class DataTypeTreeNotificationHandler : INotificationHandler<MenuRenderingNotification>
    {
        /// <summary>
        /// Adds a copy action to the DataTypes tree when rendering
        /// </summary>
        /// <param name="notification">The notification details</param>
        public void Handle(MenuRenderingNotification notification)
        {
            if (notification.TreeAlias.Equals(Constants.Trees.DataTypes))
            {
                if (notification.NodeId != "-1")
                {
                    var menuItem = new Umbraco.Cms.Core.Models.Trees.MenuItem("copy", "Copy");
                    menuItem.AdditionalData.Add("actionView", "/App_Plugins/DiploGodMode/backoffice/actions/CopyDataType.html");
                    menuItem.Icon = "documents";
                    notification.Menu.Items.Add(menuItem);
                }
            }
        }
    }
}
