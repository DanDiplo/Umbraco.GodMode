import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { GODMODE_FOLDER_WORKSPACE_ALIAS, GODMODE_TREE_FOLDER_ENTITY_TYPE } from "../constants";
import { getFolderName } from "../tree/structure";
import { GodModeCollectionWorkspaceContextBase } from "./godmode-collection-workspace.context";

export class GodModeFolderWorkspaceContext extends GodModeCollectionWorkspaceContextBase {
    constructor(host: UmbControllerHost) {
        super(host, {
            workspaceAlias: GODMODE_FOLDER_WORKSPACE_ALIAS,
            entityType: GODMODE_TREE_FOLDER_ENTITY_TYPE,
            headlineFor: (unique) => (unique ? getFolderName(unique) ?? "" : "")
        });
    }
}

export { GodModeFolderWorkspaceContext as api };
