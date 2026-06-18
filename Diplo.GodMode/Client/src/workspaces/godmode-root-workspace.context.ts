import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { GODMODE_ROOT_WORKSPACE_ALIAS, GODMODE_TREE_ROOT_ENTITY_TYPE } from "../constants";
import { GodModeCollectionWorkspaceContextBase } from "./godmode-collection-workspace.context";

export class GodModeRootWorkspaceContext extends GodModeCollectionWorkspaceContextBase {
    constructor(host: UmbControllerHost) {
        super(host, {
            workspaceAlias: GODMODE_ROOT_WORKSPACE_ALIAS,
            entityType: GODMODE_TREE_ROOT_ENTITY_TYPE,
            headlineFor: () => "God Mode"
        });
    }
}

export { GodModeRootWorkspaceContext as api };
