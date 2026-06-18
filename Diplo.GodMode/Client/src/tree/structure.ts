import { browsers } from "../manifests/catalog";
import type { BrowserDef } from "../manifests/browsers";
import {
    GODMODE_ENTITY_TYPE_PREFIX,
    GODMODE_TREE_FOLDER_ENTITY_TYPE,
    GODMODE_TREE_ITEM_ENTITY_TYPE
} from "../constants";

/**
 * A single node in the GodMode tree. Folders group browsers; leaves point at
 * an existing standalone browser workspace via {@link workspaceEntityType}.
 *
 * The grouping below is the single source of truth for the sidebar tree and
 * the root/folder collection views. It mirrors the categories the old custom
 * root menu item rendered by hand.
 */
export interface GodModeTreeNode {
    unique: string;
    name: string;
    icon?: string;
    entityType: string;
    isFolder: boolean;
    hasChildren: boolean;
    /** Parent unique — null at the root level. */
    parentUnique: string | null;
    /** Leaves only: the `diplo-godmode-<id>` workspace entityType to open. */
    workspaceEntityType?: string;
}

/** Unique of the nested "Types" folder (sits inside "Developer & runtime"). */
const TYPES_FOLDER_UNIQUE = "types";

interface GroupDef {
    unique: string;
    name: string;
    /** Browser ids, or {@link TYPES_FOLDER_UNIQUE} for the nested Types folder. */
    ids: string[];
}

const GROUPS: GroupDef[] = [
    {
        unique: "group-health",
        name: "Health & operations",
        ids: ["healthRisk", "diagnosticBrowser", "logBrowser", "databaseBrowser"]
    },
    {
        unique: "group-content",
        name: "Content & schema",
        ids: [
            "contentBrowser",
            "docTypeBrowser",
            "dataTypeBrowser",
            "templateBrowser",
            "partialBrowser",
            "mediaBrowser",
            "memberBrowser",
            "tagBrowser"
        ]
    },
    { unique: "group-relationships", name: "Relationships & usage", ids: ["referenceGraph", "usageBrowser"] },
    {
        unique: "group-developer",
        name: "Developer & runtime",
        ids: [TYPES_FOLDER_UNIQUE, "serviceBrowser", "extensionExplorer", "nugetPackages"]
    },
    { unique: "group-data", name: "Data & actions", ids: ["keyValueBrowser", "utilityBrowser"] }
];

const byId = new Map<string, BrowserDef>(browsers.map((b) => [b.id, b]));

function leafNode(id: string, parentUnique: string | null): GodModeTreeNode | undefined {
    const browser = byId.get(id);
    if (!browser) return undefined;
    return {
        unique: browser.id,
        name: browser.label,
        icon: browser.icon,
        entityType: GODMODE_TREE_ITEM_ENTITY_TYPE,
        isFolder: false,
        hasChildren: false,
        parentUnique,
        workspaceEntityType: `${GODMODE_ENTITY_TYPE_PREFIX}-${browser.id}`
    };
}

function folderNode(unique: string, name: string, parentUnique: string | null): GodModeTreeNode {
    return {
        unique,
        name,
        icon: "icon-folder",
        entityType: GODMODE_TREE_FOLDER_ENTITY_TYPE,
        isFolder: true,
        hasChildren: true,
        parentUnique
    };
}

const isNode = (node: GodModeTreeNode | undefined): node is GodModeTreeNode => !!node;

// Reflection / "Types" children — the browsers flagged skipMenuItem, heaviest first.
const TYPE_CHILD_NODES: GodModeTreeNode[] = browsers
    .filter((browser) => browser.skipMenuItem)
    .sort((a, b) => b.weight - a.weight)
    .map((browser) => leafNode(browser.id, TYPES_FOLDER_UNIQUE))
    .filter(isNode);

// "Information" is promoted to a top-level leaf labelled "Overview" (no folder).
const OVERVIEW_NODE = leafNode("informationBrowser", null);
if (OVERVIEW_NODE) {
    OVERVIEW_NODE.name = "Overview";
}

const ROOT_NODES: GodModeTreeNode[] = [
    ...(OVERVIEW_NODE ? [OVERVIEW_NODE] : []),
    ...GROUPS.map((group) => folderNode(group.unique, group.name, null))
];

const childrenByParent = new Map<string, GodModeTreeNode[]>();
for (const group of GROUPS) {
    const children = group.ids
        .map((id) =>
            id === TYPES_FOLDER_UNIQUE
                ? folderNode(TYPES_FOLDER_UNIQUE, "Types", group.unique)
                : leafNode(id, group.unique)
        )
        .filter(isNode);
    childrenByParent.set(group.unique, children);
}
childrenByParent.set(TYPES_FOLDER_UNIQUE, TYPE_CHILD_NODES);

const folderNamesByUnique = new Map<string, string>(ROOT_NODES.map((node) => [node.unique, node.name]));
folderNamesByUnique.set(TYPES_FOLDER_UNIQUE, "Types");

/** Display name for a folder unique, used as the collection workspace headline. */
export function getFolderName(unique: string): string | undefined {
    return folderNamesByUnique.get(unique);
}

/** Top-level tree nodes (the six category folders). */
export function getRootNodes(): GodModeTreeNode[] {
    return ROOT_NODES;
}

/** Children of a folder node, keyed by the folder's unique. */
export function getChildNodes(parentUnique: string): GodModeTreeNode[] {
    return childrenByParent.get(parentUnique) ?? [];
}
