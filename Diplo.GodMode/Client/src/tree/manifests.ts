import { UmbDefaultTreeItemElement } from "@umbraco-cms/backoffice/tree";
import {
    GODMODE_TREE_ALIAS,
    GODMODE_TREE_FOLDER_ENTITY_TYPE,
    GODMODE_TREE_ITEM_ENTITY_TYPE,
    GODMODE_TREE_REPOSITORY_ALIAS,
    GODMODE_TREE_ROOT_ENTITY_TYPE
} from "../constants";
import { GodModeTreeItemContext } from "./godmode-tree-item.context";

export const treeManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: GODMODE_TREE_REPOSITORY_ALIAS,
        name: "GodMode Tree Repository",
        api: () => import("./godmode-tree.repository")
    },
    {
        type: "tree",
        kind: "default",
        alias: GODMODE_TREE_ALIAS,
        name: "GodMode Tree",
        meta: {
            repositoryAlias: GODMODE_TREE_REPOSITORY_ALIAS
        }
    },
    {
        type: "treeItem",
        alias: "Diplo.TreeItem.GodMode",
        name: "GodMode Tree Item",
        api: GodModeTreeItemContext,
        element: UmbDefaultTreeItemElement,
        forEntityTypes: [
            GODMODE_TREE_ROOT_ENTITY_TYPE,
            GODMODE_TREE_FOLDER_ENTITY_TYPE,
            GODMODE_TREE_ITEM_ENTITY_TYPE
        ]
    }
];
