import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";

@customElement("godmode-modal-layout")
export class GodModeModalLayoutElement extends LitElement {
    @property({ type: String }) headline = "";
    @property({ type: String }) width = "min(980px, 92vw)";
    @property({ type: String, attribute: "max-height" }) maxHeight = "82vh";

    private _close() {
        this.dispatchEvent(new CustomEvent("close", { bubbles: true, composed: true }));
    }

    override render() {
        return html`
            <uui-dialog-layout headline=${this.headline} style=${`--godmode-modal-width: ${this.width}; --godmode-modal-max-height: ${this.maxHeight};`}>
                <uui-button class="close" compact look="secondary" label="Close" @click=${this._close}>
                    <uui-icon name="icon-wrong"></uui-icon>
                </uui-button>
                <slot></slot>
            </uui-dialog-layout>
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }

        uui-dialog-layout {
            position: relative;
            width: var(--godmode-modal-width);
            max-height: var(--godmode-modal-max-height);
        }

        .close {
            position: absolute;
            top: var(--uui-size-space-4);
            right: var(--uui-size-space-4);
            z-index: 1;
        }
    `;
}

export default GodModeModalLayoutElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-modal-layout": GodModeModalLayoutElement;
    }
}
