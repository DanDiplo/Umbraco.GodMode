import { GODMODE_AI_ENTITY_TYPE, GODMODE_AI_WORKSPACE_ALIAS, GODMODE_MENU_ALIAS } from "./constants";

const UMB_WORKSPACE_CONDITION_ALIAS = "Umb.Condition.WorkspaceAlias";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "menuItem",
    alias: "Diplo.MenuItem.GodModeAI.SchemaHealth",
    name: "GodMode AI Schema Analysis Menu Item",
    weight: 900,
    element: () => import("./schema-health-menu-item.element"),
    meta: {
      label: "AI Schema Analysis",
      icon: "icon-wand",
      menus: [GODMODE_MENU_ALIAS],
      entityType: GODMODE_AI_ENTITY_TYPE
    }
  },
  {
    type: "workspace",
    alias: GODMODE_AI_WORKSPACE_ALIAS,
    name: "GodMode AI Schema Analysis Workspace",
    element: () => import("./schema-health.element"),
    meta: {
      entityType: GODMODE_AI_ENTITY_TYPE
    }
  },
  {
    type: "workspaceView",
    alias: `${GODMODE_AI_WORKSPACE_ALIAS}.Overview`,
    name: "GodMode AI Schema Analysis Workspace Overview",
    element: () => import("./schema-health.element"),
    weight: 100,
    meta: {
      label: "AI Schema Analysis",
      pathname: "schema-health",
      icon: "icon-wand"
    },
    conditions: [
      {
        alias: UMB_WORKSPACE_CONDITION_ALIAS,
        match: GODMODE_AI_WORKSPACE_ALIAS
      }
    ]
  }
];
