import { customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

/**
 * Route component for the GodMode collection workspaces. A thin wrapper around
 * <umb-workspace-editor> so the workspace renders with proper chrome and hosts
 * the collection workspace view. The headline is set by the workspace context
 * per route (folder name, or "God Mode" at the root).
 */
@customElement("godmode-collection-workspace")
export class GodModeCollectionWorkspaceElement extends UmbLitElement {
    @property({ type: String, attribute: false })
    headline = "";

    override render() {
        return html`<umb-workspace-editor .headline=${this.headline}></umb-workspace-editor>`;
    }
}

export default GodModeCollectionWorkspaceElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-collection-workspace": GodModeCollectionWorkspaceElement;
    }
}
