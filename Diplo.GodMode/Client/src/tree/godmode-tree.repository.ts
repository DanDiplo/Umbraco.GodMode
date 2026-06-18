import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbTreeRepositoryBase } from "@umbraco-cms/backoffice/tree";
import { GODMODE_TREE_ROOT_ENTITY_TYPE } from "../constants";
import { GodModeTreeServerDataSource } from "./godmode-tree.data-source";
import type { GodModeTreeItemModel, GodModeTreeRootModel } from "./types";

export class GodModeTreeRepository extends UmbTreeRepositoryBase<GodModeTreeItemModel, GodModeTreeRootModel> {
    constructor(host: UmbControllerHost) {
        super(host, GodModeTreeServerDataSource);
    }

    async requestTreeRoot() {
        const data: GodModeTreeRootModel = {
            unique: null,
            entityType: GODMODE_TREE_ROOT_ENTITY_TYPE,
            name: "God Mode",
            icon: "icon-science",
            hasChildren: true,
            isFolder: true
        };

        return { data };
    }
}

export { GodModeTreeRepository as api };
