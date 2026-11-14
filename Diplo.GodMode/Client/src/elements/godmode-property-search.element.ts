import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import { editUrl, openEditorModal } from "../shared/edit-links";
import type { Lang } from "../shared/types";
import "../shared";

interface Choice { alias: string; name: string; }
interface SearchOptions { available: boolean; message: string | null; properties: Choice[]; documentTypes: Choice[]; }
interface Match { propertyAlias: string; propertyName: string; culture: string | null; snippet: string; }
interface SearchItem { id: string; key: string; name: string; documentType: string; published: boolean | null; publishedUrl: string | null; matches: Match[]; matchingFieldCount: number; }
interface SearchResults { totalItems: number; currentPage: number; totalPages: number; items: SearchItem[]; }
interface SearchQuery { text: string; mode: string; property: string; documentType: string; culture: string; }

@customElement("godmode-property-search")
export class GodModePropertySearchElement extends UmbElementMixin(LitElement) {
    @state() private _options?: SearchOptions;
    @state() private _languages: Lang[] = [];
    @state() private _initialising = true;
    @state() private _loading = false;
    @state() private _error = "";
    @state() private _text = "";
    @state() private _mode = "words";
    @state() private _property = "";
    @state() private _documentType = "";
    @state() private _culture = "";
    @state() private _active?: SearchQuery;
    @state() private _result?: SearchResults;
    private _request = 0;

    override connectedCallback(): void { super.connectedCallback(); void this._initialise(); }
    override disconnectedCallback(): void { this._request++; super.disconnectedCallback(); }

    private async _initialise() {
        this._initialising = true;
        this._error = "";
        try {
            [this._options, this._languages] = await Promise.all([
                godmodeGet<SearchOptions>("property-search/options"), godmodeGet<Lang[]>("languages")
            ]);
        } catch { this._error = "Could not load search options. Please try reloading."; }
        finally { this._initialising = false; }
    }

    private _submit(e?: Event) {
        e?.preventDefault();
        if (!this._text.trim()) { this._clearResults(); return; }
        const words = this._text.trim().split(/\s+/).filter(Boolean);
        if (this._text.trim().length < 2 || this._text.length > 200 || words.length > 8 || (this._mode === "prefix" && words.length !== 1)) {
            this._clearResults(false);
            this._error = "Enter 2–200 characters (up to eight words). Word prefix searches accept one word.";
            return;
        }
        this._active = { text: this._text.trim(), mode: this._mode, property: this._property, documentType: this._documentType, culture: this._culture };
        this._result = undefined;
        void this._search(1);
    }

    private _clearResults(clearText = true) {
        this._request++;
        if (clearText) this._text = "";
        this._active = undefined;
        this._result = undefined;
        this._loading = false;
        this._error = "";
    }

    private _textChanged(e: Event) {
        this._text = (e.target as HTMLInputElement).value;
        if (!this._text.trim()) this._clearResults();
    }

    private async _search(page: number, keepResults = false) {
        if (!this._active) return;
        const request = ++this._request;
        this._loading = !keepResults;
        this._error = "";
        try {
            const result = await godmodeGet<SearchResults>("property-search", { ...this._active, page, pageSize: 20 });
            if (request === this._request) this._result = result;
        } catch {
            if (request === this._request) this._error = "The search could not complete. Try narrowing the filters, or check InternalIndex in Examine Management.";
        } finally { if (request === this._request) this._loading = false; }
    }

    private _open(item: SearchItem, e: Event) {
        openEditorModal(this, "content", item.key, e, () => void this._search(this._result?.currentPage ?? 1, true));
    }

