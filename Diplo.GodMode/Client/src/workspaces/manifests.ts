import {
    GODMODE_FOLDER_WORKSPACE_ALIAS,
    GODMODE_ROOT_WORKSPACE_ALIAS,
    GODMODE_TREE_FOLDER_ENTITY_TYPE,
    GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS,
    GODMODE_TREE_ROOT_ENTITY_TYPE
} from "../constants";

const UMB_WORKSPACE_CONDITION_ALIAS = "Umb.Condition.WorkspaceAlias";

export const workspaceCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: GODMODE_ROOT_WORKSPACE_ALIAS,
        name: "GodMode Root Workspace",
        api: () => import("./godmode-root-workspace.context"),
        meta: {
            entityType: GODMODE_TREE_ROOT_ENTITY_TYPE
        }
    },
    {
        type: "workspace",
        kind: "routable",
        alias: GODMODE_FOLDER_WORKSPACE_ALIAS,
        name: "GodMode Folder Workspace",
        api: () => import("./godmode-folder-workspace.context"),
        meta: {
            entityType: GODMODE_TREE_FOLDER_ENTITY_TYPE
        }
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "Diplo.WorkspaceView.GodMode.Collection",
        name: "GodMode Collection Workspace View",
        meta: {
            label: "Items",
            pathname: "items",
            icon: "icon-folder",
            collectionAlias: GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                oneOf: [GODMODE_ROOT_WORKSPACE_ALIAS, GODMODE_FOLDER_WORKSPACE_ALIAS]
            }
        ]
    }
];
