import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { openLazyEvidenceDrawer } from "../shared/evidence-drawer";
import "../shared";
import type { DatabaseTableDetail, DatabaseTableInfo, DatabaseTableRows } from "../shared/types";

@customElement("godmode-database-browser")
export class GodModeDatabaseBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _tables: DatabaseTableInfo[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _category = "";
    @state() private _sort: SortState = { column: "rowCount", reverse: true };
    @state() private _rowTable: DatabaseTableInfo | null = null;
    @state() private _rowPage: DatabaseTableRows | null = null;
    @state() private _rowLoading = false;
    @state() private _rowError = "";
    @state() private _rowPageSize = 25;

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

    private _openDetail(table: DatabaseTableInfo, e: Event) {
        openLazyEvidenceDrawer(
            this,
            {
                title: `Table: ${table.name}`,
                subtitle: table.purpose,
                sections: [],
                load: async () => {
                    const detailName = table.schema ? `${table.schema}.${table.name}` : table.name;
                    const detail = await godmodeGet<DatabaseTableDetail>(`database/tables/${encodeURIComponent(detailName)}`);

                    return {
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
                    };
                }
            },
            e
        );
    }

    private async _openRows(table: DatabaseTableInfo, page = 1, scrollToPanel = false) {
        this._rowTable = table;
        this._rowLoading = true;
        this._rowError = "";

        if (scrollToPanel) {
            void this._scrollRowsPanelIntoView();
        }

        try {
            const detailName = table.schema ? `${table.schema}.${table.name}` : table.name;
            this._rowPage = await godmodeGet<DatabaseTableRows>(`database/tables/${encodeURIComponent(detailName)}/rows`, {
                page,
                pageSize: this._rowPageSize
            });
        } catch {
            this._rowPage = null;
            this._rowError = "Could not load table rows.";
        } finally {
            this._rowLoading = false;
        }
    }

    private async _scrollRowsPanelIntoView() {
        await this.updateComplete;
        this.renderRoot.querySelector(".data-panel")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    private _onRowsPageChange = (e: CustomEvent<number>) => {
        if (this._rowTable) {
            void this._openRows(this._rowTable, e.detail);
        }
    };

    private _renderCell(value: unknown) {
        if (value === null || value === undefined) {
            return html`<span class="muted">null</span>`;
        }

        const text = typeof value === "object" ? JSON.stringify(value) : String(value);
        return html`<span title=${text}>${text}</span>`;
    }

    private _renderRowsPanel() {
        if (!this._rowTable) return "";

        return html`
            <uui-box class="data-panel" headline=${`Data: ${this._rowTable.name}`}>
                <div class="data-toolbar">
                    <div>
                        ${this._rowTable.schema ? html`<span class="muted">${this._rowTable.schema}</span>` : ""}
                        ${this._rowPage ? html`<span class="muted">${this._rowPage.totalItems.toLocaleString()} rows</span>` : ""}
                    </div>
                    <div>
                        <label>
                            Page size
                            <select
                                @change=${(e: Event) => {
                                    this._rowPageSize = Number((e.target as HTMLSelectElement).value);
                                    if (this._rowTable) void this._openRows(this._rowTable, 1);
                                }}
                            >
                                ${[10, 25, 50, 100].map((size) => html`<option value=${size} ?selected=${size === this._rowPageSize}>${size}</option>`)}
                            </select>
                        </label>
                        <uui-button compact look="secondary" label="Close data" @click=${() => ((this._rowTable = null), (this._rowPage = null))}>
                            <uui-icon name="icon-delete"></uui-icon>
                        </uui-button>
                    </div>
                </div>

                ${this._rowLoading
                    ? html`<uui-loader></uui-loader>`
                    : this._rowError
                      ? html`<p class="warning">${this._rowError}</p>`
                      : !this._rowPage || this._rowPage.items.length === 0
                        ? html`<p class="muted">No rows found.</p>`
                        : html`
                              <div class="data-grid">
                                  <uui-table>
                                      <uui-table-head>
                                          ${this._rowPage.columns.map((column) => html`<uui-table-head-cell>${column.name}</uui-table-head-cell>`)}
                                      </uui-table-head>
                                      ${this._rowPage.items.map(
                                          (row) => html`
                                              <uui-table-row>
                                                  ${this._rowPage?.columns.map((column) => html`<uui-table-cell>${this._renderCell(row[column.name])}</uui-table-cell>`)}
                                              </uui-table-row>
                                          `
                                      )}
                                  </uui-table>
                              </div>
                              <godmode-pager
                                  .currentPage=${this._rowPage.currentPage}
                                  .totalPages=${Math.max(1, this._rowPage.totalPages)}
                                  .totalItems=${this._rowPage.totalItems}
                                  @page-change=${this._onRowsPageChange}
                              ></godmode-pager>
                          `}
            </uui-box>
        `;
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
                                              <uui-button
                                                  compact
                                                  look="secondary"
                                                  label=${table.countSucceeded && table.rowCount === 0 ? "No rows to view" : "View data"}
                                                  ?disabled=${table.countSucceeded && table.rowCount === 0}
                                                  @click=${() => void this._openRows(table, 1, true)}
                                              >
                                                  <uui-icon name="icon-table"></uui-icon>
                                                  Data
                                              </uui-button>
                                              <uui-button compact look="secondary" label="Details" @click=${(e: Event) => void this._openDetail(table, e)}>
                                                  <uui-icon name="icon-search"></uui-icon>
                                                  Details
                                              </uui-button>
                                          </uui-table-cell>
                                      </uui-table-row>
                                  `
                              )}
                          </uui-table>
                          ${this._renderRowsPanel()}
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
            width: 13rem;
            white-space: nowrap;
        }
        .data-panel {
            display: block;
            margin-top: var(--uui-size-space-5);
        }
        .data-toolbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-3);
        }
        .data-toolbar > div {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
        }
        .data-toolbar label {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            margin: 0;
        }
        .data-toolbar select {
            width: auto;
        }
        .data-grid {
            overflow: auto;
            max-height: 560px;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }
        .data-grid uui-table {
            min-width: 100%;
        }
        .data-grid uui-table-cell,
        .data-grid uui-table-head-cell {
            max-width: 24rem;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
        @media (max-width: 900px) {
            .summary,
            .filters,
            .data-toolbar {
                grid-template-columns: 1fr;
                flex-direction: column;
                align-items: stretch;
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
