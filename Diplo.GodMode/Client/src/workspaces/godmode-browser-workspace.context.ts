import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbContextToken, UmbContextProviderController } from "@umbraco-cms/backoffice/context-api";
import { UmbNumberState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UMB_ENTITY_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";

/**
 * Lightweight workspace context for the GodMode browser pages. It satisfies
 * UMB_ENTITY_WORKSPACE_CONTEXT (entityType + a fixed unique) so the native
 * workspace header action menu (`umb-workspace-entity-action-menu`) renders,
 * and exposes a reload signal that the Reload entity action raises and the
 * page (`godmode-page`) listens for. Reading entityType/alias from the manifest
 * lets a single class serve every browser workspace.
 */
export class GodModeBrowserWorkspaceContext extends UmbContextBase {
    public workspaceAlias = "";

    #entityType?: string;

    #unique = new UmbStringState<string | null>(null);
    public readonly unique = this.#unique.asObservable();

    #reload = new UmbNumberState(0);
    public readonly reload = this.#reload.asObservable();

    // Provides UMB_ENTITY_CONTEXT so the header action list can resolve entity
    // actions by forEntityTypes (separate from UMB_ENTITY_WORKSPACE_CONTEXT,
    // which only drives whether the action menu button renders).
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UMB_ENTITY_WORKSPACE_CONTEXT.toString());
        new UmbContextProviderController(this, GODMODE_BROWSER_WORKSPACE_CONTEXT, this);
    }

    set manifest(manifest: { alias: string; meta?: { entityType?: string } }) {
        this.workspaceAlias = manifest.alias;
        this.#entityType = manifest.meta?.entityType;
        // The unique is irrelevant for these singletons, but must be defined for
        // the header action menu to render.
        const unique = manifest.meta?.entityType ?? "godmode";
        this.#unique.setValue(unique);
        this.#entityContext.setEntityType(this.#entityType);
        this.#entityContext.setUnique(unique);
    }

    getEntityType() {
        return this.#entityType ?? "";
    }

    getUnique() {
        return this.#unique.getValue();
    }

    requestReload() {
        this.#reload.setValue(this.#reload.getValue() + 1);
    }
}

export const GODMODE_BROWSER_WORKSPACE_CONTEXT = new UmbContextToken<GodModeBrowserWorkspaceContext>(
    "GodModeBrowserWorkspaceContext"
);

export { GodModeBrowserWorkspaceContext as api };
