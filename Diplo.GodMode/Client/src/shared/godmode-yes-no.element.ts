import { LitElement, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";

/** Replaces the legacy `<godmode-true-false>` directive — renders ✓ Yes / ✗ No. */
@customElement("godmode-yes-no")
export class GodModeYesNoElement extends LitElement {
    @property({ type: Boolean }) value = false;

    override render() {
        return this.value
            ? html`<span><uui-icon name="icon-check"></uui-icon> Yes</span>`
            : html`<span><uui-icon name="icon-block"></uui-icon> No</span>`;
    }
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-yes-no": GodModeYesNoElement;
    }
}
