import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    UmbTreeServerDataSourceBase,
    type UmbTreeChildrenOfRequestArgs,
    type UmbTreeRootItemsRequestArgs
} from "@umbraco-cms/backoffice/tree";
import type { UmbDataSourceResponse, UmbTargetPagedModel } from "@umbraco-cms/backoffice/repository";
import {
    GODMODE_TREE_FOLDER_ENTITY_TYPE,
    GODMODE_TREE_ROOT_ENTITY_TYPE
} from "../constants";
import { getChildNodes, getRootNodes, type GodModeTreeNode } from "./structure";
import type { GodModeTreeItemModel } from "./types";

/**
 * Client-side tree data source. The GodMode "tree" is a static catalogue, so
 * the root/children functions just return the in-memory {@link structure}
 * rather than calling the server. Mirrors the shape Umbraco's server-backed
 * trees use so the default tree/treeItem kinds work unchanged.
 */
export class GodModeTreeServerDataSource extends UmbTreeServerDataSourceBase<
    GodModeTreeNode,
    GodModeTreeItemModel
> {
    constructor(host: UmbControllerHost) {
        super(host, { getRootItems, getChildrenOf, getAncestorsOf, mapper });
    }
}

const getRootItems = async (
    _args: UmbTreeRootItemsRequestArgs
): Promise<UmbDataSourceResponse<UmbTargetPagedModel<GodModeTreeNode>>> => {
    const items = getRootNodes();
    return { data: { total: items.length, items } };
};

const getChildrenOf = async (
    args: UmbTreeChildrenOfRequestArgs
): Promise<UmbDataSourceResponse<UmbTargetPagedModel<GodModeTreeNode>>> => {
    if (args.parent.unique === null) {
        return getRootItems(args);
    }
    const items = getChildNodes(args.parent.unique);
    return { data: { total: items.length, items } };
};

const getAncestorsOf = async () => {
    throw new Error("Ancestors are not supported by the GodMode tree.");
};

const mapper = (item: GodModeTreeNode): GodModeTreeItemModel => ({
    unique: item.unique,
    parent: {
        unique: item.parentUnique,
        entityType: item.parentUnique === null ? GODMODE_TREE_ROOT_ENTITY_TYPE : GODMODE_TREE_FOLDER_ENTITY_TYPE
    },
    name: item.name,
    entityType: item.entityType,
    isFolder: item.isFolder,
    icon: item.icon,
    hasChildren: item.hasChildren,
    workspaceEntityType: item.workspaceEntityType
});
