import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { openEvidenceDrawer } from "../shared/evidence-drawer";
import "../shared";
import type { DatabaseTableDetail, DatabaseTableInfo } from "../shared/types";

@customElement("godmode-database-browser")
export class GodModeDatabaseBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _tables: DatabaseTableInfo[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _category = "";
    @state() private _sort: SortState = { column: "rowCount", reverse: true };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            this._tables = await godmodeGet<DatabaseTableInfo[]>("database/tables");
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): DatabaseTableInfo[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._tables.filter((table) => {
            if (this._category && table.category !== this._category) return false;
            if (!q) return true;
            return [table.name, table.schema, table.category, table.purpose, table.warning].some((value) => value.toLowerCase().includes(q));
        });

        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as DatabaseTableInfo[];
    }

    private _categories(): string[] {
        return Array.from(new Set(this._tables.map((table) => table.category))).filter(Boolean).sort();
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    private async _openDetail(table: DatabaseTableInfo, e: Event) {
        e.preventDefault();
        e.stopPropagation();

        const detailName = table.schema ? `${table.schema}.${table.name}` : table.name;
        const detail = await godmodeGet<DatabaseTableDetail>(`database/tables/${encodeURIComponent(detailName)}`);

        openEvidenceDrawer(
            this,
            {
                title: `Table: ${detail.name}`,
                subtitle: detail.purpose,
                summary: [
                    { label: "Rows", value: detail.countSucceeded ? detail.rowCount.toLocaleString() : "Unknown" },
                    { label: "Category", value: detail.category },
                    { label: "Schema", value: detail.schema || "Default" },
                    { label: "Columns", value: detail.columns.length },
                    { label: "Outgoing FK", value: detail.outgoingRelationships.length },
                    { label: "Incoming FK", value: detail.incomingRelationships.length }
                ],
                sections: [
                    {
                        heading: "Relationship Map",
                        description: "Declared foreign-key relationships around this table.",
                        visual: "database-relationship-graph",
                        items: {
                            center: {
                                name: detail.name,
                                category: detail.category,
                                rowCount: detail.rowCount,
                                countSucceeded: detail.countSucceeded,
                                warning: detail.warning
                            },
                            incoming: detail.incomingRelationships,
                            outgoing: detail.outgoingRelationships
                        }
                    },
                    {
                        heading: "Columns",
                        items: detail.columns.map((column) => ({
                            name: column.name,
                            dataType: column.maxLength ? `${column.dataType}(${column.maxLength})` : column.dataType,
                            nullable: column.nullable,
                            primaryKey: column.primaryKey
                        }))
                    },
                    {
                        heading: "Outgoing Relationships",
                        description: "Foreign keys from this table to other tables.",
                        items: detail.outgoingRelationships
                    },
                    {
                        heading: "Incoming Relationships",
                        description: "Foreign keys from other tables into this table.",
                        items: detail.incomingRelationships
                    }
                ]
            },
            e
        );
    }

    override render() {
        const results = this._filtered();
        const totalRows = this._tables.reduce((sum, table) => sum + (table.countSucceeded ? table.rowCount : 0), 0);

        return html`
            <godmode-page
                heading="Database Browser"
                description="Inspect database tables, row counts, columns and declared relationships."
                show-reload
                @reload=${() => void this._load()}
            >
                <div class="summary">
                    <div>
                        <span>Tables</span>
                        <strong>${this._tables.length}</strong>
                    </div>
                    <div>
                        <span>Known Rows</span>
                        <strong>${totalRows.toLocaleString()}</strong>
                    </div>
                    <div>
                        <span>Warnings</span>
                        <strong>${this._tables.filter((table) => table.warning).length}</strong>
                    </div>
                </div>

                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by table, category or purpose"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Category</label>
                            <select @change=${(e: Event) => (this._category = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${this._categories().map((category) => html`<option value=${category} ?selected=${category === this._category}>${category}</option>`)}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._tables.length}</strong> tables</p>
                          <uui-table @sort-change=${this._onSortChange}>
                              <uui-table-head>
                                  <godmode-sort-header column="name" .sort=${this._sort}>Table</godmode-sort-header>
                                  <godmode-sort-header column="rowCount" .sort=${this._sort}>Rows</godmode-sort-header>
                                  <godmode-sort-header column="category" .sort=${this._sort}>Category</godmode-sort-header>
                                  <uui-table-head-cell>Purpose</uui-table-head-cell>
                                  <uui-table-head-cell>Status</uui-table-head-cell>
                                  <uui-table-head-cell>Actions</uui-table-head-cell>
                              </uui-table-head>
                              ${results.map(
                                  (table) => html`
                                      <uui-table-row>
                                          <uui-table-cell>
                                              <strong><code>${table.name}</code></strong>
                                              ${table.schema ? html`<small>${table.schema}</small>` : ""}
                                          </uui-table-cell>
                                          <uui-table-cell>${table.countSucceeded ? table.rowCount.toLocaleString() : html`<span class="muted">Unknown</span>`}</uui-table-cell>
                                          <uui-table-cell><span class="pill">${table.category}</span></uui-table-cell>
                                          <uui-table-cell><small>${table.purpose}</small></uui-table-cell>
                                          <uui-table-cell>${table.warning ? html`<span class="warning">${table.warning}</span>` : html`<span class="muted">OK</span>`}</uui-table-cell>
                                          <uui-table-cell class="action-cell">
                                              <uui-button compact look="secondary" label="Details" @click=${(e: Event) => void this._openDetail(table, e)}>
                                                  <uui-icon name="icon-search"></uui-icon>
                                                  Details
                                              </uui-button>
                                          </uui-table-cell>
                                      </uui-table-row>
                                  `
                              )}
                          </uui-table>
                      `}
            </godmode-page>
        `;
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .summary div {
            display: grid;
            gap: var(--uui-size-space-1);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            padding: var(--uui-size-space-4);
        }
        .summary span,
        .muted,
        small {
            color: var(--uui-color-text-alt);
        }
        .summary strong {
            font-size: 1.4rem;
        }
        .filters {
            display: grid;
            grid-template-columns: minmax(240px, 2fr) minmax(180px, 1fr);
            gap: var(--uui-size-space-4);
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input,
        select {
            width: 100%;
        }
        select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        small {
            display: block;
            margin-top: var(--uui-size-space-1);
        }
        .pill,
        .warning {
            display: inline-flex;
            align-items: center;
            border-radius: 999px;
            padding: 2px var(--uui-size-space-2);
            font-size: 12px;
            font-weight: 700;
        }
        .pill {
            background: var(--uui-color-surface-alt);
        }
        .warning {
            background: var(--uui-color-warning-emphasis);
            color: var(--uui-color-warning-contrast);
        }
        .action-cell {
            text-align: right;
            width: 8rem;
        }
        @media (max-width: 900px) {
            .summary,
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeDatabaseBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-database-browser": GodModeDatabaseBrowserElement;
    }
}
