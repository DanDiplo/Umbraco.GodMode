import { LitElement, css, customElement, html, state, svg } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ContentTypeMap, ContentVariation, ReferenceEdge, UsageModel } from "../shared/types";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openUsedByModal } from "../shared/used-by-modal";

type TriState = "any" | "yes" | "no";

/** v17 may emit ContentVariation enums as numbers; map to strings for UI / filtering. */
function variesByLabel(v: ContentVariation): string {
    if (typeof v === "string") return v;
    switch (v) {
        case 0:
            return "Nothing";
        case 1:
            return "Culture";
        case 2:
            return "Segment";
        case 3:
            return "CultureAndSegment";
        default:
            return String(v);
    }
}

@customElement("godmode-doc-type-browser")
export class GodModeDocTypeBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _items: ContentTypeMap[] = [];
    @state() private _propertyGroups: string[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _hasTemplate: TriState = "any";
    @state() private _isListView: TriState = "any";
    @state() private _isElement: TriState = "any";
    @state() private _allowedAtRoot: TriState = "any";
    @state() private _deliveryApiExposed: TriState = "any";
    @state() private _variesBy = "any";
    @state() private _propertyGroup = "";
    @state() private _propertyQuery = "";
    @state() private _open = new Set<number>();
    @state() private _detailViews = new Map<number, "visual" | "details">();

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            const [items, groups] = await Promise.all([
                godmodeGet<ContentTypeMap[]>("content-type-map"),
                godmodeGet<string[]>("property-groups")
            ]);
            this._items = items;
            this._propertyGroups = groups;
        } finally {
            this._loading = false;
        }
    }

    private _toggle(id: number) {
        const next = new Set(this._open);
        next.has(id) ? next.delete(id) : next.add(id);
        this._open = next;
    }

    private _setDetailView(id: number, view: "visual" | "details", e?: Event) {
        e?.stopPropagation();
        const next = new Map(this._detailViews);
        next.set(id, view);
        this._detailViews = next;
    }

    private _matchTri(value: boolean, filter: TriState): boolean {
        if (filter === "any") return true;
        return filter === "yes" ? value : !value;
    }

    private _filtered(): ContentTypeMap[] {
        const q = this._search.trim().toLowerCase();
        const propQ = this._propertyQuery.trim().toLowerCase();
        return this._items.filter((ct) => {
            if (q && ![ct.name, ct.alias, ct.udi ?? ""].some((v) => v.toLowerCase().includes(q))) return false;
            if (!this._matchTri(!!ct.templates?.length, this._hasTemplate)) return false;
            if (!this._matchTri(ct.isListView, this._isListView)) return false;
            if (!this._matchTri(ct.isElement, this._isElement)) return false;
            if (!this._matchTri(ct.allowedAtRoot, this._allowedAtRoot)) return false;
            if (!this._matchTri(ct.deliveryApiExposed, this._deliveryApiExposed)) return false;
            if (this._variesBy !== "any" && variesByLabel(ct.variesBy) !== this._variesBy) return false;
            if (this._propertyGroup && !ct.propertyGroups?.includes(this._propertyGroup)) return false;
            if (propQ) {
                const all = [...(ct.properties ?? []), ...(ct.compositionProperties ?? [])];
                if (!all.some((p) => p.name.toLowerCase().includes(propQ) || p.alias.toLowerCase().includes(propQ))) return false;
            }
            return true;
        });
    }

    override render() {
        const results = this._filtered();
        return html`
            <godmode-page
                heading="Document Type Browser"
                description="Browse, filter and search document types."
                show-reload
                @reload=${() => void this._load()}
            >
                <uui-box headline="Search Filters">
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by name, alias or udi"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Has Template?</label>
                            ${this._tri("hasTemplate", this._hasTemplate, (v) => (this._hasTemplate = v))}
                        </div>
                        <div>
                            <label>List View?</label>
                            ${this._tri("isListView", this._isListView, (v) => (this._isListView = v))}
                        </div>
                        <div>
                            <label>Element Type?</label>
                            ${this._tri("isElement", this._isElement, (v) => (this._isElement = v))}
                        </div>
                        <div>
                            <label>Allowed at Root?</label>
                            ${this._tri("allowedAtRoot", this._allowedAtRoot, (v) => (this._allowedAtRoot = v))}
                        </div>
                        <div>
                            <label>Delivery API?</label>
                            ${this._tri("deliveryApiExposed", this._deliveryApiExposed, (v) => (this._deliveryApiExposed = v))}
                        </div>
                        <div>
                            <label>Varies By</label>
                            <select @change=${(e: Event) => (this._variesBy = (e.target as HTMLSelectElement).value)}>
                                <option value="any">Any</option>
                                <option value="Nothing">Nothing</option>
                                <option value="Culture">Culture</option>
                                <option value="Segment">Segment</option>
                                <option value="CultureAndSegment">Culture and Segment</option>
                            </select>
                        </div>
                        <div>
                            <label>Has Group</label>
                            <select @change=${(e: Event) => (this._propertyGroup = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${this._propertyGroups.map((g) => html`<option value=${g}>${g}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Property Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by property name/alias"
                                .value=${this._propertyQuery}
                                @input=${(e: Event) => (this._propertyQuery = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          ${results.map((ct) => this._renderItem(ct))}
                      `}
            </godmode-page>
        `;
    }

    private _tri(id: string, value: TriState, set: (v: TriState) => void) {
        return html`
            <select id=${id} @change=${(e: Event) => set((e.target as HTMLSelectElement).value as TriState)}>
                <option value="any" ?selected=${value === "any"}>Any</option>
                <option value="yes" ?selected=${value === "yes"}>Yes</option>
                <option value="no" ?selected=${value === "no"}>No</option>
            </select>
        `;
    }

    private _renderItem(ct: ContentTypeMap) {
        const isOpen = this._open.has(ct.id);
        return html`
            <uui-box style="margin-top: var(--uui-size-space-3)">
                <div class="row" @click=${() => this._toggle(ct.id)}>
                    <h4>
                        <umb-icon name=${ct.icon || "icon-document"}></umb-icon>
                        <strong>${ct.name}</strong>
                        <small>(${ct.alias})</small>
                        ${ct.isElement ? html`<small class="badge">Element</small>` : ""}
                        ${ct.deliveryApiExposed ? html`<small class="badge api">API</small>` : ""}
                        ${ct.deliveryApiSensitiveAlias ? html`<small class="badge warning">Sensitive alias</small>` : ""}
                        ${(() => {
                            const v = variesByLabel(ct.variesBy);
                            return v && v !== "Nothing" ? html`<small class="badge">Varies by ${v}</small>` : "";
                        })()}
                        ${ct.isListView ? html`<small class="badge">List</small>` : ""}
                    </h4>
                    <span class="actions" @click=${(e: Event) => e.stopPropagation()}>
                        <uui-button compact look="secondary" label="Used by" @click=${(e: Event) => openUsedByModal(this, {
                            targetType: ct.isElement ? "Element Type" : "Document Type",
                            targetKey: ct.udi,
                            targetName: ct.name,
                            targetAlias: ct.alias
                        }, e)}>
                            <uui-icon name="icon-link"></uui-icon>
                            Used by
                        </uui-button>
                        <uui-button compact look="secondary" label="Edit" href=${editUrl("documentType", ct.udi)} @click=${(e: Event) => openEditorModal(this, "documentType", ct.udi, e)}>
                            <uui-icon name="icon-edit"></uui-icon>
                            Edit
                        </uui-button>
                        <godmode-ai-explain-host .subjectProvider=${() => this._explainSubject(ct)}></godmode-ai-explain-host>
                    </span>
                </div>
                ${isOpen ? this._renderDetail(ct) : ""}
            </uui-box>
        `;
    }

    private async _explainSubject(ct: ContentTypeMap) {
        const allProperties = [...(ct.properties ?? []), ...(ct.compositionProperties ?? [])];
        const entityType = ct.isElement ? "Element Type" : "Document Type";
        const [usedBy, uses, usage] = await Promise.all([
            godmodeGet<ReferenceEdge[]>("references/used-by", { targetType: entityType, targetKey: ct.udi }),
            godmodeGet<ReferenceEdge[]>("references/uses", { sourceType: entityType, sourceKey: ct.udi }),
            ct.isElement ? Promise.resolve([] as UsageModel[]) : godmodeGet<UsageModel[]>("content-usage", { id: ct.id })
        ]);

        return {
            subjectType: ct.isElement ? "Umbraco element type" : "Umbraco document type",
            title: `${ct.name} (${ct.alias})`,
            data: {
                id: ct.id,
                name: ct.name,
                alias: ct.alias,
                udi: ct.udi,
                description: ct.description,
                icon: ct.icon,
                isElement: ct.isElement,
                isListView: ct.isListView,
                allowedAtRoot: ct.allowedAtRoot,
                deliveryApiExposed: ct.deliveryApiExposed,
                deliveryApiExposure: ct.deliveryApiExposure,
                deliveryApiSensitiveAlias: ct.deliveryApiSensitiveAlias,
                variesBy: variesByLabel(ct.variesBy),
                templateCount: ct.templates?.length ?? 0,
                templates: (ct.templates ?? []).map((template) => ({
                    name: template.name,
                    alias: template.alias,
                    udi: template.udi
                })),
                compositionCount: ct.compositions?.length ?? 0,
                compositions: (ct.compositions ?? []).map((composition) => ({
                    name: composition.name,
                    alias: composition.alias,
                    udi: composition.udi
                })),
                nativePropertyCount: ct.properties?.length ?? 0,
                inheritedPropertyCount: ct.compositionProperties?.length ?? 0,
                propertyGroups: ct.propertyGroups ?? [],
                properties: allProperties.slice(0, 30).map((property) => ({
                    name: property.name,
                    alias: property.alias,
                    editorAlias: property.editorAlias,
                    variesBy: property.variesBy,
                    storageType: property.storageType,
                    inherited: (ct.compositionProperties ?? []).some((p) => p.alias === property.alias)
                })),
                propertySampleLimit: 30,
                totalPropertyCount: allProperties.length
            },
            context: {
                usedBy,
                uses,
                contentUsage: usage,
                detailSources: ["content-type-map", "content-usage", "God Mode reference graph"]
            }
        };
    }

    private _renderDetail(ct: ContentTypeMap) {
        const view = this._detailViews.get(ct.id) ?? "visual";
        return html`
            <div class="meta">
                <p>${ct.description ?? ""}</p>
                <small><span class="label">${ct.id}</span> <code>${ct.udi}</code></small>
            </div>
            <div class="detail-tabs" role="tablist" aria-label=${`${ct.name} detail views`}>
                <uui-button compact look=${view === "visual" ? "primary" : "secondary"} label="Visual" @click=${(e: Event) => this._setDetailView(ct.id, "visual", e)}>
                    <uui-icon name="icon-molecular-network"></uui-icon>
                    Visual
                </uui-button>
                <uui-button compact look=${view === "details" ? "primary" : "secondary"} label="Details" @click=${(e: Event) => this._setDetailView(ct.id, "details", e)}>
                    <uui-icon name="icon-list"></uui-icon>
                    Details
                </uui-button>
            </div>
            ${view === "visual" ? this._renderSchemaMap(ct) : this._renderDetailTables(ct)}
        `;
    }

    private _renderDetailTables(ct: ContentTypeMap) {
        return html`
            ${ct.templates?.length
                ? html`
                      <h5>Templates</h5>
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Alias</uui-table-head-cell>
                              <uui-table-head-cell>Kind</uui-table-head-cell>
                              <uui-table-head-cell>Properties</uui-table-head-cell>
                              <uui-table-head-cell>Groups</uui-table-head-cell>
                              <uui-table-head-cell>Varies By</uui-table-head-cell>
                              <uui-table-head-cell>Composes</uui-table-head-cell>
                          </uui-table-head>
                          ${ct.templates.map(
                              (t) => html`<uui-table-row>
                                  <uui-table-cell
                                      ><a href=${editUrl("template", t.udi)} @click=${(e: Event) => openEditorModal(this, "template", t.udi, e)}
                                          >${t.name}</a
                                      ></uui-table-cell
                                  >
                                  <uui-table-cell><code>${t.alias}</code></uui-table-cell>
                                  <uui-table-cell>Template</uui-table-cell>
                                  <uui-table-cell></uui-table-cell>
                                  <uui-table-cell></uui-table-cell>
                                  <uui-table-cell></uui-table-cell>
                                  <uui-table-cell></uui-table-cell>
                              </uui-table-row>`
                          )}
                      </uui-table>
                  `
                : ""}
            ${ct.properties?.length
                ? html`
                      <h5>Native Properties</h5>
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Alias</uui-table-head-cell>
                              <uui-table-head-cell>Editor</uui-table-head-cell>
                              <uui-table-head-cell>Varies By</uui-table-head-cell>
                              <uui-table-head-cell>Stored As</uui-table-head-cell>
                          </uui-table-head>
                          ${ct.properties.map(
                              (p) => html`<uui-table-row>
                                  <uui-table-cell title=${p.description ?? ""}>${p.name}</uui-table-cell>
                                  <uui-table-cell><code>${p.alias}</code></uui-table-cell>
                                  <uui-table-cell><code>${p.editorAlias}</code></uui-table-cell>
                                  <uui-table-cell>${p.variesBy}</uui-table-cell>
                                  <uui-table-cell><code>${p.storageType}</code></uui-table-cell>
                              </uui-table-row>`
                          )}
                      </uui-table>
                  `
                : ""}
            ${ct.compositionProperties?.length
                ? html`
                      <h5>Inherited Properties</h5>
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Alias</uui-table-head-cell>
                              <uui-table-head-cell>Editor</uui-table-head-cell>
                              <uui-table-head-cell>Varies By</uui-table-head-cell>
                              <uui-table-head-cell>Stored As</uui-table-head-cell>
                          </uui-table-head>
                          ${ct.compositionProperties.map(
                              (p) => html`<uui-table-row>
                                  <uui-table-cell title=${p.description ?? ""}>${p.name}</uui-table-cell>
                                  <uui-table-cell><code>${p.alias}</code></uui-table-cell>
                                  <uui-table-cell><code>${p.editorAlias}</code></uui-table-cell>
                                  <uui-table-cell>${p.variesBy}</uui-table-cell>
                                  <uui-table-cell><code>${p.storageType}</code></uui-table-cell>
                              </uui-table-row>`
                          )}
                      </uui-table>
                  `
                : ""}
            ${ct.compositions?.length
                ? html`
                      <h5>Composed Of</h5>
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Alias</uui-table-head-cell>
                              <uui-table-head-cell>Kind</uui-table-head-cell>
                              <uui-table-head-cell>Properties</uui-table-head-cell>
                              <uui-table-head-cell>Groups</uui-table-head-cell>
                              <uui-table-head-cell>Varies By</uui-table-head-cell>
                              <uui-table-head-cell>Composes</uui-table-head-cell>
                          </uui-table-head>
                          ${ct.compositions.map(
                              (c) => html`<uui-table-row>
                                  <uui-table-cell
                                      ><a href=${editUrl("documentType", c.udi)} @click=${(e: Event) => openEditorModal(this, "documentType", c.udi, e)}
                                          >${c.name}</a
                                      ></uui-table-cell
                                  >
                                  <uui-table-cell><code>${c.alias}</code></uui-table-cell>
                                  <uui-table-cell>${c.isElement ? "Element" : "Document"}</uui-table-cell>
                                  <uui-table-cell>${c.propertyCount ?? 0}</uui-table-cell>
                                  <uui-table-cell>${c.propertyGroupCount ?? 0}</uui-table-cell>
                                  <uui-table-cell>${c.variesBy ?? ""}</uui-table-cell>
                                  <uui-table-cell><godmode-yes-no .value=${c.hasCompositions}></godmode-yes-no></uui-table-cell>
                              </uui-table-row>`
                          )}
                      </uui-table>
                  `
                : ""}
        `;
    }

    private _renderSchemaMap(ct: ContentTypeMap) {
        const compositions = (ct.compositions ?? []).slice(0, 5);
        const templates = (ct.templates ?? []).slice(0, 5);
        const nativeEditors = this._propertyEditorSummaries(ct.properties ?? []).slice(0, 4);
        const inheritedEditors = this._propertyEditorSummaries(ct.compositionProperties ?? []).slice(0, 4);
        const hasMoreCompositions = (ct.compositions?.length ?? 0) > compositions.length;
        const hasMoreTemplates = (ct.templates?.length ?? 0) > templates.length;
        const hasMoreNativeEditors = this._propertyEditorSummaries(ct.properties ?? []).length > nativeEditors.length;
        const hasMoreInheritedEditors = this._propertyEditorSummaries(ct.compositionProperties ?? []).length > inheritedEditors.length;
        const height = 288 + Math.max(nativeEditors.length, inheritedEditors.length) * 38;
        const center = { x: 520, y: 112 };

        return html`
            <div class="schema-map-wrap">
                ${svg`
                    <svg viewBox=${`0 0 1040 ${height}`} role="img" aria-label=${`Schema map for ${ct.name}`}>
                        <defs>
                            <marker id="godmode-schema-arrow" markerWidth="8" markerHeight="8" refX="7" refY="3" orient="auto" markerUnits="strokeWidth">
                                <path d="M0,0 L0,6 L7,3 z"></path>
                            </marker>
                        </defs>
                        ${compositions.map((composition, index) => {
                            const y = this._stackY(index, compositions.length, center.y, 42);
                            return svg`
                                <line class="schema-edge composition" x1="258" y1=${y} x2=${center.x - 118} y2=${center.y} marker-end="url(#godmode-schema-arrow)">
                                    <title>${composition.name} composes ${ct.name}</title>
                                </line>
                                ${this._renderSchemaNode(150, y, composition.name, composition.alias, "composition")}
                            `;
                        })}
                        ${templates.map((template, index) => {
                            const y = this._stackY(index, templates.length, center.y, 42);
                            return svg`
                                <line class="schema-edge template" x1=${center.x + 118} y1=${center.y} x2="782" y2=${y} marker-end="url(#godmode-schema-arrow)">
                                    <title>${ct.name} allows ${template.name}</title>
                                </line>
                                ${this._renderSchemaNode(890, y, template.name, template.alias, "template")}
                            `;
                        })}
                        <g class=${`schema-node center ${ct.isElement ? "element" : "document"}`} transform=${`translate(${center.x}, ${center.y})`}>
                            <rect x="-118" y="-34" width="236" height="68" rx="5"></rect>
                            <text class="name" y="-11">${ct.name}</text>
                            <text class="meta" y="7">${ct.isElement ? "Element Type" : "Document Type"} · ${ct.alias}</text>
                            <text class="meta" y="23">${(ct.properties?.length ?? 0)} native · ${(ct.compositionProperties?.length ?? 0)} inherited</text>
                            <title>${ct.name} (${ct.alias})</title>
                        </g>
                        ${nativeEditors.map((editor, index) => this._renderPropertyEditorNode(322, 218 + index * 38, editor.name, editor.count, "native"))}
                        ${inheritedEditors.map((editor, index) => this._renderPropertyEditorNode(718, 218 + index * 38, editor.name, editor.count, "inherited"))}
                        ${nativeEditors.length ? svg`<line class="schema-edge property" x1=${center.x - 36} y1="149" x2="322" y2="195" marker-end="url(#godmode-schema-arrow)"></line>` : ""}
                        ${inheritedEditors.length ? svg`<line class="schema-edge property inherited" x1=${center.x + 36} y1="149" x2="718" y2="195" marker-end="url(#godmode-schema-arrow)"></line>` : ""}
                        ${nativeEditors.length ? svg`<text class="lane-label" x="322" y="195">Native property editors</text>` : ""}
                        ${inheritedEditors.length ? svg`<text class="lane-label" x="718" y="195">Inherited property editors</text>` : ""}
                    </svg>
                `}
            </div>
            ${hasMoreCompositions || hasMoreTemplates || hasMoreNativeEditors || hasMoreInheritedEditors
                ? html`<p class="schema-note">
                      Map is capped for readability. Full templates, properties and compositions are in the
                      <button type="button" class="text-link" @click=${(e: Event) => this._setDetailView(ct.id, "details", e)}>Details View</button>.
                  </p>`
                : ""}
        `;
    }

    private _stackY(index: number, count: number, centerY: number, gap: number): number {
        return centerY - ((count - 1) * gap) / 2 + index * gap;
    }

    private _renderSchemaNode(x: number, y: number, name: string, alias: string, kind: "composition" | "template") {
        return svg`
            <g class=${`schema-node ${kind}`} transform=${`translate(${x}, ${y})`}>
                <rect x="-108" y="-20" width="216" height="40" rx="5"></rect>
                <text class="name" y="-3">${name}</text>
                <text class="meta" y="12">${alias}</text>
                <title>${name} (${alias})</title>
            </g>
        `;
    }

    private _renderPropertyEditorNode(x: number, y: number, name: string, count: number, kind: "native" | "inherited") {
        return svg`
            <g class=${`schema-node property-editor ${kind}`} transform=${`translate(${x}, ${y})`}>
                <rect x="-136" y="-17" width="272" height="34" rx="5"></rect>
                <text class="name" y="-2">${name}</text>
                <text class="meta" y="11">${count} ${count === 1 ? "property" : "properties"}</text>
                <title>${name}: ${count} ${count === 1 ? "property" : "properties"}</title>
            </g>
        `;
    }

    private _propertyEditorSummaries(properties: ContentTypeMap["properties"]): Array<{ name: string; count: number }> {
        const editors = new Map<string, number>();
        for (const property of properties ?? []) {
            const editor = property.editorAlias || "Unknown editor";
            editors.set(editor, (editors.get(editor) ?? 0) + 1);
        }

        return Array.from(editors.entries())
            .map(([name, count]) => ({ name, count }))
            .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name));
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
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
        h4 {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            flex-wrap: wrap;
            margin: 0;
            min-width: 0;
        }
        h5 {
            margin: var(--uui-size-space-3) 0 var(--uui-size-space-2);
        }
        .badge {
            background: var(--uui-color-surface-alt);
            padding: 2px 6px;
            border-radius: var(--uui-border-radius);
            font-size: 0.75em;
        }
        .badge.api {
            background: var(--uui-color-positive-standalone);
            color: var(--uui-color-surface);
        }
        .badge.warning {
            background: var(--uui-color-warning-standalone);
            color: var(--uui-color-surface);
        }
        .meta {
            display: flex;
            justify-content: space-between;
            margin-bottom: var(--uui-size-space-3);
        }
        .schema-map-wrap {
            overflow-x: auto;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            margin: var(--uui-size-space-3) 0 var(--uui-size-space-4);
        }
        .schema-map-wrap svg {
            display: block;
            min-width: 820px;
            width: 100%;
        }
        marker path {
            fill: var(--uui-color-border-emphasis);
        }
        .schema-edge {
            stroke: var(--uui-color-border-emphasis);
            stroke-width: 1.4;
            opacity: 0.58;
        }
        .schema-edge.template {
            stroke: var(--uui-color-selected);
        }
        .schema-edge.property {
            stroke-dasharray: 5 5;
        }
        .schema-node rect {
            fill: var(--uui-color-surface-alt);
            stroke: var(--uui-color-border);
        }
        .schema-node.center rect {
            fill: var(--uui-color-selected);
            stroke: var(--uui-color-selected);
        }
        .schema-node.element rect {
            fill: var(--uui-color-positive-emphasis);
            stroke: var(--uui-color-positive-emphasis);
        }
        .schema-node.property-editor rect {
            fill: var(--uui-color-surface);
        }
        .schema-node.inherited rect {
            stroke-dasharray: 4 4;
        }
        .schema-node text,
        .lane-label {
            text-anchor: middle;
            paint-order: stroke;
            stroke: var(--uui-color-surface);
            stroke-width: 3px;
            stroke-linejoin: round;
        }
        .schema-node.center text {
            fill: var(--uui-color-selected-contrast);
            stroke: transparent;
        }
        .schema-node.element text {
            fill: var(--uui-color-positive-contrast);
            stroke: transparent;
        }
        .schema-node .name {
            font-size: 13px;
            font-weight: 700;
        }
        .schema-node .meta {
            font-size: 10px;
            fill: var(--uui-color-text-alt);
        }
        .schema-node.center .meta {
            fill: var(--uui-color-selected-contrast);
            opacity: 0.92;
        }
        .schema-node.element .meta {
            fill: var(--uui-color-positive-contrast);
            opacity: 0.92;
        }
        .lane-label,
        .schema-note {
            color: var(--uui-color-text-alt);
        }
        .lane-label {
            font-size: 11px;
            font-weight: 700;
            fill: var(--uui-color-text-alt);
        }
        .schema-note {
            margin: calc(var(--uui-size-space-3) * -1) 0 var(--uui-size-space-3);
        }
        .text-link {
            border: 0;
            padding: 0;
            background: transparent;
            color: var(--uui-color-interactive);
            font: inherit;
            cursor: pointer;
            text-decoration: underline;
        }
        .text-link:hover,
        .text-link:focus-visible {
            color: var(--uui-color-interactive-emphasis);
        }
        .label {
            background: var(--uui-color-surface-alt);
            padding: 2px 6px;
            border-radius: var(--uui-border-radius);
            font-size: 0.8em;
        }
        .detail-tabs {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
            margin-bottom: var(--uui-size-space-3);
        }
        .row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: var(--uui-size-space-3);
            width: 100%;
            cursor: pointer;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .edit-link {
            flex: 0 0 auto;
            font-weight: 600;
        }
        .actions {
            display: inline-flex;
            align-items: center;
            justify-content: flex-end;
            gap: var(--uui-size-space-2);
            margin-left: auto;
            flex: 0 0 auto;
            justify-self: end;
        }
        .actions uui-button {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
    `;
}

export default GodModeDocTypeBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-doc-type-browser": GodModeDocTypeBrowserElement;
    }
}
