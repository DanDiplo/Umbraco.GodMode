import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { NameValue, TypeDetail, TypeMap } from "../shared/types";
import { openEvidenceDrawer } from "../shared/evidence-drawer";

@customElement("godmode-type-browser")
export class GodModeTypeBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _assemblies: NameValue[] = [];
    @state() private _interfaces: TypeMap[] = [];
    @state() private _types: TypeMap[] = [];
    @state() private _selectedOrigin = "";
    @state() private _selectedAssembly = "";
    @state() private _selectedInterface = "";
    @state() private _loading = false;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadAssemblies();
    }

    private async _loadAssemblies() {
        this._loading = true;
        try {
            this._assemblies = await godmodeGet<NameValue[]>("assemblies/with-interfaces");
        } finally {
            this._loading = false;
        }
    }

    private _originFromAssembly(assemblyName: string) {
        const simpleName = assemblyName.split(",")[0]?.trim() ?? "";
        if (!simpleName) return "Unknown";
        if (simpleName.toLowerCase() === "umbraco" || simpleName.toLowerCase().startsWith("umbraco.")) return "Umbraco";
        return simpleName.split(".")[0] || simpleName;
    }

    private _assemblyOrigins() {
        return Array.from(new Set(this._assemblies.map((assembly) => this._originFromAssembly(assembly.value)).filter(Boolean))).sort();
    }

    private _filteredAssemblies() {
        return this._selectedOrigin ? this._assemblies.filter((assembly) => this._originFromAssembly(assembly.value) === this._selectedOrigin) : this._assemblies;
    }

    private _onOriginChange(e: Event) {
        this._selectedOrigin = (e.target as HTMLSelectElement).value;
        this._selectedAssembly = "";
        this._selectedInterface = "";
        this._interfaces = [];
        this._types = [];
    }

    private async _onAssemblyChange(e: Event) {
        this._selectedAssembly = (e.target as HTMLSelectElement).value;
        this._interfaces = [];
        this._types = [];
        this._selectedInterface = "";
        if (!this._selectedAssembly) return;
        this._loading = true;
        try {
            this._interfaces = await godmodeGet<TypeMap[]>(`assemblies/${encodeURIComponent(this._selectedAssembly)}/interfaces`);
        } finally {
            this._loading = false;
        }
    }

    private async _onInterfaceChange(e: Event) {
        this._selectedInterface = (e.target as HTMLSelectElement).value;
        this._types = [];
        if (!this._selectedInterface) return;
        this._loading = true;
        try {
            this._types = await godmodeGet<TypeMap[]>("reflection/types-assignable-from", { baseType: this._selectedInterface });
        } finally {
            this._loading = false;
        }
    }

    private async _openDetails(type: TypeMap, e: Event) {
        const detail = await godmodeGet<TypeDetail>("reflection/type-detail", { loadableName: type.loadableName });

        openEvidenceDrawer(
            this,
            {
                title: detail.name,
                subtitle: detail.namespace,
                summary: [
                    { label: "Assembly", value: detail.module },
                    { label: "Origin", value: detail.origin },
                    { label: "Base", value: detail.baseType || "None" },
                    { label: "Source", value: detail.isUmbraco ? "Umbraco" : "Custom / package" },
                    { label: "Interfaces", value: detail.interfaces.length }
                ],
                sections: [
                    {
                        heading: "Type",
                        items: {
                            name: detail.name,
                            namespace: detail.namespace,
                            module: detail.module,
                            origin: detail.origin,
                            assembly: detail.assembly,
                            loadableName: detail.loadableName,
                            isUmbraco: detail.isUmbraco
                        }
                    },
                    {
                        heading: "Inheritance Chain",
                        description: "Base types from immediate parent up to System.Object.",
                        items: detail.inheritanceChain.map((item, index) => ({
                            depth: index + 1,
                            name: item.name,
                            namespace: item.namespace,
                            assembly: item.module
                        }))
                    },
                    {
                        heading: "Implemented Interfaces",
                        items: detail.interfaces.map((item) => ({
                            name: item.name,
                            namespace: item.namespace,
                            assembly: item.module,
                            selected: item.loadableName === this._selectedInterface
                        }))
                    }
                ]
            },
            e
        );
    }

    override render() {
        const origins = this._assemblyOrigins();
        const assemblies = this._filteredAssemblies();
        const results = this._types;

        return html`
            <godmode-page
                heading="Interface Browser"
                description="Pick an assembly, then an interface, to see every type in your app that implements it."
            >
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Origin</label>
                            <select @change=${(e: Event) => this._onOriginChange(e)}>
                                <option value="">Any</option>
                                ${origins.map((origin) => html`<option value=${origin} ?selected=${origin === this._selectedOrigin}>${origin}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Assembly</label>
                            <select @change=${(e: Event) => void this._onAssemblyChange(e)}>
                                <option value="">— pick an assembly —</option>
                                ${assemblies.map((a) => html`<option value=${a.value} ?selected=${a.value === this._selectedAssembly}>${a.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Interface</label>
                            <select ?disabled=${!this._interfaces.length} @change=${(e: Event) => void this._onInterfaceChange(e)}>
                                <option value="">— pick an interface —</option>
                                ${this._interfaces.map(
                                    (i) => html`<option value=${i.loadableName}>${i.name} (${i.implementationCount})</option>`
                                )}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._types.length
                      ? html`
                            <p class="results"><strong>${results.length}</strong> / <strong>${this._types.length}</strong> implementations</p>
                            <uui-table>
                                <uui-table-head>
                                    <uui-table-head-cell>Name</uui-table-head-cell>
                                    <uui-table-head-cell>Namespace</uui-table-head-cell>
                                    <uui-table-head-cell>Origin</uui-table-head-cell>
                                    <uui-table-head-cell>Assembly</uui-table-head-cell>
                                    <uui-table-head-cell>Base</uui-table-head-cell>
                                    <uui-table-head-cell class="action-head">Actions</uui-table-head-cell>
                                </uui-table-head>
                                ${results.map(
                                    (t) => html`
                                        <uui-table-row>
                                            <uui-table-cell><strong>${t.name}</strong></uui-table-cell>
                                            <uui-table-cell><code>${t.namespace}</code></uui-table-cell>
                                            <uui-table-cell><span class="origin">${t.origin}</span></uui-table-cell>
                                            <uui-table-cell><code>${t.assembly}</code></uui-table-cell>
                                            <uui-table-cell>${t.baseType}</uui-table-cell>
                                            <uui-table-cell class="action-cell">
                                                <uui-button compact look="secondary" label="Details" @click=${(e: Event) => void this._openDetails(t, e)}>Details</uui-button>
                                            </uui-table-cell>
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
            grid-template-columns: 1fr 1fr minmax(140px, 0.5fr);
            gap: var(--uui-size-space-4);
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
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
        .origin {
            display: inline-flex;
            align-items: center;
            min-height: 22px;
            padding: 0 var(--uui-size-space-2);
            border-radius: 999px;
            background: var(--uui-color-surface-alt);
            color: var(--uui-color-text);
            font-size: 12px;
            white-space: nowrap;
        }
        .action-head,
        .action-cell {
            width: 7rem;
            text-align: right;
        }
    `;
}

export default GodModeTypeBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-type-browser": GodModeTypeBrowserElement;
    }
}
