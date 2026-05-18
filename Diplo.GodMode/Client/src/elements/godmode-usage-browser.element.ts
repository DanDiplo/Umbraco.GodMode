import { LitElement, css, customElement, html, state, svg } from "@umbraco-cms/backoffice/external/lit";
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
    @state() private _view: "chart" | "table" = "chart";

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
                        autocomplete="off"
                        autocorrect="off"
                        autocapitalize="off"
                        spellcheck="false"
                        placeholder="Filter by name or alias"
                        .value=${this._search}
                        @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                    ></uui-input>
                    <div class="view-switch">
                        <uui-button compact look=${this._view === "chart" ? "primary" : "secondary"} label="Chart" @click=${() => (this._view = "chart")}>
                            <uui-icon name="icon-chart-curve"></uui-icon>
                            Chart
                        </uui-button>
                        <uui-button compact look=${this._view === "table" ? "primary" : "secondary"} label="Table" @click=${() => (this._view = "table")}>
                            <uui-icon name="icon-list"></uui-icon>
                            Table
                        </uui-button>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          ${this._view === "chart" ? this._renderChart(results) : this._renderTable(results)}
                      `}
            </godmode-page>
        `;
    }

    private _renderTable(results: UsageModel[]) {
        return html`
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
        `;
    }

    private _renderChart(results: UsageModel[]) {
        if (!results.length) {
            return html`<uui-box><p class="muted">No content usage matches the current filter.</p></uui-box>`;
        }

        const top = [...results].sort((a, b) => b.nodeCount - a.nodeCount || a.alias.localeCompare(b.alias)).slice(0, 20);
        const max = Math.max(...top.map((item) => item.nodeCount), 1);
        const rowHeight = 34;
        const height = 76 + top.length * rowHeight;
        const chartLeft = 250;
        const chartWidth = 690;

        return html`
            <uui-box>
                <div class="chart-wrap">
                    ${svg`
                        <svg viewBox=${`0 0 980 ${height}`} role="img" aria-label="Content type usage chart">
                            <text class="chart-title" x="16" y="28">Highest usage by content type</text>
                            ${top.map((item, index) => {
                                const y = 58 + index * rowHeight;
                                const barWidth = Math.max(2, (item.nodeCount / max) * chartWidth);
                                return svg`
                                    <text class="bar-label" x="16" y=${y + 16}>${item.alias}</text>
                                    <rect class=${`bar ${this._typeClass(item.type)}`} x=${chartLeft + 16} y=${y} width=${barWidth} height="22" rx="4">
                                        <title>${item.alias}: ${item.nodeCount.toLocaleString()} ${item.nodeCount === 1 ? "instance" : "instances"}</title>
                                    </rect>
                                    <text class="bar-value" x=${chartLeft + 16 + barWidth + 8} y=${y + 16}>${item.nodeCount.toLocaleString()}</text>
                                `;
                            })}
                        </svg>
                    `}
                </div>
                ${results.length > top.length ? html`<p class="muted">Showing the 20 highest-use types. Use the Table view for the full filtered list.</p>` : ""}
            </uui-box>
        `;
    }

    private _typeClass(type: string): string {
        return `type-${type.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
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
        .view-switch {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
            margin-top: var(--uui-size-space-3);
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        .muted {
            color: var(--uui-color-text-alt);
        }
        .chart-wrap {
            overflow-x: auto;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }
        .chart-wrap svg {
            display: block;
            min-width: 760px;
            width: 100%;
        }
        .chart-title {
            font-weight: 700;
            fill: var(--uui-color-text);
        }
        .bar-label,
        .bar-value {
            font-size: 12px;
            fill: var(--uui-color-text);
        }
        .bar-label {
            font-family: Consolas, "Liberation Mono", Menlo, Monaco, "Courier New", monospace;
        }
        .bar-value {
            fill: var(--uui-color-text-alt);
        }
        .bar {
            fill: var(--uui-color-selected);
        }
        .bar.type-media {
            fill: var(--uui-color-positive-emphasis);
        }
        .bar.type-members {
            fill: var(--uui-color-warning-emphasis);
        }
        .bar.type-content-item,
        .bar.type-not-classified {
            fill: var(--uui-color-border-emphasis);
        }
    `;
}

export default GodModeUsageBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-usage-browser": GodModeUsageBrowserElement;
    }
}
