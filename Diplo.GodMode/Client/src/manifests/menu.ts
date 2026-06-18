import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import { GODMODE_TREE_ALIAS } from "../constants";

export const GODMODE_MENU_ALIAS = "Diplo.Menu.GodMode";
export const GODMODE_ROOT_MENU_ITEM_ALIAS = "Diplo.MenuItem.GodMode";
export const GODMODE_SECTION_SIDEBAR_APP_ALIAS = "Diplo.SectionSidebarApp.GodMode";
type GodModeManifest = ManifestBase & Record<string, unknown>;

const UMB_SETTINGS_SECTION_ALIAS = "Umb.Section.Settings";
const UMB_SECTION_ALIAS_CONDITION_ALIAS = "Umb.Condition.SectionAlias";

export const menuManifests: GodModeManifest[] = [
    {
        type: "menu",
        alias: GODMODE_MENU_ALIAS,
        name: "GodMode Menu"
    },
    {
        type: "sectionSidebarApp",
        kind: "menu",
        alias: GODMODE_SECTION_SIDEBAR_APP_ALIAS,
        name: "GodMode Sidebar Menu",
        weight: 50,
        meta: {
            label: "God Mode",
            menu: GODMODE_MENU_ALIAS
        },
        conditions: [
            {
                alias: UMB_SECTION_ALIAS_CONDITION_ALIAS,
                match: UMB_SETTINGS_SECTION_ALIAS
            }
        ]
    },
    {
        type: "menuItem",
        kind: "tree",
        alias: GODMODE_ROOT_MENU_ITEM_ALIAS,
        name: "GodMode Menu Item",
        weight: 1000,
        meta: {
            label: "God Mode",
            icon: "icon-science",
            menus: [GODMODE_MENU_ALIAS],
            treeAlias: GODMODE_TREE_ALIAS,
            hideTreeRoot: true
        }
    }
];
