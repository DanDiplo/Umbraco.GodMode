import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeDelete, godmodeGet, godmodePost, godmodePut } from "../api/client";
import "../shared";
import type { UmbracoKeyValue } from "../shared/types";

@customElement("godmode-key-value-browser")
export class GodModeKeyValueBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _items: UmbracoKeyValue[] = [];
    @state() private _drafts = new Map<string, string>();
    @state() private _loading = true;
    @state() private _savingKey: string | null = null;
    @state() private _filter = "";
    @state() private _newKey = "";
    @state() private _newValue = "";
    @state() private _creating = false;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            this._items = await godmodeGet<UmbracoKeyValue[]>("key-values");
            this._drafts = new Map(this._items.map((item) => [item.key, item.value ?? ""]));
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): UmbracoKeyValue[] {
        const query = this._filter.trim().toLowerCase();
        if (!query) return this._items;

        return this._items.filter((item) => [item.key, item.value ?? ""].some((value) => value.toLowerCase().includes(query)));
    }

    private _draftFor(key: string): string {
        return this._drafts.get(key) ?? "";
    }

    private _setDraft(key: string, value: string) {
        const drafts = new Map(this._drafts);
        drafts.set(key, value);
        this._drafts = drafts;
    }

    private async _save(item: UmbracoKeyValue) {
        const value = this._draftFor(item.key);
        this._savingKey = item.key;
        try {
            await godmodePut<boolean>("key-values", { value }, { key: item.key });
            await this._load();
        } catch (e) {
            console.error(e);
        } finally {
            this._savingKey = null;
        }
    }

    private async _create() {
        const key = this._newKey.trim();
        if (!key) return;
        if (this._items.some((item) => item.key.toLowerCase() === key.toLowerCase())) {
            alert("A row with this key already exists.");
            return;
        }

        this._creating = true;
        try {
            await godmodePost<boolean>("key-values", undefined, { key, value: this._newValue });
            this._newKey = "";
            this._newValue = "";
            await this._load();
        } catch (e) {
            console.error(e);
        } finally {
            this._creating = false;
        }
    }

    private async _delete(item: UmbracoKeyValue) {
        if (!confirm(`Delete the key value '${item.key}'?`)) return;
        this._savingKey = item.key;
        try {
            await godmodeDelete<boolean>(`key-values?key=${encodeURIComponent(item.key)}`);
            await this._load();
        } catch (e) {
            console.error(e);
        } finally {
            this._savingKey = null;
        }
    }

    override render() {
        const results = this._filtered();

        return html`
            <godmode-page heading="Key Values" description="View and edit rows in the umbracoKeyValue table. Be careful!" show-reload @reload=${() => void this._load()}>
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
                                placeholder="Filter by key or value"
                                .value=${this._filter}
                                @input=${(e: Event) => (this._filter = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                    </div>
                </uui-box>

                <uui-box headline="Add Key Value">
                    <div class="new-row">
                        <div>
                            <label>Key</label>
                            <uui-input
                                placeholder="Key"
                                .value=${this._newKey}
                                ?disabled=${this._creating}
                                @input=${(e: Event) => (this._newKey = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Value</label>
                            <uui-textarea
                                rows="2"
                                auto-height
                                .value=${this._newValue}
                                ?disabled=${this._creating}
                                @input=${(e: Event) => (this._newValue = (e.target as HTMLTextAreaElement).value)}
                            ></uui-textarea>
                        </div>
                        <div class="new-row-actions">
                            <uui-button look="primary" label="Add row" ?disabled=${this._creating || !this._newKey.trim()} @click=${() => void this._create()}>
                                Add row
                            </uui-button>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          <uui-box>
                              <uui-table>
                                  <uui-table-head>
                                      <uui-table-head-cell>Key</uui-table-head-cell>
                                      <uui-table-head-cell>Value</uui-table-head-cell>
                                      <uui-table-head-cell>Updated</uui-table-head-cell>
                                      <uui-table-head-cell>Actions</uui-table-head-cell>
                                  </uui-table-head>
                                  ${results.map((item) => this._renderRow(item))}
                              </uui-table>
                          </uui-box>
                      `}
            </godmode-page>
        `;
    }

    private _renderRow(item: UmbracoKeyValue) {
        const draft = this._draftFor(item.key);
        const changed = draft !== (item.value ?? "");
        const busy = this._savingKey === item.key;

        return html`
            <uui-table-row>
                <uui-table-cell><code title=${item.key}>${item.key}</code></uui-table-cell>
                <uui-table-cell>
                    <uui-textarea
                        rows="2"
                        auto-height
                        .value=${draft}
                        ?disabled=${busy}
                        @input=${(e: Event) => this._setDraft(item.key, (e.target as HTMLTextAreaElement).value)}
                    ></uui-textarea>
                </uui-table-cell>
                <uui-table-cell>${this._formatDate(item.updated)}</uui-table-cell>
                <uui-table-cell>
                    <div class="actions">
                        <uui-button look="primary" label="Save" ?disabled=${busy || !changed} @click=${() => void this._save(item)}>
                            Save
                        </uui-button>
                        <uui-button look="secondary" color="danger" label="Delete" ?disabled=${busy} @click=${() => void this._delete(item)}>
                            Delete
                        </uui-button>
                        <godmode-ai-explain-host .subject=${this._explainSubject(item, draft)}></godmode-ai-explain-host>
                    </div>
                </uui-table-cell>
            </uui-table-row>
        `;
    }

    private _explainSubject(item: UmbracoKeyValue, draft: string) {
        return {
            subjectType: "Umbraco key value",
            title: item.key,
            data: {
                key: item.key,
                savedValue: item.value,
                draftValue: draft,
                hasUnsavedChange: draft !== (item.value ?? ""),
                updated: item.updated,
                table: "umbracoKeyValue"
            }
        };
    }

    private _formatDate(value: string): string {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: minmax(18rem, 32rem);
            gap: var(--uui-size-space-4);
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .filters uui-input,
        .new-row uui-input,
        uui-textarea {
            width: 100%;
        }
        .new-row {
            display: grid;
            grid-template-columns: minmax(16rem, 1fr) minmax(20rem, 2fr) auto;
            gap: var(--uui-size-space-4);
            align-items: end;
        }
        .new-row label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .new-row-actions {
            display: flex;
            align-items: center;
            min-height: 3rem;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        uui-table {
            min-width: 760px;
        }
        uui-table-cell:first-child {
            width: 26%;
        }
        uui-table-cell:nth-child(3) {
            width: 13rem;
            color: var(--uui-color-text-alt);
        }
        uui-table-cell:last-child {
            width: 12rem;
        }
        code {
            white-space: normal;
            overflow-wrap: anywhere;
        }
        .actions {
            display: flex;
            gap: var(--uui-size-space-2);
            flex-wrap: wrap;
        }
        @media (max-width: 900px) {
            .filters,
            .new-row {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeKeyValueBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-key-value-browser": GodModeKeyValueBrowserElement;
    }
}
