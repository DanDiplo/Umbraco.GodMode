import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import type { SortState } from "./sort";

/**
 * Clickable table-header cell that emits a `sort-change` event when activated.
 * Place inside `<uui-table-head>` exactly like a native `<uui-table-head-cell>`.
 */
@customElement("godmode-sort-header")
export class GodModeSortHeaderElement extends LitElement {
    @property({ type: String }) column = "";
    @property({ type: Object }) sort: SortState = { column: "", reverse: false };

    private _onClick() {
        this.dispatchEvent(
            new CustomEvent<string>("sort-change", {
                detail: this.column,
                bubbles: true,
                composed: true
            })
        );
    }

    override render() {
        const active = this.sort.column === this.column;
        const arrow = !active ? "" : this.sort.reverse ? " ▾" : " ▴";
        return html`
            <uui-table-head-cell>
                <button type="button" @click=${this._onClick}>
                    <slot></slot>${arrow}
                </button>
            </uui-table-head-cell>
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }
        button {
            background: none;
            border: 0;
            padding: 0;
            font: inherit;
            cursor: pointer;
            color: inherit;
            font-weight: 600;
        }
        button:hover {
            text-decoration: underline;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-sort-header": GodModeSortHeaderElement;
    }
}
