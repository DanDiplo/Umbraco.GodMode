import {
  GODMODE_AI_FIX_PLAN_ENTITY_TYPE,
  GODMODE_AI_FIX_PLAN_WORKSPACE_ALIAS,
  GODMODE_AI_SCHEMA_ENTITY_TYPE,
  GODMODE_AI_SCHEMA_WORKSPACE_ALIAS,
  GODMODE_MENU_ALIAS
} from "./constants";
import { GODMODE_AI_EXPLAIN_MODAL_ALIAS } from "./explain-modal";

const UMB_WORKSPACE_CONDITION_ALIAS = "Umb.Condition.WorkspaceAlias";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "modal",
    alias: GODMODE_AI_EXPLAIN_MODAL_ALIAS,
    name: "GodMode AI Explain Modal",
    element: () => import("./explain-modal.element")
  },
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
      entityType: GODMODE_AI_SCHEMA_ENTITY_TYPE
    }
  },
  {
    type: "menuItem",
    alias: "Diplo.MenuItem.GodModeAI.FixPlan",
    name: "GodMode AI Fix Plan Menu Item",
    weight: 901,
    element: () => import("./schema-health-menu-item.element"),
    meta: {
      label: "AI Fix Plan",
      icon: "icon-ordered-list",
      menus: [GODMODE_MENU_ALIAS],
      entityType: GODMODE_AI_FIX_PLAN_ENTITY_TYPE
    }
  },
  {
    type: "workspace",
    alias: GODMODE_AI_SCHEMA_WORKSPACE_ALIAS,
    name: "GodMode AI Schema Analysis Workspace",
    element: () => import("./schema-health.element"),
    meta: {
      entityType: GODMODE_AI_SCHEMA_ENTITY_TYPE
    }
  },
  {
    type: "workspace",
    alias: GODMODE_AI_FIX_PLAN_WORKSPACE_ALIAS,
    name: "GodMode AI Fix Plan Workspace",
    element: () => import("./fix-plan.element"),
    meta: {
      entityType: GODMODE_AI_FIX_PLAN_ENTITY_TYPE
    }
  },
  {
    type: "workspaceView",
    alias: `${GODMODE_AI_SCHEMA_WORKSPACE_ALIAS}.Overview`,
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
        match: GODMODE_AI_SCHEMA_WORKSPACE_ALIAS
      }
    ]
  },
  {
    type: "workspaceView",
    alias: `${GODMODE_AI_FIX_PLAN_WORKSPACE_ALIAS}.Overview`,
    name: "GodMode AI Fix Plan Workspace Overview",
    element: () => import("./fix-plan.element"),
    weight: 100,
    meta: {
      label: "AI Fix Plan",
      pathname: "fix-plan",
      icon: "icon-ordered-list"
    },
    conditions: [
      {
        alias: UMB_WORKSPACE_CONDITION_ALIAS,
        match: GODMODE_AI_FIX_PLAN_WORKSPACE_ALIAS
      }
    ]
  }
];
