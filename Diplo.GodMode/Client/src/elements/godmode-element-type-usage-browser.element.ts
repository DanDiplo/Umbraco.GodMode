import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ElementTypeUsageStatus, ElementTypeUsageSummary } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openElementTypeUsageModal } from "../shared/element-type-usage-modal";

type TriState = "any" | "yes" | "no";

@customElement("godmode-element-type-usage-browser")
export class GodModeElementTypeUsageBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _status: ElementTypeUsageStatus | null = null;
    @state() private _items: ElementTypeUsageSummary[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _usedFilter: TriState = "any";
    @state() private _sort: SortState = { column: "elementTypeName", reverse: false };
    @state() private _currentPage = 1;
    @state() private _pageSize = 25;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            const status = await godmodeGet<ElementTypeUsageStatus>("element-type-usage/status");
            this._status = status;
            this._items = status.isSupported ? await godmodeGet<ElementTypeUsageSummary[]>("element-type-usage") : [];
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): ElementTypeUsageSummary[] {
        const q = this._search.trim().toLowerCase();
        const used = this._usedFilter;

        const matched = this._items.filter((item) => {
            if (q) {
                const hit = item.elementTypeName.toLowerCase().includes(q) || item.elementTypeAlias.toLowerCase().includes(q);
                if (!hit) return false;
            }
            if (used === "yes" && item.usageCount === 0) return false;
            if (used === "no" && item.usageCount !== 0) return false;
            return true;
        });

        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as ElementTypeUsageSummary[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
        this._currentPage = 1;
    };

    private _onPageChange = (e: CustomEvent<number>) => {
        this._currentPage = e.detail;
    };

    private _onSearchChange = (e: Event) => {
        this._search = (e.target as HTMLInputElement).value;
        this._currentPage = 1;
    };

    private _onUsedFilterChange = (e: Event) => {
        this._usedFilter = (e.target as unknown as { value: string }).value as TriState;
        this._currentPage = 1;
    };

    private _pageSlice(results: ElementTypeUsageSummary[]): ElementTypeUsageSummary[] {
        const start = (this._currentPage - 1) * this._pageSize;
        return results.slice(start, start + this._pageSize);
    }

    private _openDetail(item: ElementTypeUsageSummary, e: Event) {
        openElementTypeUsageModal(
            this,
            {
                elementTypeKey: item.elementTypeKey,
                elementTypeName: item.elementTypeName,
                elementTypeAlias: item.elementTypeAlias
            },
            e
        );
    }

    override render() {
        return html`
            <godmode-page
                heading="Element Type Usage"
                description="Browse Element Type usage across Block Lists, Block Grids, Library Items and Element Pickers."
                show-reload
                @reload=${() => void this._load()}
            >
                ${this._loading ? html`<uui-loader></uui-loader>` : this._renderContent()}
            </godmode-page>
        `;
    }

    private _renderContent() {
        if (this._status && !this._status.isSupported) {
            return html`
                <uui-box headline="Not available">
                    <p>${this._status.message}</p>
                </uui-box>
            `;
        }

        const results = this._filtered();
        const pageResults = this._pageSlice(results);
        const totalPages = Math.max(1, Math.ceil(results.length / this._pageSize));

        return html`
            ${this._status && !this._status.libraryFeatureAvailable
                ? html`
                      <uui-box class="notice">
                          <p><uui-icon name="icon-info"></uui-icon> ${this._status.message}</p>
                      </uui-box>
                  `
                : ""}

            <uui-box>
                <div class="filters">
                    <div>
                        <label for="search">Search</label>
                        <uui-input
                            id="search"
                            type="search"
                            autocomplete="off"
                            autocorrect="off"
                            autocapitalize="off"
                            spellcheck="false"
                            placeholder="Filter by name or alias"
                            .value=${this._search}
                            @input=${this._onSearchChange}
                        ></uui-input>
                    </div>
                    <div>
                        <label for="used">Usage</label>
                        <uui-select
                            id="used"
                            label="Usage"
                            .options=${[
                                { name: "Any", value: "any", selected: this._usedFilter === "any" },
                                { name: "Used", value: "yes", selected: this._usedFilter === "yes" },
                                { name: "Unused", value: "no", selected: this._usedFilter === "no" }
                            ]}
                            @change=${this._onUsedFilterChange}
                        ></uui-select>
                    </div>
                </div>
            </uui-box>

            <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>

            <uui-table @sort-change=${this._onSortChange}>
                <uui-table-head>
                    <godmode-sort-header column="elementTypeName" .sort=${this._sort}>Element Type</godmode-sort-header>
                    <godmode-sort-header column="usageCount" .sort=${this._sort}>Usage Count</godmode-sort-header>
                    <godmode-sort-header column="contentUses" .sort=${this._sort}>Content Uses</godmode-sort-header>
                    <godmode-sort-header column="settingsUses" .sort=${this._sort}>Settings Uses</godmode-sort-header>
                    <godmode-sort-header column="libraryItems" .sort=${this._sort}>Library Items</godmode-sort-header>
                    <godmode-sort-header column="elementPickerUses" .sort=${this._sort}>Element Picker Uses</godmode-sort-header>
                    <uui-table-head-cell>Actions</uui-table-head-cell>
                </uui-table-head>
                ${pageResults.map(
                    (item) => html`
                        <uui-table-row>
                            <uui-table-cell>
                                <a
                                    href=${editUrl("documentType", item.elementTypeKey)}
                                    @click=${(e: Event) => openEditorModal(this, "documentType", item.elementTypeKey, e)}
                                >
                                    ${item.icon ? html`<uui-icon name=${item.icon}></uui-icon>` : ""}
                                    <strong>${item.elementTypeName}</strong>
                                </a>
                                <div><code>${item.elementTypeAlias}</code></div>
                            </uui-table-cell>
                            <uui-table-cell>
                                <strong class=${item.usageCount === 0 ? "zero" : ""}>${item.usageCount}</strong>
                            </uui-table-cell>
                            <uui-table-cell>${item.contentUses}</uui-table-cell>
                            <uui-table-cell>${item.settingsUses}</uui-table-cell>
                            <uui-table-cell>${item.libraryItems}</uui-table-cell>
                            <uui-table-cell>${item.elementPickerUses}</uui-table-cell>
                            <uui-table-cell class="action-cell">
                                <uui-button compact look="secondary" label="Details" @click=${(e: Event) => this._openDetail(item, e)}>
                                    Details
                                </uui-button>
                            </uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>

            <godmode-pager
                .currentPage=${this._currentPage}
                .totalPages=${totalPages}
                .totalItems=${results.length}
                @page-change=${this._onPageChange}
            ></godmode-pager>
        `;
    }

    static override styles = css`
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .filters {
            display: grid;
            grid-template-columns: minmax(220px, 2fr) minmax(140px, 1fr);
            gap: var(--uui-size-space-4);
        }
        .filters uui-input,
        .filters uui-select {
            width: 100%;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .notice {
            margin-bottom: var(--uui-size-space-4);
        }
        .notice p {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            margin: 0;
            color: var(--uui-color-text-alt);
        }
        .zero {
            color: var(--uui-color-warning-emphasis);
        }
    `;
}

export default GodModeElementTypeUsageBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-element-type-usage-browser": GodModeElementTypeUsageBrowserElement;
    }
}
