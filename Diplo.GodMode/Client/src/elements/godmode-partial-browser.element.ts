import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { PartialMap, TemplateModel } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { openUsedByModal } from "../shared/used-by-modal";

interface PartialRow extends PartialMap {
    templateId: number;
    templateAlias: string;
    templateName: string;
}

@customElement("godmode-partial-browser")
export class GodModePartialBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _templates: TemplateModel[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _templateFilter = "";
    @state() private _sort: SortState = { column: "name", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            this._templates = await godmodeGet<TemplateModel[]>("templates");
        } finally {
            this._loading = false;
        }
    }

    private _allRows(): PartialRow[] {
        return this._templates.flatMap((t) =>
            (t.partials ?? []).map((p) => ({
                ...p,
                // Server already populates templateId/templateAlias on PartialMap;
                // backfill from the parent template if missing or empty.
                templateId: p.templateId || t.id,
                templateAlias: p.templateAlias || t.alias,
                templateName: t.name
            }))
        );
    }

    private _filtered(): PartialRow[] {
        const q = this._search.trim().toLowerCase();
        const rows = this._allRows().filter((p) => {
            if (q && !p.name.toLowerCase().includes(q)) return false;
            if (this._templateFilter && String(p.templateId) !== this._templateFilter) return false;
            return true;
        });
        return applySort(rows as unknown as Array<Record<string, unknown>>, this._sort) as unknown as PartialRow[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const all = this._allRows();
        const results = this._filtered();
        return html`
            <godmode-page
                heading="Partial Browser"
                description="Browse partial views and see which templates reference them."
                show-reload
                @reload=${() => void this._load()}
            >
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
                                placeholder="Filter partial names"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>In Template</label>
                            <select @change=${(e: Event) => (this._templateFilter = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${this._templates.map((t) => html`<option value=${t.id}>${t.name}</option>`)}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${all.length}</strong></p>
                          <uui-table @sort-change=${this._onSortChange}>
                              <uui-table-head>
                                  <godmode-sort-header column="name" .sort=${this._sort}>Partial Name</godmode-sort-header>
                                  <godmode-sort-header column="templateAlias" .sort=${this._sort}>Template</godmode-sort-header>
                                  <godmode-sort-header column="path" .sort=${this._sort}>Path</godmode-sort-header>
                                  <uui-table-head-cell>Lookup</uui-table-head-cell>
                              </uui-table-head>
                              ${results.map(
                                  (p) => html`
                                      <uui-table-row>
                                          <uui-table-cell><strong>${p.name}</strong></uui-table-cell>
                                          <uui-table-cell>${p.templateAlias}</uui-table-cell>
                                          <uui-table-cell><code>${p.path}</code></uui-table-cell>
                                          <uui-table-cell class="action-cell">
                                              <uui-button compact look="secondary" label="Used by" @click=${(e: Event) => openUsedByModal(this, {
                                                  targetType: "Partial",
                                                  targetKey: p.path || p.name,
                                                  targetName: p.name,
                                                  targetAlias: p.path
                                              }, e)}>
                                                  <uui-icon name="icon-link"></uui-icon>
                                                  Used by
                                              </uui-button>
                                              <godmode-ai-explain-host .subject=${this._explainSubject(p)}></godmode-ai-explain-host>
                                          </uui-table-cell>
                                      </uui-table-row>
                                  `
                              )}
                          </uui-table>
                      `}
            </godmode-page>
        `;
    }

    private _explainSubject(partial: PartialRow) {
        return {
            subjectType: "Umbraco partial view reference",
            title: partial.name,
            data: {
                name: partial.name,
                path: partial.path,
                templateId: partial.templateId,
                templateAlias: partial.templateAlias,
                templateName: partial.templateName
            }
        };
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
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
            font-weight: 600;
        }
        .action-cell {
            display: flex;
            gap: var(--uui-size-space-2);
            justify-content: flex-end;
        }
        .action-cell uui-button,
        .action-cell godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
    `;
}

export default GodModePartialBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-partial-browser": GodModePartialBrowserElement;
    }
}
