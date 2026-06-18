import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";

const UMB_WORKSPACE_CONDITION_ALIAS = "Umb.Condition.WorkspaceAlias";
type GodModeManifest = ManifestBase & Record<string, unknown>;

/**
 * One browser = a menu item that opens a workspace.
 * The workspace lazy-loads a Lit element implementing the page.
 */
export interface BrowserDef {
    /** Alias suffix — combined with the GodMode prefix for menu/workspace aliases */
    id: string;
    /** Label shown in the sidebar */
    label: string;
    /** Short description shown on the welcome page */
    description?: string;
    /** Umbraco icon alias */
    icon: string;
    /** Sidebar weight (higher = closer to the top) */
    weight: number;
    /** Lazy importer for the Lit element that renders the page */
    element: () => Promise<unknown>;
    /**
     * Marks a page as a child of the "Types" folder rather than a top-level
     * tree node. The GodMode tree groups these under the Types folder; the
     * workspace + workspaceView are still emitted so /workspace/<id> routes.
     */
    skipMenuItem?: boolean;
}

/**
 * Build manifests for one browser: a menuItem + a workspace that hosts the
 * Lit element. Each browser also gets its own entityType so the workspace
 * router knows which view to mount when its menu item is clicked.
 */
export function browserManifests(b: BrowserDef): GodModeManifest[] {
    const entityType = `${GODMODE_ENTITY_TYPE_PREFIX}-${b.id}`;
    const workspaceAlias = `Diplo.Workspace.GodMode.${b.id}`;

    const manifests: GodModeManifest[] = [];

    manifests.push(
        {
            type: "workspace",
            alias: workspaceAlias,
            name: `GodMode ${b.label} Workspace`,
            element: b.element,
            // Provides UMB_ENTITY_WORKSPACE_CONTEXT (so the header action menu
            // renders) plus the reload signal the Reload entity action raises.
            api: () => import("../workspaces/godmode-browser-workspace.context"),
            meta: { entityType }
        },
        // Default landing view for this workspace
        {
            type: "workspaceView",
            alias: `${workspaceAlias}.Overview`,
            name: `GodMode ${b.label} Workspace Overview`,
            element: b.element,
            weight: 100,
            meta: {
                label: b.label,
                pathname: b.id,
                icon: b.icon
            },
            conditions: [
                {
                    alias: UMB_WORKSPACE_CONDITION_ALIAS,
                    match: workspaceAlias
                }
            ]
        }
    );

    return manifests;
}
