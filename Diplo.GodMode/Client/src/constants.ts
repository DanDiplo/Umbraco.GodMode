export const GODMODE_PACKAGE_ALIAS = "Diplo.GodMode";

// Settings menu items (alias is used as the entityType for the workspace match)
export const GODMODE_ENTITY_TYPE_PREFIX = "diplo-godmode";

export const GODMODE_API_BASE = "/umbraco/management/api/v1/godmode";

// Tree entity types. Leaves share a single entityType; the custom tree-item
// context maps each leaf to its existing standalone browser workspace.
export const GODMODE_TREE_ROOT_ENTITY_TYPE = "godmode-root";
export const GODMODE_TREE_FOLDER_ENTITY_TYPE = "godmode-folder";
export const GODMODE_TREE_ITEM_ENTITY_TYPE = "godmode";

// Tree + collection extension aliases.
export const GODMODE_TREE_ALIAS = "Diplo.Tree.GodMode";
export const GODMODE_TREE_REPOSITORY_ALIAS = "Diplo.Repository.GodMode.Tree";
export const GODMODE_TREE_ITEM_CHILDREN_COLLECTION_ALIAS = "Diplo.Collection.GodMode.TreeItemChildren";
export const GODMODE_TREE_ITEM_CHILDREN_COLLECTION_REPOSITORY_ALIAS =
    "Diplo.Repository.GodMode.TreeItemChildren.Collection";

// Workspaces that host the tree-item-children collection view (root + folders).
export const GODMODE_ROOT_WORKSPACE_ALIAS = "Diplo.Workspace.GodMode.Root";
export const GODMODE_FOLDER_WORKSPACE_ALIAS = "Diplo.Workspace.GodMode.Folder";

// Editor URLs in the v17 backoffice, used by browsers when offering a "edit" link.
// These match what the v17 router uses for built-in entities.
export const EDIT_URLS = {
    template: "section/settings/workspace/template/edit/",
    dataType: "section/settings/workspace/data-type/edit/",
    documentType: "section/settings/workspace/document-type/edit/",
    partial: "section/settings/workspace/partial-view/edit/",
    media: "section/media/workspace/media/edit/",
    content: "section/content/workspace/document/edit/",
    member: "section/member-management/workspace/member/edit/"
} as const;
