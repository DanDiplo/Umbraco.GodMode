import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { NameValue, TypeMap } from "../shared/types";

@customElement("godmode-type-browser")
export class GodModeTypeBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _assemblies: NameValue[] = [];
    @state() private _interfaces: TypeMap[] = [];
    @state() private _types: TypeMap[] = [];
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

    override render() {
        return html`
            <godmode-page
                heading="Interface Browser"
                description="Pick an assembly, then an interface, to see every type in your app that implements it."
            >
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Assembly</label>
                            <select @change=${(e: Event) => void this._onAssemblyChange(e)}>
                                <option value="">— pick an assembly —</option>
                                ${this._assemblies.map((a) => html`<option value=${a.value}>${a.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Interface</label>
                            <select ?disabled=${!this._interfaces.length} @change=${(e: Event) => void this._onInterfaceChange(e)}>
                                <option value="">— pick an interface —</option>
                                ${this._interfaces.map(
                                    (i) => html`<option value=${i.loadableName}>${i.name}</option>`
                                )}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._types.length
                      ? html`
                            <p class="results"><strong>${this._types.length}</strong> implementations</p>
                            <uui-table>
                                <uui-table-head>
                                    <uui-table-head-cell>Name</uui-table-head-cell>
                                    <uui-table-head-cell>Namespace</uui-table-head-cell>
                                    <uui-table-head-cell>Assembly</uui-table-head-cell>
                                </uui-table-head>
                                ${this._types.map(
                                    (t) => html`
                                        <uui-table-row>
                                            <uui-table-cell><strong>${t.name}</strong></uui-table-cell>
                                            <uui-table-cell><code>${t.namespace}</code></uui-table-cell>
                                            <uui-table-cell><code>${t.assembly}</code></uui-table-cell>
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
            grid-template-columns: 1fr 1fr;
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
    `;
}

export default GodModeTypeBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-type-browser": GodModeTypeBrowserElement;
    }
}
