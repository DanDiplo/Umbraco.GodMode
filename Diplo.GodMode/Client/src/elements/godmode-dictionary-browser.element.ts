import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_EDIT_DICTIONARY_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/dictionary";
import { godmodeGet } from "../api/client";
import { openEditorModal } from "../shared/edit-links";
import type { DictionaryBrowserItem, Lang, Page } from "../shared/types";
import "../shared";

@customElement("godmode-dictionary-browser")
export class GodModeDictionaryBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _languages: Lang[] = [];
    @state() private _result?: Page<DictionaryBrowserItem>;
    @state() private _loading = true;
    @state() private _error = "";
    @state() private _search = "";
    @state() private _languageId = "";
    @state() private _status = "all";
    @state() private _includeGroups = false;
    private _page = 1;
    private _request = 0;
    private _searchTimer?: ReturnType<typeof setTimeout>;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load(true);
    }

    override disconnectedCallback(): void {
        clearTimeout(this._searchTimer);
        this._request++;
        super.disconnectedCallback();
    }

    private async _load(refreshLanguages = false, keepResults = false) {
        clearTimeout(this._searchTimer);
        const request = ++this._request;
        this._loading = !keepResults || !this._result;
        this._error = "";
        try {
            const [result, languages] = await Promise.all([
                godmodeGet<Page<DictionaryBrowserItem>>("dictionary", {
                    page: this._page,
                    pageSize: 25,
                    search: this._search,
                    languageId: this._languageId,
                    status: this._status,
                    includeGroups: this._includeGroups
                }),
                refreshLanguages || !this._languages.length ? godmodeGet<Lang[]>("languages") : Promise.resolve(this._languages)
            ]);
            if (request !== this._request) return;
            this._languages = languages;
            this._result = result;
        } catch {
            if (request === this._request) this._error = "Could not load dictionary items. Please try reloading.";
        } finally {
            if (request === this._request) this._loading = false;
        }
    }

    private _filterChanged() {
        this._page = 1;
        void this._load();
    }

    private _searchChanged(e: Event) {
        this._search = (e.target as HTMLInputElement).value;
        this._page = 1;
        this._request++;
        this._loading = true;
        clearTimeout(this._searchTimer);
        this._searchTimer = setTimeout(() => void this._load(), 300);
    }

    private _editHref(id: string) {
        return UMB_EDIT_DICTIONARY_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: id });
    }

    private _openEditor(id: string, e: Event) {
        // Keep the table mounted during the refresh so closing the editor also
        // preserves the browser's scroll position.
        openEditorModal(this, "dictionary", id, e, () => void this._load(false, true));
    }

    override render() {
        const languages = this._languages.filter((l) => !this._languageId || String(l.id) === this._languageId);
        return html`
            <godmode-page heading="Dictionary Browser" icon="icon-book"
                description="Search dictionary keys and translations, compare languages and find missing values."
                show-reload @reload=${() => void this._load(true)}>
                <uui-box>
                    <div class="filters">
                        <div>
                            <label for="search">Search keys and translations</label>
                            <uui-input id="search" label="Search keys and translations" type="search" maxlength="500"
                                placeholder="Search dictionary…" .value=${this._search}
                                @input=${this._searchChanged}></uui-input>
                        </div>
                        <div>
                            <label for="language">Language</label>
                            <select id="language" .value=${this._languageId} @change=${(e: Event) => {
                                this._languageId = (e.target as HTMLSelectElement).value;
                                this._filterChanged();
                            }}>
                                <option value="">All languages</option>
                                ${this._languages.map((l) => html`<option value=${l.id}>${l.name} (${l.culture})</option>`)}
                            </select>
                        </div>
                        <div>
                            <label for="status">Translation status</label>
                            <select id="status" .value=${this._status} @change=${(e: Event) => {
                                this._status = (e.target as HTMLSelectElement).value;
                                this._filterChanged();
                            }}>
                                <option value="all">All items</option>
                                <option value="missing">Missing translations</option>
                                <option value="complete">Complete translations</option>
                            </select>
                        </div>
                    </div>
                    <div class="group-filter">
                        <uui-checkbox label="Include empty parent items" .checked=${this._includeGroups}
                            @change=${(e: Event) => {
                                this._includeGroups = (e.target as HTMLInputElement).checked;
                                this._filterChanged();
                            }}>Include empty parent items</uui-checkbox>
                        <p class="muted">Parents with no values in any language may be grouping items. These are hidden by default.</p>
                    </div>
                </uui-box>

                <div aria-busy=${this._loading ? "true" : "false"}>
                    ${this._loading ? html`<div class="loading" role="status"><uui-loader></uui-loader><span>Loading dictionary items…</span></div>`
                        : this._error ? html`<uui-box class="message"><p role="alert">${this._error}</p><uui-button look="secondary" label="Reload dictionary" @click=${() => void this._load(true)}>Reload</uui-button></uui-box>`
                        : this._renderResults(languages)}
                </div>
            </godmode-page>
        `;
    }

    private _renderResults(languages: Lang[]) {
        const result = this._result;
        if (!result) return "";
        return html`
            <p class="results" role="status">${result.totalItems} matching ${result.totalItems === 1 ? "item" : "items"} · ${languages.length} ${languages.length === 1 ? "language" : "languages"}</p>
            ${result.items.length ? html`
                <uui-box>
                    <p class="muted hint">Select a key to edit it in a side panel. Status is based on the selected languages; a non-empty value does not guarantee a reviewed translation.</p>
                    <div class="table-scroll" tabindex="0" role="region" aria-label="Dictionary translations">
                        <table>
                            <caption class="sr-only">Dictionary keys and their values in the selected languages</caption>
                            <thead><tr>
                                <th scope="col">Dictionary key</th>
                                <th scope="col">Status</th>
                                ${languages.map((l) => html`<th scope="col">${l.name}<span class="culture">${l.culture}</span></th>`)}
                            </tr></thead>
                            <tbody>${result.items.map((item) => {
                                const missing = languages.filter((l) => !item.values[l.culture]?.trim()).length;
                                return html`<tr>
                                    <th scope="row">
                                        <a href=${this._editHref(item.id)} @click=${(e: Event) => this._openEditor(item.id, e)}>${item.key}</a>
                                        ${item.parentName ? html`<span class="parent">Parent: <a href=${this._editHref(item.parentId!)} @click=${(e: Event) => this._openEditor(item.parentId!, e)}>${item.parentName}</a></span>` : ""}
                                    </th>
                                    <td class="status">
                                        ${item.isGrouping ? html`<uui-tag title="This item has children and no translations; it may be used for grouping.">Empty parent</uui-tag>`
                                            : missing ? html`<uui-tag color="warning">Missing ${missing} of ${languages.length}</uui-tag>`
                                            : html`<uui-tag color="positive">Complete</uui-tag>`}
                                    </td>
                                    ${languages.map((l) => html`<td>
                                        ${item.values[l.culture]?.trim()
                                            ? html`<div class="value" dir="auto">${item.values[l.culture]}</div>`
                                            : html`<span class="missing">${item.isGrouping ? "No value" : "Missing value"}</span>`}
                                    </td>`)}
                                </tr>`;
                            })}</tbody>
                        </table>
                    </div>
                </uui-box>
                <godmode-pager .currentPage=${result.currentPage} .totalPages=${result.totalPages} .totalItems=${result.totalItems}
                    @page-change=${(e: CustomEvent<number>) => { this._page = e.detail; void this._load(); }}></godmode-pager>
            ` : html`<uui-box><p>${this._search || this._languageId || this._status !== "all"
                ? "No dictionary items match these filters. Try another search, language or status."
                : "No dictionary items to show. Empty parent items are hidden unless included above."}</p></uui-box>`}
        `;
    }

    static override styles = css`
        .filters { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: var(--uui-size-space-4); }
        label { display: block; font-weight: 600; margin-bottom: var(--uui-size-space-2); }
        uui-input, select { width: 100%; }
        select { min-height: 36px; padding: 6px; background: var(--uui-color-surface); color: var(--uui-color-text); border: 1px solid var(--uui-color-border); border-radius: 3px; font: inherit; }
        .group-filter { margin-top: var(--uui-size-space-4); }
        .muted, .parent, .culture, .results { color: var(--uui-color-text-alt); }
        .group-filter p { margin: var(--uui-size-space-2) 0 0; font-size: 12px; }
        .results { margin: var(--uui-size-space-4) 0; }
        .loading { display: flex; align-items: center; gap: 12px; padding: 24px 0; }
        .message { margin-top: var(--uui-size-space-4); }
        .hint { margin: 0 0 var(--uui-size-space-4); }
        .table-scroll { overflow: auto; }
        table { width: 100%; border-collapse: collapse; text-align: left; }
        th, td { padding: 12px; border-bottom: 1px solid var(--uui-color-border); vertical-align: top; }
        thead th { background: var(--uui-color-surface-alt); }
        tbody th { min-width: 180px; max-width: 280px; font-weight: 600; overflow-wrap: anywhere; }
        .status { min-width: 120px; white-space: nowrap; }
        .culture, .parent { display: block; font-size: 12px; font-weight: normal; margin-top: 4px; }
        .value { min-width: 180px; max-width: 380px; max-height: 160px; overflow: auto; white-space: pre-wrap; overflow-wrap: anywhere; }
        .missing { color: var(--uui-color-text-alt); font-style: italic; }
        a { color: var(--uui-color-interactive); text-underline-offset: 3px; }
        a:focus-visible, .table-scroll:focus-visible { outline: 2px solid var(--uui-color-interactive); outline-offset: 2px; }
        .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
        @media (max-width: 1000px) { .filters { grid-template-columns: 1fr; } }
    `;
}

export default GodModeDictionaryBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-dictionary-browser": GodModeDictionaryBrowserElement;
    }
}
