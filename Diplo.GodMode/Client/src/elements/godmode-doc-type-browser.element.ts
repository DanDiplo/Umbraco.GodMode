import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ContentTypeMap, ContentVariation } from "../shared/types";
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
    @state() private _variesBy = "any";
    @state() private _propertyGroup = "";
    @state() private _propertyQuery = "";
    @state() private _open = new Set<number>();

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
                    </span>
                </div>
                ${isOpen ? this._renderDetail(ct) : ""}
            </uui-box>
        `;
    }

    private _renderDetail(ct: ContentTypeMap) {
        return html`
            <div class="meta">
                <p>${ct.description ?? ""}</p>
                <small><span class="label">${ct.id}</span> <code>${ct.udi}</code></small>
            </div>
            ${ct.templates?.length
                ? html`
                      <h5>Templates</h5>
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Name</uui-table-head-cell>
                              <uui-table-head-cell>Alias</uui-table-head-cell>
                          </uui-table-head>
                          ${ct.templates.map(
                              (t) => html`<uui-table-row>
                                  <uui-table-cell
                                      ><a href=${editUrl("template", t.udi)} @click=${(e: Event) => openEditorModal(this, "template", t.udi, e)}
                                          >${t.name}</a
                                      ></uui-table-cell
                                  >
                                  <uui-table-cell><code>${t.alias}</code></uui-table-cell>
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
                          </uui-table-head>
                          ${ct.compositions.map(
                              (c) => html`<uui-table-row>
                                  <uui-table-cell
                                      ><a href=${editUrl("documentType", c.udi)} @click=${(e: Event) => openEditorModal(this, "documentType", c.udi, e)}
                                          >${c.name}</a
                                      ></uui-table-cell
                                  >
                                  <uui-table-cell><code>${c.alias}</code></uui-table-cell>
                              </uui-table-row>`
                          )}
                      </uui-table>
                  `
                : ""}
        `;
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
        .meta {
            display: flex;
            justify-content: space-between;
            margin-bottom: var(--uui-size-space-3);
        }
        .label {
            background: var(--uui-color-surface-alt);
            padding: 2px 6px;
            border-radius: var(--uui-border-radius);
            font-size: 0.8em;
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
