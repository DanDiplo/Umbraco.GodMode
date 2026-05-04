import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { TypeMap } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { uniqueBy } from "../shared/format";

/**
 * Reusable "list of TypeMap rows" page used by every reflection browser
 * (surface, api, render, models, composers, converters, components, taghelpers,
 *  finders, urlproviders, services). The catalog wires each menu entry to a
 * thin wrapper that just sets `endpoint`, `heading` and `description`.
 */
@customElement("godmode-reflection-browser")
export class GodModeReflectionBrowserElement extends UmbElementMixin(LitElement) {
    @property({ type: String }) endpoint = "";
    @property({ type: String }) heading = "Reflection Browser";
    @property({ type: String }) description = "";
    @property({ type: Boolean, attribute: "group-by-assembly" }) groupByAssembly = false;

    @state() private _items: TypeMap[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _ns = "";
    @state() private _sort: SortState = { column: "name", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    override willUpdate(changed: Map<string, unknown>) {
        if (changed.has("endpoint")) {
            void this._load();
        }
    }

    private async _load() {
        if (!this.endpoint) return;
        this._loading = true;
        try {
            this._items = await godmodeGet<TypeMap[]>(this.endpoint);
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): TypeMap[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._items.filter((c) => {
            if (q && !c.name.toLowerCase().includes(q) && !(c.loadableName ?? "").toLowerCase().includes(q)) return false;
            if (this._ns && c.namespace !== this._ns) return false;
            return true;
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as TypeMap[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const namespaces = uniqueBy(this._items, "namespace")
            .map((x) => x.namespace)
            .filter(Boolean)
            .sort();
        const results = this._filtered();
        const groups = this.groupByAssembly ? this._groupByAssembly(results) : [];

        return html`
            <godmode-page heading=${this.heading} description=${this.description} show-reload @reload=${() => void this._load()}>
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by name"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Namespace</label>
                            <select @change=${(e: Event) => (this._ns = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${namespaces.map((n) => html`<option value=${n}>${n}</option>`)}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          ${this.groupByAssembly
                              ? groups.map(
                                    (group) => html`
                                        <h3>${group.assembly} <span>${group.items.length}</span></h3>
                                        ${this._renderTable(group.items, false)}
                                    `
                                )
                              : this._renderTable(results, true)}
                      `}
            </godmode-page>
        `;
    }

    private _groupByAssembly(items: TypeMap[]): Array<{ assembly: string; items: TypeMap[] }> {
        const grouped = new Map<string, TypeMap[]>();
        for (const item of items) {
            const key = item.assembly || "Unknown";
            grouped.set(key, [...(grouped.get(key) ?? []), item]);
        }

        return [...grouped.entries()]
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([assembly, groupItems]) => ({ assembly, items: groupItems }));
    }

    private _renderTable(items: TypeMap[], sortable: boolean) {
        return html`
            <uui-table @sort-change=${sortable ? this._onSortChange : undefined}>
                <uui-table-head>
                    ${sortable
                        ? html`
                              <godmode-sort-header column="name" .sort=${this._sort}>Name</godmode-sort-header>
                              <godmode-sort-header column="namespace" .sort=${this._sort}>Namespace</godmode-sort-header>
                              <godmode-sort-header column="assembly" .sort=${this._sort}>Assembly</godmode-sort-header>
                              <godmode-sort-header column="baseType" .sort=${this._sort}>Base</godmode-sort-header>
                          `
                        : html`
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Namespace</uui-table-head-cell>
                              <uui-table-head-cell>Assembly</uui-table-head-cell>
                              <uui-table-head-cell>Base</uui-table-head-cell>
                          `}
                </uui-table-head>
                ${items.map(
                    (c) => html`
                        <uui-table-row>
                            <uui-table-cell><strong>${c.name}</strong></uui-table-cell>
                            <uui-table-cell><code>${c.namespace}</code></uui-table-cell>
                            <uui-table-cell><code>${c.assembly}</code></uui-table-cell>
                            <uui-table-cell>${c.baseType}</uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: 2fr 1fr;
            gap: var(--uui-size-space-4);
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .filters uui-input {
            width: 100%;
        }
        .filters select {
            width: 100%;
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
        h3 {
            margin: var(--uui-size-space-5) 0 var(--uui-size-space-2);
            font-size: 1rem;
        }
        h3 span {
            color: var(--uui-color-text-alt);
            font-weight: 400;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-reflection-browser": GodModeReflectionBrowserElement;
    }
}
