import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { GODMODE_BROWSER_WORKSPACE_CONTEXT } from "../workspaces/godmode-browser-workspace.context";

/**
 * Shared page chrome — every browser slots its content into this element so
 * we get a consistent header and padding. Reload is provided by the native
 * workspace header action menu (the Reload entity action); when raised, we
 * re-dispatch the existing `reload` event so each browser refetches.
 */
@customElement("godmode-page")
export class GodModePageElement extends UmbElementMixin(LitElement) {
    @property({ type: String }) heading = "";
    @property({ type: String }) icon = "icon-sience";
    @property({ type: String }) description = "";
    @property({ type: Boolean, attribute: "show-reload" }) showReload = false;

    #reloadInitialised = false;

    constructor() {
        super();
        this.consumeContext(GODMODE_BROWSER_WORKSPACE_CONTEXT, (context) => {
            this.#reloadInitialised = false;
            this.observe(
                context?.reload,
                () => {
                    // Skip the initial emission; only react to actual reload requests.
                    if (!this.#reloadInitialised) {
                        this.#reloadInitialised = true;
                        return;
                    }
                    this.dispatchEvent(new CustomEvent("reload", { bubbles: true, composed: true }));
                },
                "observeReload"
            );
        });
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
                </div>
                ${this.showReload
                    ? html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
                    : ""}
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
            padding: var(--uui-size-space-5) 0;
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
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-page": GodModePageElement;
    }
}
