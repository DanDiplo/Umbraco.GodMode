import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { godmodeGet } from "../api/client";
import type { ReferenceEdge } from "../shared/types";
import type { GodModeUsedByModalData } from "../shared/used-by-modal";
import "../shared";

@customElement("godmode-used-by-modal")
export class GodModeUsedByModalElement extends LitElement {
    @property({ attribute: false }) modalContext?: { reject: () => void };
    @property({ type: Object, attribute: false }) data?: GodModeUsedByModalData;
    @state() private _edges: ReferenceEdge[] = [];
    @state() private _loading = true;
    @state() private _search = "";

    override connectedCallback(): void {
        super.connectedCallback();
        window.addEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
        void this._load();
    }

    override disconnectedCallback(): void {
        window.removeEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
        super.disconnectedCallback();
    }

    private async _load() {
        if (!this.data?.targetType || !this.data.targetKey) {
            this._loading = false;
            return;
        }

        this._loading = true;
        try {
            this._edges = await godmodeGet<ReferenceEdge[]>("references/used-by", {
                targetType: this.data.targetType,
                targetKey: this.data.targetKey
            });
        } finally {
            this._loading = false;
        }
    }

    private _filtered() {
        const q = this._search.trim().toLowerCase();
        if (!q) return this._edges;
        return this._edges.filter((edge) =>
            [edge.sourceType, edge.sourceName, edge.sourceAlias, edge.relation, edge.context ?? ""].some((value) => value.toLowerCase().includes(q))
        );
    }

    private _close = () => {
        this.modalContext?.reject();
    };

    private _onOutsidePointerDown = (e: PointerEvent) => {
        if (e.composedPath().includes(this)) {
            return;
        }

        this._close();
    };

    override render() {
        const target = this.data?.targetName || this.data?.targetAlias || this.data?.targetKey || "Selected item";
        const results = this._filtered();

        return html`
            <godmode-modal-layout headline=${`Used by: ${target}`} @close=${this._close}>
                <div class="summary">
                    <strong>${this.data?.targetType ?? ""}</strong>
                    ${this.data?.targetAlias ? html`<code>${this.data.targetAlias}</code>` : ""}
                    ${this.data?.targetKey ? html`<small><code>${this.data.targetKey}</code></small>` : ""}
                </div>

                <uui-box>
                    <label>Search</label>
                    <uui-input
                        type="search"
                        autocomplete="off"
                        autocorrect="off"
                        autocapitalize="off"
                        spellcheck="false"
                        placeholder="Filter by source, relation or context"
                        .value=${this._search}
                        @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                    ></uui-input>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._edges.length}</strong> references</p>
                          ${results.length ? this._renderTable(results) : html`<uui-box><p>No references found.</p></uui-box>`}
                      `}
            </godmode-modal-layout>
        `;
    }

    private _renderTable(edges: ReferenceEdge[]) {
        return html`
            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Used By</uui-table-head-cell>
                    <uui-table-head-cell>Relation</uui-table-head-cell>
                    <uui-table-head-cell>Context</uui-table-head-cell>
                </uui-table-head>
                ${edges.map(
                    (edge) => html`
                        <uui-table-row>
                            <uui-table-cell>
                                <strong>${edge.sourceName}</strong>
                                <small>${edge.sourceType}${edge.sourceAlias ? html` · <code>${edge.sourceAlias}</code>` : ""}</small>
                            </uui-table-cell>
                            <uui-table-cell><span class="relation">${edge.relation}</span></uui-table-cell>
                            <uui-table-cell>${edge.context ? html`<small>${edge.context}</small>` : ""}</uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    static override styles = css`
        .summary {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: var(--uui-size-space-2);
            margin-bottom: var(--uui-size-space-4);
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input {
            width: 100%;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        small {
            display: block;
            color: var(--uui-color-text-alt);
            margin-top: var(--uui-size-space-1);
        }
        .relation {
            font-weight: 600;
        }
    `;
}

export default GodModeUsedByModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-used-by-modal": GodModeUsedByModalElement;
    }
}
