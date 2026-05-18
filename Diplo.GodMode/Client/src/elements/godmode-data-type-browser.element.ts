import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { DataTypeMap, ReferenceEdge } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { truncate, uniqueBy } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openUsedByModal } from "../shared/used-by-modal";
import { openEvidenceDrawer } from "../shared/evidence-drawer";

type TriState = "any" | "yes" | "no";

@customElement("godmode-data-type-browser")
export class GodModeDataTypeBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _items: DataTypeMap[] = [];
    @state() private _editors: Array<{ alias: string; name: string }> = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _editorFilter = "";
    @state() private _dbTypeFilter = "";
    @state() private _usedFilter: TriState = "any";
    @state() private _nestedUsedFilter: TriState = "any";
    @state() private _sort: SortState = { column: "name", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
        void this._loadEditors();
    }

    private async _load() {
        this._loading = true;
        try {
            this._items = await godmodeGet<DataTypeMap[]>("data-types-status");
        } finally {
            this._loading = false;
        }
    }

    private async _loadEditors() {
        try {
            const data = await godmodeGet<Array<{ alias: string; name: string }>>("property-editors");
            this._editors = data;
        } catch {
            // non-fatal
        }
    }

    private _filtered(): DataTypeMap[] {
        const q = this._search.trim().toLowerCase();
        const editor = this._editorFilter.toLowerCase();
        const db = this._dbTypeFilter;
        const used = this._usedFilter;
        const nestedUsed = this._nestedUsedFilter;

        const matched = this._items.filter((d) => {
            if (q) {
                const hit =
                    d.name.toLowerCase().includes(q) ||
                    String(d.id) === q ||
                    (d.udi ?? "").toLowerCase().includes(q);
                if (!hit) return false;
            }
            if (editor && !d.alias.toLowerCase().includes(editor)) return false;
            if (db && d.dbType !== db) return false;
            if (used === "yes" && !d.isUsed) return false;
            if (used === "no" && d.isUsed) return false;
            if (nestedUsed === "yes" && !d.isNestedUsed) return false;
            if (nestedUsed === "no" && d.isNestedUsed) return false;
            return true;
        });

        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as DataTypeMap[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const dbTypes = uniqueBy(this._items, "dbType").map((x) => x.dbType).filter(Boolean);
        const results = this._filtered();

        return html`
            <godmode-page
                heading="DataType Browser"
                description="Browse data types, see whether they are used, and by which editor."
                show-reload
                @reload=${() => void this._load()}
            >
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
                                placeholder="Filter by name, id or udi"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label for="editor">Editor</label>
                            <select
                                id="editor"
                                .value=${this._editorFilter}
                                @change=${(e: Event) => (this._editorFilter = (e.target as HTMLSelectElement).value)}
                            >
                                <option value="">Any</option>
                                ${this._editors.map(
                                    (ed) => html`<option value=${ed.alias}>${ed.alias}</option>`
                                )}
                            </select>
                        </div>
                        <div>
                            <label for="db">DB Type</label>
                            <select
                                id="db"
                                .value=${this._dbTypeFilter}
                                @change=${(e: Event) => (this._dbTypeFilter = (e.target as HTMLSelectElement).value)}
                            >
                                <option value="">Any</option>
                                ${dbTypes.map((d) => html`<option value=${d}>${d}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label for="used">Is Used?</label>
                            <select
                                id="used"
                                .value=${this._usedFilter}
                                @change=${(e: Event) => (this._usedFilter = (e.target as HTMLSelectElement).value as TriState)}
                            >
                                <option value="any">Any</option>
                                <option value="yes">Yes</option>
                                <option value="no">No</option>
                            </select>
                        </div>
                        <div>
                            <label for="nested-used">Nested in Blocks?</label>
                            <select
                                id="nested-used"
                                .value=${this._nestedUsedFilter}
                                @change=${(e: Event) => (this._nestedUsedFilter = (e.target as HTMLSelectElement).value as TriState)}
                            >
                                <option value="any">Any</option>
                                <option value="yes">Yes</option>
                                <option value="no">No</option>
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results">
                              <strong>${results.length}</strong> / <strong>${this._items.length}</strong>
                          </p>
                          <uui-table @sort-change=${this._onSortChange}>
                              <uui-table-head>
                                  <godmode-sort-header column="name" .sort=${this._sort}>Name</godmode-sort-header>
                                  <godmode-sort-header column="alias" .sort=${this._sort}>Editor Alias</godmode-sort-header>
                                  <godmode-sort-header column="dbType" .sort=${this._sort}>Database Type</godmode-sort-header>
                                  <godmode-sort-header column="isUsed" .sort=${this._sort}>Used by DocType?</godmode-sort-header>
                                  <godmode-sort-header column="isNestedUsed" .sort=${this._sort}>Nested in Blocks?</godmode-sort-header>
                                  <godmode-sort-header column="updateDate" .sort=${this._sort}>Last Updated</godmode-sort-header>
                                  <uui-table-head-cell>Lookup</uui-table-head-cell>
                                  <godmode-sort-header column="id" .sort=${this._sort}>Id</godmode-sort-header>
                              </uui-table-head>
                              ${results.map(
                                  (d) => html`
                                      <uui-table-row>
                                          <uui-table-cell
                                              ><a href=${editUrl("dataType", d.udi)} @click=${(e: Event) => openEditorModal(this, "dataType", d.udi, e)}
                                                  ><strong>${d.name}</strong></a
                                              ></uui-table-cell
                                          >
                                          <uui-table-cell><code>${d.alias}</code></uui-table-cell>
                                          <uui-table-cell><code>${d.dbType}</code></uui-table-cell>
                                          <uui-table-cell><godmode-yes-no .value=${d.isUsed}></godmode-yes-no></uui-table-cell>
                                          <uui-table-cell><godmode-yes-no .value=${d.isNestedUsed}></godmode-yes-no></uui-table-cell>
                                          <uui-table-cell><small>${truncate(d.updateDate, 22)}</small></uui-table-cell>
                                          <uui-table-cell class="action-cell">
                                              <uui-button compact look="secondary" label="Impact" @click=${(e: Event) => void this._openImpact(d, e)}>
                                                  <uui-icon name="icon-alert"></uui-icon>
                                                  Impact
                                              </uui-button>
                                              ${this._renderUsedByAction(d)}
                                              <godmode-ai-explain-host .subjectProvider=${() => this._explainSubject(d)}></godmode-ai-explain-host>
                                          </uui-table-cell>
                                          <uui-table-cell>
                                              <div><strong>${d.id}</strong></div>
                                              <code>${d.udi}</code>
                                          </uui-table-cell>
                                      </uui-table-row>
                                  `
                              )}
                          </uui-table>

                          <uui-box headline="Important" style="margin-top: var(--uui-size-space-4)">
                              <p>
                                  Direct usage checks document and media type properties. Nested usage is inferred from
                                  Block List, Block Grid, Single Block and Rich Text block configurations.
                              </p>
                          </uui-box>
                      `}
            </godmode-page>
        `;
    }

    private async _openImpact(d: DataTypeMap, e: Event) {
        e.preventDefault();
        e.stopPropagation();

        const [usedBy, uses] = await Promise.all([
            godmodeGet<ReferenceEdge[]>("references/used-by", { targetType: "Data Type", targetKey: d.udi }),
            godmodeGet<ReferenceEdge[]>("references/uses", { sourceType: "Data Type", sourceKey: d.udi })
        ]);

        const directUsage = usedBy.filter((edge) => edge.relation === "uses data type");
        const nestedUsage = usedBy.filter((edge) => edge.relation === "uses nested data type");
        const blockConfigurationUsage = usedBy.filter((edge) => edge.relation.startsWith("configures"));
        const appearsUnused = !d.isUsed && !d.isNestedUsed && usedBy.length === 0;

        openEvidenceDrawer(
            this,
            {
                title: `Impact: ${d.name}`,
                subtitle: appearsUnused
                    ? "No direct, nested, or reference graph usage was found."
                    : "Review where this data type is used before changing or deleting it.",
                summary: [
                    { label: "Editor", value: d.alias },
                    { label: "Database Type", value: d.dbType },
                    { label: "Direct Usage", value: d.isUsed },
                    { label: "Nested Block Usage", value: d.isNestedUsed },
                    { label: "Used By Edges", value: usedBy.length },
                    { label: "Uses Edges", value: uses.length }
                ],
                sections: [
                    {
                        heading: "Impact Summary",
                        items: {
                            appearsUnused,
                            caution: appearsUnused
                                ? "This looks like a low-risk cleanup candidate, but custom code can still reference data type aliases or keys outside the reference graph."
                                : "Changing configuration can affect editors and stored values for every property that uses this data type.",
                            recommendedCheck: d.isNestedUsed
                                ? "Review nested block usage carefully; changes may affect element types embedded inside Block List, Block Grid, Single Block, or Rich Text configurations."
                                : "Review direct property usage and any custom code references before changing storage/editor configuration."
                        }
                    },
                    {
                        heading: "Direct Property Usage",
                        description: "Document, media, or member type properties that directly use this data type.",
                        items: directUsage
                    },
                    {
                        heading: "Nested Block Usage",
                        description: "Element type properties that use this data type inside supported block editor configurations.",
                        items: nestedUsage
                    },
                    {
                        heading: "Block Configuration Usage",
                        description: "Block editor configuration references involving this data type.",
                        items: blockConfigurationUsage
                    },
                    {
                        heading: "All Used By References",
                        description: "Complete reference graph edges where this data type is the target.",
                        items: usedBy
                    },
                    {
                        heading: "Uses References",
                        description: "Reference graph edges where this data type is the source.",
                        items: uses
                    }
                ]
            },
            e
        );
    }

    private _renderUsedByAction(d: DataTypeMap) {
        if (!d.isUsed && !d.isNestedUsed) {
            return html`
                <uui-button compact look="secondary" label="Not used" disabled title="No direct document/media/member type usage or supported block usage was found.">
                    <uui-icon name="icon-link"></uui-icon>
                    Not used
                </uui-button>
            `;
        }

        return html`
            <uui-button compact look="secondary" label="Used by" @click=${(e: Event) => openUsedByModal(this, {
                targetType: "Data Type",
                targetKey: d.udi,
                targetName: d.name,
                targetAlias: d.alias
            }, e)}>
                <uui-icon name="icon-link"></uui-icon>
                Used by
            </uui-button>
        `;
    }

    private async _explainSubject(d: DataTypeMap) {
        const [usedBy, uses] = await Promise.all([
            godmodeGet<ReferenceEdge[]>("references/used-by", { targetType: "Data Type", targetKey: d.udi }),
            godmodeGet<ReferenceEdge[]>("references/uses", { sourceType: "Data Type", sourceKey: d.udi })
        ]);

        return {
            subjectType: "Umbraco data type",
            title: `${d.name} (${d.alias})`,
            data: {
                id: d.id,
                name: d.name,
                editorAlias: d.alias,
                databaseType: d.dbType,
                udi: d.udi,
                isUsedByDocumentOrMediaTypes: d.isUsed,
                isNestedUsedInBlocks: d.isNestedUsed,
                updateDate: d.updateDate
            },
            context: {
                usedBy,
                uses,
                detailSources: ["data-types-status", "God Mode reference graph"]
            }
        };
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: 2fr repeat(4, 1fr);
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
        }
        a:hover {
            text-decoration: underline;
        }
        .action-cell {
            display: flex;
            gap: var(--uui-size-space-2);
            justify-content: flex-end;
            min-width: 18rem;
        }
        .action-cell uui-button,
        .action-cell godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
    `;
}

export default GodModeDataTypeBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-data-type-browser": GodModeDataTypeBrowserElement;
    }
}
