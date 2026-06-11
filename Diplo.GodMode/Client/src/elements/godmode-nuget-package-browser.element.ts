import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import { isGodModeAiExplainAvailable, observeGodModeAiExplainAvailability } from "../shared/ai-availability";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import type { NuGetPackageInfo, NuGetPackageInventory } from "../shared/types";

@customElement("godmode-nuget-package-browser")
export class GodModeNuGetPackageBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _inventory: NuGetPackageInventory | null = null;
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _scope = "";
    @state() private _loaded = "";
    @state() private _expanded = new Set<string>();
    @state() private _sort: SortState = { column: "id", reverse: false };
    @state() private _isAiExplainAvailable = isGodModeAiExplainAvailable();
    private _disposeAvailabilityObserver?: () => void;

    override connectedCallback(): void {
        super.connectedCallback();
        this._disposeAvailabilityObserver = observeGodModeAiExplainAvailability(() => {
            this._isAiExplainAvailable = true;
        });
        void this._load();
    }

    override disconnectedCallback(): void {
        this._disposeAvailabilityObserver?.();
        this._disposeAvailabilityObserver = undefined;
        super.disconnectedCallback();
    }

    private async _load() {
        this._loading = true;
        try {
            this._inventory = await godmodeGet<NuGetPackageInventory>("packages/runtime");
        } finally {
            this._loading = false;
        }
    }

    private _items(): NuGetPackageInfo[] {
        return this._inventory?.packages ?? [];
    }

    private _filtered(): NuGetPackageInfo[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._items().filter((item) => {
            if (q && !this._searchText(item).includes(q)) return false;
            if (this._scope === "direct" && !item.isDirect) return false;
            if (this._scope === "transitive" && !item.isTransitive) return false;
            if (this._loaded === "loaded" && !item.isLoaded) return false;
            if (this._loaded === "not-loaded" && (item.isLoaded || !this._hasRuntimeAssemblies(item))) return false;
            if (this._loaded === "no-runtime-assemblies" && this._hasRuntimeAssemblies(item)) return false;
            return true;
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as NuGetPackageInfo[];
    }

    private _hasRuntimeAssemblies(item: NuGetPackageInfo): boolean {
        return item.runtimeAssemblies.length > 0;
    }

    private _loadStatus(item: NuGetPackageInfo): { label: string; tone: "positive" | "neutral" | "warning"; title: string } {
        if (item.isLoaded) {
            return {
                label: "Assemblies loaded",
                tone: "positive",
                title: "At least one runtime assembly from this package is currently loaded."
            };
        }

        if (!this._hasRuntimeAssemblies(item)) {
            return {
                label: "No runtime assemblies",
                tone: "neutral",
                title: "This package is referenced in the dependency context but does not contribute runtime DLLs of its own. Umbrella packages often look like this."
            };
        }

        return {
            label: "Not loaded",
            tone: "warning",
            title: "The package contributes runtime DLLs, but none of them appear to be loaded in the current application domain."
        };
    }

    private _searchText(item: NuGetPackageInfo): string {
        return [
            item.id,
            item.version,
            item.type,
            item.directProjects.join(" "),
            item.requestedBy.join(" "),
            item.dependencies.join(" "),
            item.runtimeAssemblies.join(" "),
            item.loadedAssemblies.join(" ")
        ]
            .join(" ")
            .toLowerCase();
    }

    private _toggleExpanded(id: string) {
        const expanded = new Set(this._expanded);
        expanded.has(id) ? expanded.delete(id) : expanded.add(id);
        this._expanded = expanded;
    }

    private _sortBy(column: string) {
        this._sort = toggleSort(this._sort, column);
    }

    private _sortLabel(column: string, label: string): string {
        if (this._sort.column !== column) return label;
        return `${label} ${this._sort.reverse ? "▼" : "▲"}`;
    }

    override render() {
        const inventory = this._inventory;
        const items = this._items();
        const results = this._filtered();

        return html`
            <godmode-page
                heading="NuGet Packages"
                description="Inspect NuGet packages in the current runtime dependency context."
                show-reload
                @reload=${() => void this._load()}
            >
                <div class="summary">
                    <button class=${`summary-card ${!this._scope && !this._loaded ? "active" : ""}`} @click=${() => { this._scope = ""; this._loaded = ""; }}>
                        <strong>${inventory?.packageCount ?? 0}</strong>
                        <span>Runtime Packages</span>
                    </button>
                    <button class=${`summary-card ${this._scope === "direct" ? "active" : ""}`} @click=${() => (this._scope = this._scope === "direct" ? "" : "direct")}>
                        <strong>${inventory?.directPackageCount ?? 0}</strong>
                        <span>Direct</span>
                    </button>
                    <button class=${`summary-card ${this._scope === "transitive" ? "active" : ""}`} @click=${() => (this._scope = this._scope === "transitive" ? "" : "transitive")}>
                        <strong>${Math.max(0, (inventory?.packageCount ?? 0) - (inventory?.directPackageCount ?? 0))}</strong>
                        <span>Transitive</span>
                    </button>
                    <button class=${`summary-card ${this._loaded === "loaded" ? "active" : ""}`} @click=${() => (this._loaded = this._loaded === "loaded" ? "" : "loaded")}>
                        <strong>${inventory?.loadedPackageCount ?? 0}</strong>
                        <span>Assemblies Loaded</span>
                    </button>
                </div>

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
                                placeholder="Filter by package, project, dependency or assembly"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Scope</label>
                            <select @change=${(e: Event) => (this._scope = (e.target as HTMLSelectElement).value)}>
                                <option value="" ?selected=${this._scope === ""}>Any</option>
                                <option value="direct" ?selected=${this._scope === "direct"}>Direct</option>
                                <option value="transitive" ?selected=${this._scope === "transitive"}>Transitive</option>
                            </select>
                        </div>
                        <div>
                            <label>Runtime assemblies</label>
                            <select @change=${(e: Event) => (this._loaded = (e.target as HTMLSelectElement).value)}>
                                <option value="" ?selected=${this._loaded === ""}>Any</option>
                                <option value="loaded" ?selected=${this._loaded === "loaded"}>Assemblies loaded</option>
                                <option value="not-loaded" ?selected=${this._loaded === "not-loaded"}>Assemblies not loaded</option>
                                <option value="no-runtime-assemblies" ?selected=${this._loaded === "no-runtime-assemblies"}>No runtime assemblies</option>
                            </select>
                        </div>
                    </div>
                    <p class="help">
                        Direct packages are inferred from project libraries in the deployed dependency context. Some package references are umbrellas and do not contribute
                        runtime DLLs of their own. The view does not execute dotnet CLI commands or require the SDK.
                    </p>
                    ${inventory
                        ? html`<p class="runtime">
                              <strong>${inventory.targetFramework || "Target framework unavailable"}</strong>
                              ${inventory.runtimeName ? html`<span>${inventory.runtimeName}</span>` : ""}
                          </p>`
                        : ""}
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${items.length}</strong></p>
                          <div class=${`package-table ${this._isAiExplainAvailable ? "has-actions" : ""}`}>
                              <div class="package-head">
                                  <button class="package-col" @click=${() => this._sortBy("id")}>${this._sortLabel("id", "Package")}</button>
                                  <button class="version-col" @click=${() => this._sortBy("version")}>${this._sortLabel("version", "Version")}</button>
                                  <span class="scope-col">Scope</span>
                                  <span class="project-col">Project</span>
                                  <span class="requested-col">Requested By</span>
                                  <span class="link-col">NuGet</span>
                                  ${this._isAiExplainAvailable ? html`<span class="action-head">Actions</span>` : ""}
                              </div>
                              ${results.map((item) => this._renderRow(item))}
                          </div>
                      `}
            </godmode-page>
        `;
    }

    private _renderRow(item: NuGetPackageInfo) {
        const id = `${item.id}|${item.version}`;
        const directProjects = item.directProjects.join(", ");
        const requestedBy = item.requestedBy.join(", ");
        const loadStatus = this._loadStatus(item);

        return html`
            <div
                class=${`package-row ${this._expanded.has(id) ? "expanded" : ""}`}
                role="button"
                tabindex="0"
                @click=${() => this._toggleExpanded(id)}
                @keydown=${(e: KeyboardEvent) => {
                    if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        this._toggleExpanded(id);
                    }
                }}
            >
                <span class="package-col"><code>${item.id}</code></span>
                <span class="version-col"><code>${item.version}</code></span>
                <span class="scope-col">
                    <span class="flags">
                        <span class=${`pill ${item.isDirect ? "positive" : ""}`}>${item.isDirect ? "Direct" : "Transitive"}</span>
                        <span class=${`pill ${loadStatus.tone}`} title=${loadStatus.title}>${loadStatus.label}</span>
                    </span>
                </span>
                <span class="project-col" title=${directProjects}>${directProjects || "Runtime"}</span>
                <span class="requested-col" title=${requestedBy}>${requestedBy || ""}</span>
                <span class="link-col" @click=${(e: Event) => e.stopPropagation()}>
                    <a href=${item.nuGetUrl} target="_blank" rel="noopener">NuGet</a>
                </span>
                ${this._isAiExplainAvailable
                    ? html`
                          <span class="action-cell" @click=${(e: Event) => e.stopPropagation()}>
                              <godmode-ai-explain-host .subject=${this._explainSubject(item)}></godmode-ai-explain-host>
                          </span>
                      `
                    : ""}
            </div>
            ${this._expanded.has(id)
                ? html`
                      <div class="details-panel" @click=${(e: Event) => e.stopPropagation()}>
                          <div class="detail-grid">
                              ${this._renderList("Dependencies", item.dependencies)}
                              ${this._renderList("Runtime Assemblies", item.runtimeAssemblies)}
                              ${this._renderList("Loaded Assemblies", item.loadedAssemblies)}
                          </div>
                      </div>
                  `
                : ""}
        `;
    }

    private _renderList(label: string, items: string[]) {
        return html`
            <section class="detail-list">
                <h4>${label}</h4>
                ${items.length
                    ? html`<ul>${items.map((item) => html`<li><code>${item}</code></li>`)}</ul>`
                    : html`<p class="muted">None reported.</p>`}
            </section>
        `;
    }

    private _explainSubject(item: NuGetPackageInfo) {
        return {
            subjectType: "NuGet runtime package",
            title: item.id,
            data: item,
            context: {
                source: "DependencyContext.Default runtime libraries",
                notes: [
                    "Direct/transitive is inferred from deployed project library dependencies.",
                    "Loaded means at least one matching runtime assembly is currently loaded in the application domain.",
                    "Some referenced packages are umbrella packages and do not contribute runtime assemblies of their own.",
                    "No dotnet CLI commands were executed to build this inventory."
                ]
            }
        };
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .summary-card {
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
            cursor: pointer;
            padding: var(--uui-size-space-3);
            text-align: left;
        }
        .summary-card.active {
            border-color: var(--uui-color-interactive);
            box-shadow: inset 0 0 0 1px var(--uui-color-interactive);
        }
        .summary-card strong {
            display: block;
            font-size: 1.35rem;
        }
        .filters {
            display: grid;
            grid-template-columns: 2fr 1fr 1fr;
            gap: var(--uui-size-space-4);
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input,
        select {
            width: 100%;
        }
        select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .help,
        .results,
        .runtime,
        .muted {
            color: var(--uui-color-text-alt);
        }
        .help {
            margin: var(--uui-size-space-4) 0 0;
        }
        .runtime,
        .results {
            margin: var(--uui-size-space-3) 0 0;
        }
        .runtime {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-3);
        }
        .package-table {
            display: grid;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            overflow: hidden;
        }
        .package-head,
        .package-row {
            display: grid;
            grid-template-columns: minmax(220px, 1.4fr) 9rem 12rem minmax(160px, 0.95fr) minmax(220px, 1.25fr) 5rem;
            align-items: start;
            column-gap: var(--uui-size-space-4);
            padding: var(--uui-size-space-3);
        }
        .package-table.has-actions .package-head,
        .package-table.has-actions .package-row {
            grid-template-columns: minmax(220px, 1.4fr) 9rem 12rem minmax(160px, 0.95fr) minmax(220px, 1.25fr) 5rem 5.5rem;
        }
        .package-head {
            font-weight: 700;
            background: var(--uui-color-surface-alt);
            border-bottom: 1px solid var(--uui-color-border);
        }
        .package-head button {
            border: 0;
            background: transparent;
            color: var(--uui-color-text);
            cursor: pointer;
            font: inherit;
            font-weight: 700;
            padding: 0;
            text-align: left;
        }
        .package-row {
            border-bottom: 1px solid var(--uui-color-border);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
            cursor: pointer;
            transition: background-color 120ms ease;
        }
        .package-row:hover,
        .package-row.expanded {
            background: color-mix(in srgb, var(--uui-color-interactive) 7%, var(--uui-color-surface-alt));
        }
        .package-row > span,
        .package-head > span,
        .package-head > button {
            min-width: 0;
            overflow-wrap: anywhere;
        }
        code {
            white-space: normal;
            overflow-wrap: anywhere;
            word-break: break-word;
        }
        .flags {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-1);
        }
        .pill {
            border-radius: 3px;
            background: var(--uui-color-border);
            color: var(--uui-color-text);
            padding: 1px var(--uui-size-space-1);
            font-size: 0.75rem;
            font-weight: 600;
        }
        .pill.positive {
            background: var(--uui-color-positive);
            color: var(--uui-color-surface);
        }
        .pill.warning {
            background: var(--uui-color-warning);
            color: var(--uui-color-text);
        }
        .action-cell {
            cursor: default;
            text-align: right;
        }
        .details-panel {
            background: var(--uui-color-surface-alt);
            border-bottom: 1px solid var(--uui-color-border);
            cursor: default;
            padding: var(--uui-size-space-4);
            width: 100%;
            box-sizing: border-box;
        }
        .detail-grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-4);
        }
        .detail-list h4 {
            margin: 0 0 var(--uui-size-space-2);
        }
        .detail-list ul {
            display: grid;
            gap: var(--uui-size-space-1);
            list-style: none;
            margin: 0;
            padding: 0;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        @media (max-width: 1000px) {
            .filters,
            .detail-grid {
                grid-template-columns: 1fr;
            }
            .package-head {
                display: none;
            }
            .package-row,
            .package-table.has-actions .package-row {
                grid-template-columns: 1fr;
                row-gap: var(--uui-size-space-2);
            }
        }
    `;
}

export default GodModeNuGetPackageBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-nuget-package-browser": GodModeNuGetPackageBrowserElement;
    }
}
