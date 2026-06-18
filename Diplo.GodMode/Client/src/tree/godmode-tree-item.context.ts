import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbDefaultTreeItemContext } from "@umbraco-cms/backoffice/tree";
import { GODMODE_TREE_ITEM_ENTITY_TYPE } from "../constants";
import type { GodModeTreeItemModel, GodModeTreeRootModel } from "./types";

/**
 * Custom tree-item context that keeps leaves pointing at GodMode's existing
 * standalone workspaces (`workspace/diplo-godmode-<id>`) instead of the
 * default `workspace/<entityType>/edit/<unique>` path. Folders and the root
 * fall through to the default behaviour, which resolves to the routable
 * `godmode-folder` / `godmode-root` collection workspaces.
 */
export class GodModeTreeItemContext extends UmbDefaultTreeItemContext<
    GodModeTreeItemModel,
    GodModeTreeRootModel
> {
    constructor(host: UmbControllerHost) {
        super(host);
    }

    override constructPath(pathname: string, entityType: string, unique: string | null) {
        if (entityType === GODMODE_TREE_ITEM_ENTITY_TYPE) {
            const workspaceEntityType = this.getTreeItem()?.workspaceEntityType;
            if (workspaceEntityType) {
                return `section/${pathname}/workspace/${workspaceEntityType}`;
            }
        }

        return super.constructPath(pathname, entityType, unique);
    }
}

export { GodModeTreeItemContext as api };
