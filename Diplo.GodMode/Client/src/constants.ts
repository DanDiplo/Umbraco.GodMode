export const GODMODE_PACKAGE_ALIAS = "Diplo.GodMode";

// Settings menu items (alias is used as the entityType for the workspace match)
export const GODMODE_ENTITY_TYPE_PREFIX = "diplo-godmode";

export const GODMODE_API_BASE = "/umbraco/management/api/v1/godmode";

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
