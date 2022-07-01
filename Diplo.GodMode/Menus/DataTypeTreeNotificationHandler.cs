using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Diplo.GodMode.Menus
{
    /// <summary>
    /// Used to add a menu item when the tree renders
    /// </summary>
    public class DataTypeTreeNotificationHandler : INotificationHandler<MenuRenderingNotification>
    {
        private readonly IEntityService entityService;

        public DataTypeTreeNotificationHandler(IEntityService entityService)
        {
            this.entityService = entityService;
        }

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
                    if (int.TryParse(notification.NodeId, out int nodeId))
                    {
                        var container = entityService.Get(nodeId); // gets the container type

                        if (container != null && container.NodeObjectType != UmbracoObjectTypes.DataTypeContainer.GetGuid()) // checks item isn't a countainer (folder)
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
    }
}
