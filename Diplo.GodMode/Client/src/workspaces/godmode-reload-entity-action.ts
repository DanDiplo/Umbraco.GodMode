import { UmbEntityActionBase } from "@umbraco-cms/backoffice/entity-action";
import { GODMODE_BROWSER_WORKSPACE_CONTEXT } from "./godmode-browser-workspace.context";

/**
 * Reload entity action shown in the GodMode browser workspace header menu.
 * Raises the reload signal on the browser workspace context; `godmode-page`
 * turns that into its existing `reload` event so each browser refetches.
 */
export class GodModeReloadEntityAction extends UmbEntityActionBase<never> {
    override async execute() {
        const context = await this.getContext(GODMODE_BROWSER_WORKSPACE_CONTEXT);
        context?.requestReload();
    }
}

export { GodModeReloadEntityAction as api };
