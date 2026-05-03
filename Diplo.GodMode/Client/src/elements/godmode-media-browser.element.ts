import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ItemBase, MediaMap, Page } from "../shared/types";
import { formatBytes, truncate } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";

@customElement("godmode-media-browser")
export class GodModeMediaBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _page: Page<MediaMap> | null = null;
    @state() private _mediaTypes: ItemBase[] = [];
    @state() private _loading = false;
    @state() private _currentPage = 1;
    @state() private _pageSize = 20;
    @state() private _name = "";
    @state() private _mediaTypeId: number | null = null;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadFilters();
        void this._fetch();
    }

    private async _loadFilters() {
        try {
            this._mediaTypes = await godmodeGet<ItemBase[]>("media-types");
        } catch {
            // non-fatal
        }
    }

    private async _fetch() {
        this._loading = true;
        try {
            this._page = await godmodeGet<Page<MediaMap>>("media", {
                page: this._currentPage,
                pageSize: this._pageSize,
                name: this._name,
                mediaTypeId: this._mediaTypeId
            });
        } finally {
            this._loading = false;
        }
    }

    private _onPageChange = (e: CustomEvent<number>) => {
        this._currentPage = e.detail;
        void this._fetch();
    };

    override render() {
        return html`
            <godmode-page
                heading="Media Browser"
                description="Search media items and filter by type."
                show-reload
                @reload=${() => void this._fetch()}
            >
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by name"
                                .value=${this._name}
                                @change=${(e: Event) => {
                                    this._name = (e.target as HTMLInputElement).value;
                                    this._currentPage = 1;
                                    void this._fetch();
                                }}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Media Type</label>
                            <select
                                @change=${(e: Event) => {
                                    const v = (e.target as HTMLSelectElement).value;
                                    this._mediaTypeId = v === "" ? null : Number(v);
                                    this._currentPage = 1;
                                    void this._fetch();
                                }}
                            >
                                <option value="">Any</option>
                                ${this._mediaTypes.map((mt) => html`<option value=${mt.id}>${mt.name}</option>`)}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._page
                      ? html`
                            <godmode-pager
                                .currentPage=${this._page.currentPage}
                                .totalPages=${this._page.totalPages}
                                .totalItems=${this._page.totalItems}
                                @page-change=${this._onPageChange}
                            ></godmode-pager>
                            <uui-table>
                                <uui-table-head>
                                    <uui-table-head-cell>Id</uui-table-head-cell>
                                    <uui-table-head-cell>Name</uui-table-head-cell>
                                    <uui-table-head-cell>Type</uui-table-head-cell>
                                    <uui-table-head-cell>Ext</uui-table-head-cell>
                                    <uui-table-head-cell>Size</uui-table-head-cell>
                                    <uui-table-head-cell>Updated</uui-table-head-cell>
                                </uui-table-head>
                                ${this._page.items.map(
                                    (m) => html`
                                        <uui-table-row>
                                            <uui-table-cell><strong>${m.id}</strong></uui-table-cell>
                                            <uui-table-cell>
                                                <a href=${editUrl("media", m.udi)} @click=${(e: Event) => openEditorModal(this, "media", m.udi, e)}
                                                    ><strong>${m.name}</strong></a
                                                >
                                                <small style="display:block;color:var(--uui-color-text-alt)">${m.path}</small>
                                            </uui-table-cell>
                                            <uui-table-cell><code>${m.type}</code></uui-table-cell>
                                            <uui-table-cell><code>${m.ext ?? ""}</code></uui-table-cell>
                                            <uui-table-cell>${formatBytes(m.size)}</uui-table-cell>
                                            <uui-table-cell><small>${truncate(m.updateDate, 22)}</small></uui-table-cell>
                                        </uui-table-row>
                                    `
                                )}
                            </uui-table>
                        `
                      : ""}
            </godmode-page>
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
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    `;
}

export default GodModeMediaBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-media-browser": GodModeMediaBrowserElement;
    }
}
