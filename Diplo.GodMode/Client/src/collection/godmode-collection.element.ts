import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbRoute } from "@umbraco-cms/backoffice/router";

/**
 * Collection element for Diplo.Collection.GodMode.TreeItemChildren. Behaves like
 * Umbraco's default collection element (loading-aware router, empty state) but
 * omits the toolbar/header — these collections have no filter, create action or
 * view switcher, so that header would otherwise render empty. No <umb-body-layout>
 * wrapper either: the host <umb-workspace-editor> already provides one.
 */
@customElement("godmode-collection")
export class GodModeCollectionElement extends UmbLitElement {
    #collectionContext?: typeof UMB_COLLECTION_CONTEXT.TYPE;

    @state()
    private _routes: UmbRoute[] = [];

    @state()
    private _hasItems = false;

    @state()
    private _initialLoadDone = false;

    @state()
    private _emptyLabel = "";

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (context) => {
            this.#collectionContext = context;
            this.#observeIsLoading();
            this.observe(
                context?.view.routes,
                (routes) => {
                    this._routes = routes ?? [];
                },
                "observeRoutes"
            );
            this.observe(
                context?.totalItems,
                (totalItems) => {
                    this._hasItems = (totalItems ?? 0) > 0;
                },
                "observeTotalItems"
            );
            this._emptyLabel = context?.getEmptyLabel?.() ?? "";
            this.#collectionContext?.loadCollection();
        });
    }

    #observeIsLoading() {
        let hasBeenLoading = false;
        this.observe(
            this.#collectionContext?.loading,
            (isLoading) => {
                if (isLoading) {
                    hasBeenLoading = true;
                } else if (hasBeenLoading) {
                    this._initialLoadDone = true;
                }
            },
            "observeIsLoading"
        );
    }

    override render() {
        if (!this._routes.length) return nothing;
        return html`
            <umb-router-slot
                id="router"
                class=${this._hasItems ? "has-items" : ""}
                .routes=${this._routes}></umb-router-slot>
            ${this._hasItems ? nothing : this.#renderEmptyState()}
        `;
    }

    #renderEmptyState() {
        if (!this._initialLoadDone) return nothing;
        return html`
            <div id="empty-state" class="uui-text">
                <h4>${this.localize.string(this._emptyLabel)}</h4>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: flex;
                flex-direction: column;
                height: 100%;
                box-sizing: border-box;
                /* umb-workspace-editor hard-codes main-no-padding on its body-layout
                   (shadow DOM, not overridable), so restore the standard content
                   padding here — matching the padded body-layout the default
                   collection element renders. */
                padding: var(--uui-size-layout-1);
            }

            #router {
                visibility: hidden;
                width: 100%;
                height: 100%;
            }

            #router.has-items {
                visibility: visible;
            }

            #empty-state {
                height: 80%;
                align-content: center;
                text-align: center;
            }
        `
    ];
}

export default GodModeCollectionElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-collection": GodModeCollectionElement;
    }
}