    private _highlight(value: string) {
        const terms = this._active?.text.split(/\s+/).filter(Boolean) ?? [];
        if (!terms.length) return value;
        const pattern = terms.map(t => t.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|");
        return value.split(new RegExp(`(${pattern})`, "gi")).map((part, i) => i % 2 ? html`<mark>${part}</mark>` : part);
    }

    override render() {
        return html`
            <godmode-page heading="Property Search" icon="icon-search"
                description="Find text in indexed content properties, including supported blocks and rich text."
                show-reload @reload=${() => { void this._initialise(); if (this._active) void this._search(this._result?.currentPage ?? 1); }}>
                <uui-box>
                    <form autocomplete="off" @submit=${this._submit}>
                        <div class="search-line">
                            <div class="search-input">
                                <label for="search">Search property values</label>
                                <uui-input id="search" label="Search property values" placeholder="e.g. embedded rich text" maxlength="200" autocomplete="off"
                                    .value=${this._text} @input=${this._textChanged}
                                    @keydown=${(e: KeyboardEvent) => { if (e.key === "Enter") this._submit(e); }}></uui-input>
                            </div>
                            <uui-button type="submit" look="primary" color="positive" label="Search properties"
                                ?disabled=${this._initialising || !this._options?.available || this._loading}>Search</uui-button>
                            ${this._text || this._result || this._error ? html`<uui-button type="button" look="secondary" label="Clear search" @click=${() => this._clearResults()}>Clear</uui-button>` : ""}
                        </div>
                        <div class="filters">
                            <div><label for="mode">Match</label><select id="mode" .value=${this._mode} @change=${(e: Event) => this._mode = (e.target as HTMLSelectElement).value}>
                                <option value="words">All words in a property</option><option value="phrase">Phrase</option><option value="prefix">Word prefix</option>
                            </select></div>
                            <div><label for="property">Property</label><select id="property" .value=${this._property} @change=${(e: Event) => this._property = (e.target as HTMLSelectElement).value}>
                                <option value="">All properties</option>${this._options?.properties.map(p => html`<option value=${p.alias}>${p.name} (${p.alias})</option>`)}
                            </select></div>
                            <div><label for="document-type">Document type</label><select id="document-type" .value=${this._documentType} @change=${(e: Event) => this._documentType = (e.target as HTMLSelectElement).value}>
                                <option value="">All document types</option>${this._options?.documentTypes.map(t => html`<option value=${t.alias}>${t.name} (${t.alias})</option>`)}
                            </select></div>
                            <div><label for="culture">Language</label><select id="culture" .value=${this._culture} @change=${(e: Event) => this._culture = (e.target as HTMLSelectElement).value}>
                                <option value="">All languages</option>${this._languages.map(l => html`<option value=${l.culture}>${l.name}</option>`)}
                            </select></div>
                        </div>
                        <p class="muted help">${this._mode === "prefix" ? "Enter one word beginning, such as embed. " : ""}Language filters include shared properties. Change filters and select Search to apply them.</p>
                    </form>
                </uui-box>

                <div class="scope"><uui-tag>InternalIndex</uui-tag><span>Saved content, including drafts. Recycle-bin items are excluded. Indexed text may omit link URLs and other editor values.</span></div>
                ${this._initialising ? html`<p role="status">Loading search options…</p>` : ""}
                ${this._options && !this._options.available ? html`<uui-box><p role="status">${this._options.message}</p></uui-box>` : ""}
                ${this._error ? html`<uui-box class="error"><p role="alert">${this._error}</p></uui-box>` : ""}
                ${this._loading ? html`<div class="loading" role="status"><uui-loader></uui-loader>Searching indexed property values…</div>`
                    : this._result ? this._renderResults(this._result)
                    : !this._initialising && this._options?.available ? html`<uui-box class="empty"><umb-icon name="icon-search"></umb-icon><h2>Find the content behind a value</h2><p>Search a word or phrase, then narrow by property, document type or language.</p><p class="muted">Block text is grouped under its containing property. This searches the index, rather than every stored value.</p></uui-box>` : ""}
            </godmode-page>
        `;
    }

    private _renderResults(result: SearchResults) {
        return html`
            <p class="results" role="status"><strong>${result.totalItems}</strong> matching ${result.totalItems === 1 ? "content item" : "content items"} for <strong>“${this._active?.text}”</strong></p>
            ${!result.items.length ? html`<uui-box><h2>No indexed matches</h2><p>Try another word, a word prefix, or broader filters. A missing index match does not prove the value is absent from saved content.</p></uui-box>` : ""}
            ${result.items.map(item => html`
                <uui-box class="result-card">
                    <div class="result-heading">
                        <div><h2><a href=${editUrl("content", item.key)} @click=${(e: Event) => this._open(item, e)}>${item.name}</a></h2>
                            <div class="meta"><code>${item.documentType}</code><span>ID ${item.id}</span><span>${item.published === null ? "Publication state unknown" : item.published ? "Published" : "Unpublished"}</span></div>
                        </div>
                        ${item.publishedUrl ? html`<uui-button look="secondary" href=${item.publishedUrl} target="_blank" rel="noopener noreferrer"
                            label=${`View published page: ${item.name}`} title="Open published page in a new tab">View <span aria-hidden="true">↗</span></uui-button>` : ""}
                    </div>
                    ${item.matches.map(match => html`<div class="match">
                        <div class="match-heading"><strong>${match.propertyName}</strong><code>${match.propertyAlias}</code><uui-tag>${match.culture ?? "Shared"}</uui-tag></div>
                        <div class="snippet" dir="auto">${this._highlight(match.snippet)}</div>
                    </div>`)}
                    ${item.matchingFieldCount > item.matches.length ? html`<p class="muted">${item.matchingFieldCount - item.matches.length} more matching indexed values. Open the editor to inspect this content.</p>` : ""}
                    ${!item.matches.length ? html`<p class="muted">The index matched this item, but its analyzer did not produce a matching display snippet. Open the editor to inspect the property values.</p>` : ""}
                </uui-box>
            `)}
            ${result.totalItems ? html`<godmode-pager .currentPage=${result.currentPage} .totalPages=${result.totalPages} .totalItems=${result.totalItems}
                @page-change=${(e: CustomEvent<number>) => void this._search(e.detail)}></godmode-pager>` : ""}
        `;
    }

    static override styles = css`
        .search-line { display: flex; gap: var(--uui-size-space-4); align-items: flex-end; }
        .search-input { display: flex; flex-direction: column; flex: 1; min-width: 0; }
        label { display: block; font-weight: 600; margin-bottom: 6px; }
        uui-input, select { width: 100%; }
        select { min-height: 36px; padding: 6px; font: inherit; color: var(--uui-color-text); background: var(--uui-color-surface); border: 1px solid var(--uui-color-border); border-radius: 3px; }
        .filters { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: var(--uui-size-space-4); margin-top: var(--uui-size-space-4); }
        .help { margin: 12px 0 0; font-size: 12px; }
        .scope, .meta, .match-heading { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
        .scope { margin: var(--uui-size-space-4) 0; color: var(--uui-color-text-alt); font-size: 12px; }
        .scope span { flex: 1; min-width: 200px; }
        .loading { display: flex; align-items: center; gap: 12px; margin: 24px 0; }
        .empty { text-align: center; }
        .empty umb-icon { font-size: 28px; color: var(--uui-color-text-alt); }
        h2 { font-size: 18px; margin: 0 0 8px; }
        .result-card, .error { margin-bottom: var(--uui-size-space-4); }
        .result-heading { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; }
        .meta, .muted { color: var(--uui-color-text-alt); }
        .meta { font-size: 12px; }
        a { color: var(--uui-color-interactive); text-underline-offset: 3px; }
        .match { border-top: 1px solid var(--uui-color-border); padding-top: 14px; margin-top: 14px; }
        .match-heading { margin-bottom: 8px; }
        code { font-size: 12px; overflow-wrap: anywhere; }
        .snippet { white-space: pre-wrap; overflow-wrap: anywhere; line-height: 1.6; }
        mark { background: var(--uui-color-warning); color: var(--uui-color-warning-contrast); padding: 0 2px; border-radius: 2px; }
        @media(max-width: 1200px) { .filters { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
        @media(max-width: 700px) { .filters { grid-template-columns: 1fr; } }
    `;
}

export default GodModePropertySearchElement;
declare global { interface HTMLElementTagNameMap { "godmode-property-search": GodModePropertySearchElement; } }
