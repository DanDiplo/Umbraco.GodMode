import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { TemplateModel } from "../shared/types";
import { uniqueBy } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openUsedByModal } from "../shared/used-by-modal";

type TriState = "any" | "yes" | "no";

@customElement("godmode-template-browser")
export class GodModeTemplateBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _templates: TemplateModel[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _masterFilter = "";
    @state() private _partialFilter = "";
    @state() private _hasScripts: TriState = "any";
    @state() private _hasCss: TriState = "any";
    @state() private _hasImages: TriState = "any";
    @state() private _hasForms: TriState = "any";
    @state() private _hasMissingAssets: TriState = "any";
    @state() private _open = new Set<number>();

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

    private _toggle(id: number) {
        const next = new Set(this._open);
        next.has(id) ? next.delete(id) : next.add(id);
        this._open = next;
    }

    private _filtered(): TemplateModel[] {
        const q = this._search.trim().toLowerCase();
        return this._templates.filter((t) => {
            if (q && ![t.name, t.alias].some((v) => v.toLowerCase().includes(q))) return false;
            if (this._masterFilter && (t.masterAlias ?? "") !== this._masterFilter) return false;
            if (this._partialFilter && !t.partials?.some((p) => p.name === this._partialFilter)) return false;
            if (!this._matchTri((t.assets ?? []).some((asset) => asset.kind === "Script"), this._hasScripts)) return false;
            if (!this._matchTri((t.assets ?? []).some((asset) => asset.kind === "Stylesheet" || asset.kind === "Style" || asset.kind === "CssUrl"), this._hasCss)) return false;
            if (!this._matchTri((t.assets ?? []).some((asset) => asset.kind === "Image"), this._hasImages)) return false;
            if (!this._matchTri(!!t.forms?.length, this._hasForms)) return false;
            if (!this._matchTri((t.assets ?? []).some((asset) => asset.isResolved && !asset.exists), this._hasMissingAssets)) return false;
            return true;
        });
    }

    private _matchTri(value: boolean, filter: TriState): boolean {
        if (filter === "any") return true;
        return filter === "yes" ? value : !value;
    }

    override render() {
        const masters = Array.from(
            new Set(this._templates.map((t) => t.masterAlias).filter((v): v is string => !!v))
        );

        const allPartials = uniqueBy(
            this._templates.flatMap((t) => t.partials ?? []),
            "name"
        );

        const results = this._filtered();

        return html`
            <godmode-page
                heading="Template Browser"
                description="Browse the template hierarchy and see which partials each template uses."
                show-reload
                @reload=${() => void this._load()}
            >
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by name or alias"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Uses Master</label>
                            <select @change=${(e: Event) => (this._masterFilter = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${masters.map((m) => html`<option value=${m}>${m}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Uses Partial</label>
                            <select @change=${(e: Event) => (this._partialFilter = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${allPartials.map((p) => html`<option value=${p.name}>${p.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Has Scripts?</label>
                            ${this._tri("hasScripts", this._hasScripts, (v) => (this._hasScripts = v))}
                        </div>
                        <div>
                            <label>Has CSS?</label>
                            ${this._tri("hasCss", this._hasCss, (v) => (this._hasCss = v))}
                        </div>
                        <div>
                            <label>Has Images?</label>
                            ${this._tri("hasImages", this._hasImages, (v) => (this._hasImages = v))}
                        </div>
                        <div>
                            <label>Has Forms?</label>
                            ${this._tri("hasForms", this._hasForms, (v) => (this._hasForms = v))}
                        </div>
                        <div>
                            <label>Missing Assets?</label>
                            ${this._tri("hasMissingAssets", this._hasMissingAssets, (v) => (this._hasMissingAssets = v))}
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._templates.length}</strong></p>
                          ${results.map((t) => this._renderTemplate(t))}
                      `}
            </godmode-page>
        `;
    }

    private _renderTemplate(t: TemplateModel) {
        const isOpen = this._open.has(t.id);
        return html`
            <uui-box style="margin-top: var(--uui-size-space-3)">
                <div class="row" @click=${() => this._toggle(t.id)}>
                    <h4><strong>${t.name}</strong> <small>(${t.alias})</small></h4>
                    <span class="actions" @click=${(e: Event) => e.stopPropagation()}>
                        <uui-button compact look="secondary" label="Used by" @click=${(e: Event) => openUsedByModal(this, {
                            targetType: "Template",
                            targetKey: t.udi,
                            targetName: t.name,
                            targetAlias: t.alias
                        }, e)}>
                            <uui-icon name="icon-link"></uui-icon>
                            Used by
                        </uui-button>
                        <uui-button compact look="secondary" label="Edit" href=${editUrl("template", t.udi)} @click=${(e: Event) => openEditorModal(this, "template", t.udi, e)}>
                            <uui-icon name="icon-edit"></uui-icon>
                            Edit
                        </uui-button>
                        <godmode-ai-explain-host .subject=${this._explainSubject(t)}></godmode-ai-explain-host>
                    </span>
                </div>
                ${isOpen
                    ? html`
                          <div class="meta">
                              <small class="muted">${t.virtualPath ?? t.filePath ?? ""}</small>
                              <span class="label">${t.id}</span>
                          </div>
                          ${t.parents?.length
                              ? html`
                                    <h5>Inheritance</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Name</uui-table-head-cell>
                                            <uui-table-head-cell>Alias</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.parents.map(
                                            (p) => html`<uui-table-row>
                                                <uui-table-cell
                                                    ><a href=${editUrl("template", p.udi)} @click=${(e: Event) => openEditorModal(this, "template", p.udi, e)}
                                                        >${p.name}</a
                                                    ></uui-table-cell
                                                >
                                                <uui-table-cell><code>${p.alias}</code></uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.partials?.length
                              ? html`
                                    <h5>Partials</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Partial Name</uui-table-head-cell>
                                            <uui-table-head-cell>Path</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.partials.map(
                                            (p) => html`
                                                <uui-table-row>
                                                    <uui-table-cell><strong>${p.name}</strong></uui-table-cell>
                                                    <uui-table-cell><code>${p.path}</code></uui-table-cell>
                                                </uui-table-row>
                                            `
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.viewComponents?.length
                              ? html`
                                    <h5>View Components</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Name</uui-table-head-cell>
                                            <uui-table-head-cell>Parameters</uui-table-head-cell>
                                            <uui-table-head-cell>Tag Helper?</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.viewComponents.map(
                                            (c) => html`<uui-table-row>
                                                <uui-table-cell>${c.name}</uui-table-cell>
                                                <uui-table-cell><code>${c.parameters}</code></uui-table-cell>
                                                <uui-table-cell><godmode-yes-no .value=${c.tagHelper}></godmode-yes-no></uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.assets?.length
                              ? html`
                                    <h5>Assets</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Kind</uui-table-head-cell>
                                            <uui-table-head-cell>Url</uui-table-head-cell>
                                            <uui-table-head-cell>Status</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.assets.map(
                                            (asset) => html`<uui-table-row>
                                                <uui-table-cell>${asset.kind}${asset.isInline ? " (inline)" : ""}</uui-table-cell>
                                                <uui-table-cell><code>${asset.url || asset.attributes}</code></uui-table-cell>
                                                <uui-table-cell>
                                                    ${asset.isResolved
                                                        ? asset.exists
                                                            ? html`<span class="ok">Found</span>`
                                                            : html`<span class="warning">Missing</span>`
                                                        : asset.host || (asset.isExternal ? "External" : "Not resolved")}
                                                    ${asset.warning ? html`<div class="muted">${asset.warning}</div>` : ""}
                                                </uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.sections?.length
                              ? html`
                                    <h5>Sections</h5>
                                    <div class="chips">
                                        ${t.sections.map((section) => html`<span class="label">${section.name}</span>`)}
                                    </div>
                                `
                              : ""}
                          ${t.forms?.length
                              ? html`
                                    <h5>Forms</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Kind</uui-table-head-cell>
                                            <uui-table-head-cell>Method</uui-table-head-cell>
                                            <uui-table-head-cell>Target</uui-table-head-cell>
                                            <uui-table-head-cell>Anti-forgery?</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.forms.map(
                                            (form) => html`<uui-table-row>
                                                <uui-table-cell>${form.kind}</uui-table-cell>
                                                <uui-table-cell><code>${form.method || ""}</code></uui-table-cell>
                                                <uui-table-cell><code>${[form.controller, form.action].filter(Boolean).join("/")}</code></uui-table-cell>
                                                <uui-table-cell><godmode-yes-no .value=${form.hasAntiForgeryToken}></godmode-yes-no></uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.tagHelpers?.length
                              ? html`
                                    <h5>Tag Helpers</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Tag</uui-table-head-cell>
                                            <uui-table-head-cell>Kind</uui-table-head-cell>
                                            <uui-table-head-cell>Attributes</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.tagHelpers.map(
                                            (tag) => html`<uui-table-row>
                                                <uui-table-cell><code>${tag.tagName}</code></uui-table-cell>
                                                <uui-table-cell>${tag.kind}</uui-table-cell>
                                                <uui-table-cell><code>${tag.attributes}</code></uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                          ${t.umbracoUsages?.length
                              ? html`
                                    <h5>Umbraco Usage</h5>
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell>Kind</uui-table-head-cell>
                                            <uui-table-head-cell>Name</uui-table-head-cell>
                                            <uui-table-head-cell>Expression</uui-table-head-cell>
                                        </uui-table-head>
                                        ${t.umbracoUsages.map(
                                            (usage) => html`<uui-table-row>
                                                <uui-table-cell>${usage.kind}</uui-table-cell>
                                                <uui-table-cell><code>${usage.name}</code></uui-table-cell>
                                                <uui-table-cell><code>${usage.expression}</code></uui-table-cell>
                                            </uui-table-row>`
                                        )}
                                    </uui-table>
                                `
                              : ""}
                      `
                    : ""}
            </uui-box>
        `;
    }

    private _explainSubject(t: TemplateModel) {
        return {
            subjectType: "Umbraco template",
            title: `${t.name} (${t.alias})`,
            data: {
                id: t.id,
                name: t.name,
                alias: t.alias,
                udi: t.udi,
                masterAlias: t.masterAlias,
                hasCorrectMaster: t.hasCorrectMaster,
                virtualPath: t.virtualPath,
                filePath: t.filePath,
                parentTemplates: (t.parents ?? []).map((parent) => ({
                    name: parent.name,
                    alias: parent.alias,
                    udi: parent.udi
                })),
                partials: (t.partials ?? []).map((partial) => ({
                    name: partial.name,
                    path: partial.path
                })),
                viewComponents: (t.viewComponents ?? []).map((component) => ({
                    name: component.name,
                    parameters: component.parameters,
                    tagHelper: component.tagHelper
                })),
                assets: (t.assets ?? []).map((asset) => ({
                    kind: asset.kind,
                    url: asset.url,
                    host: asset.host,
                    isExternal: asset.isExternal,
                    isInline: asset.isInline,
                    isResolved: asset.isResolved,
                    exists: asset.exists,
                    warning: asset.warning
                })),
                sections: (t.sections ?? []).map((section) => section.name),
                forms: (t.forms ?? []).map((form) => ({
                    kind: form.kind,
                    method: form.method,
                    action: form.action,
                    controller: form.controller,
                    hasAntiForgeryToken: form.hasAntiForgeryToken
                })),
                tagHelpers: (t.tagHelpers ?? []).map((tag) => ({
                    tagName: tag.tagName,
                    kind: tag.kind
                })),
                umbracoUsages: (t.umbracoUsages ?? []).map((usage) => ({
                    kind: usage.kind,
                    name: usage.name,
                    expression: usage.expression
                }))
            }
        };
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

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(145px, 1fr));
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
        h4,
        h5 {
            margin: var(--uui-size-space-3) 0 var(--uui-size-space-2);
        }
        h4 {
            min-width: 0;
        }
        .meta {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: var(--uui-size-space-3);
        }
        .muted {
            color: var(--uui-color-text-alt);
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
            gap: var(--uui-size-space-3);
            margin-left: auto;
            flex: 0 0 auto;
            justify-self: end;
        }
        .actions uui-button {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
        .chips {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
        }
        code {
            white-space: normal;
            overflow-wrap: anywhere;
        }
        .ok {
            color: var(--uui-color-positive);
            font-weight: 600;
        }
        .warning {
            color: var(--uui-color-danger);
            font-weight: 600;
        }
    `;
}

export default GodModeTemplateBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-template-browser": GodModeTemplateBrowserElement;
    }
}
