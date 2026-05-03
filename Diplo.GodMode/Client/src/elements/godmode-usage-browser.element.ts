import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { UsageModel } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";

@customElement("godmode-usage-browser")
export class GodModeUsageBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _items: UsageModel[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _sort: SortState = { column: "alias", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            this._items = await godmodeGet<UsageModel[]>("content-usage");
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): UsageModel[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._items.filter((u) => {
            if (!q) return true;
            return (
                u.alias.toLowerCase().includes(q) ||
                (u.description ?? "").toLowerCase().includes(q) ||
                u.type.toLowerCase().includes(q)
            );
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as UsageModel[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const results = this._filtered();
        return html`
            <godmode-page
                heading="Usage Browser"
                description="See how content types are used and how many instances exist."
                show-reload
                @reload=${() => void this._load()}
            >
                <uui-box>
                    <label>Search</label>
                    <uui-input
                        type="search"
                        placeholder="Filter by name or alias"
                        .value=${this._search}
                        @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                    ></uui-input>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          <uui-table @sort-change=${this._onSortChange}>
                              <uui-table-head>
                                  <godmode-sort-header column="alias" .sort=${this._sort}>Alias</godmode-sort-header>
                                  <godmode-sort-header column="type" .sort=${this._sort}>Type</godmode-sort-header>
                                  <godmode-sort-header column="nodeCount" .sort=${this._sort}>Instances</godmode-sort-header>
                                  <godmode-sort-header column="description" .sort=${this._sort}>Description</godmode-sort-header>
                              </uui-table-head>
                              ${results.map(
                                  (u) => html`
                                      <uui-table-row>
                                          <uui-table-cell><strong><code>${u.alias}</code></strong></uui-table-cell>
                                          <uui-table-cell>${u.type}</uui-table-cell>
                                          <uui-table-cell>${u.nodeCount}</uui-table-cell>
                                          <uui-table-cell><small>${u.description ?? ""}</small></uui-table-cell>
                                      </uui-table-row>
                                  `
                              )}
                          </uui-table>
                      `}
            </godmode-page>
        `;
    }

    static override styles = css`
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
    `;
}

export default GodModeUsageBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-usage-browser": GodModeUsageBrowserElement;
    }
}
