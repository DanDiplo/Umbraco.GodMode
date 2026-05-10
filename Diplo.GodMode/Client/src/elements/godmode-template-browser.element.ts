import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { TemplateModel } from "../shared/types";
import { uniqueBy } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openUsedByModal } from "../shared/used-by-modal";

@customElement("godmode-template-browser")
export class GodModeTemplateBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _templates: TemplateModel[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _masterFilter = "";
    @state() private _partialFilter = "";
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
            return true;
        });
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
                }))
            }
        };
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: 2fr 1fr 1fr;
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
    `;
}

export default GodModeTemplateBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-template-browser": GodModeTemplateBrowserElement;
    }
}
