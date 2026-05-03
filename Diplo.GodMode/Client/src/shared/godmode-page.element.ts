import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";

/**
 * Shared page chrome — every browser slots its content into this element so
 * we get a consistent header, padding and reload button for free.
 */
@customElement("godmode-page")
export class GodModePageElement extends UmbElementMixin(LitElement) {
    @property({ type: String }) heading = "";
    @property({ type: String }) icon = "icon-sience";
    @property({ type: String }) description = "";
    @property({ type: Boolean, attribute: "show-reload" }) showReload = false;

    private _onReload() {
        this.dispatchEvent(new CustomEvent("reload", { bubbles: true, composed: true }));
    }

    override render() {
        return html`
            <umb-body-layout header-fit-height>
                <div slot="header" class="header">
                    <h1>
                        <umb-icon name=${this.icon}></umb-icon>
                        ${this.heading}
                    </h1>
                    ${this.description ? html`<p class="muted">${this.description}</p>` : ""}
                    ${this.showReload
                        ? html`<uui-button look="secondary" label="Reload" @click=${this._onReload}>
                              <uui-icon name="icon-refresh"></uui-icon> Reload
                          </uui-button>`
                        : ""}
                </div>
                <div class="content">
                    <slot></slot>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }
        .header {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-5);
        }
        h1 {
            margin: 0;
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
            font-size: var(--uui-type-h3-size);
        }
        .muted {
            color: var(--uui-color-text-alt);
            margin: 0;
        }
        .content {
            padding: var(--uui-size-space-5);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-page": GodModePageElement;
    }
}
