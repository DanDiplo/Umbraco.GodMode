import {
    GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS,
    GODMODE_TREE_ITEM_CHILDREN_COLLECTION_REPOSITORY_ALIAS
} from "../constants";

const UMB_COLLECTION_ALIAS_CONDITION = "Umb.Condition.CollectionAlias";

export const collectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "repository",
        alias: GODMODE_TREE_ITEM_CHILDREN_COLLECTION_REPOSITORY_ALIAS,
        name: "GodMode Tree Item Children Collection Repository",
        api: () => import("./godmode-tree-item-children-collection.repository")
    },
    {
        type: "collection",
        kind: "default",
        alias: GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS,
        name: "GodMode Tree Item Children Collection",
        // Custom element (no toolbar) — overrides the default kind's element.
        element: () => import("./godmode-collection.element"),
        meta: {
            repositoryAlias: GODMODE_TREE_ITEM_CHILDREN_COLLECTION_REPOSITORY_ALIAS
        }
    },
    {
        type: "collectionView",
        alias: "Diplo.CollectionView.GodMode.Table",
        name: "GodMode Tree Item Table Collection View",
        element: () => import("./godmode-tree-item-table-collection-view.element"),
        weight: 300,
        meta: {
            label: "Table",
            icon: "icon-table",
            pathName: "table"
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS
            }
        ]
    }
];
