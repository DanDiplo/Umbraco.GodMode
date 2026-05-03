import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import "../shared";
import { applySort, toggleSort, type SortState } from "../shared/sort";

type ExtensionManifest = ManifestBase & Record<string, unknown>;

interface ExtensionRow {
    alias: string;
    type: string;
    kind: string;
    name: string;
    weight: number | null;
    package: string;
    conditions: string;
    conditionCount: number;
    meta: string;
    manifest: ExtensionManifest;
}

const FEATURE_TYPES = ["section", "menu", "menuItem", "workspace", "workspaceView", "dashboard", "propertyEditorUi", "tree", "sectionSidebarApp"];

@customElement("godmode-extension-explorer")
export class GodModeExtensionExplorerElement extends UmbElementMixin(LitElement) {
    @state() private _items: ExtensionRow[] = [];
    @state() private _search = "";
    @state() private _type = "";
    @state() private _package = "";
    @state() private _featuredOnly = false;
    @state() private _expanded = new Set<string>();
    @state() private _sort: SortState = { column: "type", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        this.observe(
            umbExtensionsRegistry.extensions,
            (manifests) => {
                this._items = manifests.map((manifest) => this._toRow(manifest as ExtensionManifest));
            },
            "observeExtensionRegistry"
        );
        this._items = umbExtensionsRegistry.getAllExtensions().map((manifest) => this._toRow(manifest as ExtensionManifest));
    }

    private _toRow(manifest: ExtensionManifest): ExtensionRow {
        const meta = this._record(manifest.meta);
        const weight = this._number(meta.weight ?? manifest.weight);
        const conditions = this._conditions(manifest.conditions);

        return {
            alias: String(manifest.alias ?? ""),
            type: String(manifest.type ?? ""),
            kind: String(manifest.kind ?? ""),
            name: String(manifest.name ?? meta.label ?? ""),
            weight,
            package: this._sourcePackage(manifest),
            conditions,
            conditionCount: Array.isArray(manifest.conditions) ? manifest.conditions.length : 0,
            meta: this._summarizeMeta(meta),
            manifest
        };
    }

    private _record(value: unknown): Record<string, unknown> {
        return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : {};
    }

    private _number(value: unknown): number | null {
        return typeof value === "number" ? value : null;
    }

    private _sourcePackage(manifest: ExtensionManifest): string {
        const candidate = manifest.packageName ?? manifest.packageAlias ?? manifest.packageId ?? manifest.package ?? manifest.js;
        if (typeof candidate === "string" && candidate) return candidate;
        if (String(manifest.alias ?? "").startsWith("Diplo.")) return "Diplo.GodMode";
        if (String(manifest.alias ?? "").startsWith("Umb.")) return "Umbraco";
        return "Unknown";
    }

    private _conditions(value: unknown): string {
        if (!Array.isArray(value) || !value.length) return "";
        return value
            .map((condition) => {
                const c = this._record(condition);
                const details = Object.entries(c)
                    .filter(([key]) => key !== "alias")
                    .map(([key, val]) => `${key}: ${this._brief(val)}`)
                    .join(", ");
                return `${c.alias ?? "condition"}${details ? ` (${details})` : ""}`;
            })
            .join("; ");
    }

    private _summarizeMeta(meta: Record<string, unknown>): string {
        return Object.entries(meta)
            .filter(([, value]) => value !== undefined && value !== null && typeof value !== "function")
            .slice(0, 8)
            .map(([key, value]) => `${key}: ${this._brief(value)}`)
            .join("; ");
    }

    private _brief(value: unknown): string {
        if (Array.isArray(value)) return `[${value.map((v) => this._brief(v)).join(", ")}]`;
        if (value && typeof value === "object") return JSON.stringify(value);
        return String(value);
    }

    private _filtered(): ExtensionRow[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._items.filter((item) => {
            if (q && !this._searchText(item).includes(q)) return false;
            if (this._type && item.type !== this._type) return false;
            if (this._package && item.package !== this._package) return false;
            if (this._featuredOnly && !FEATURE_TYPES.includes(item.type)) return false;
            return true;
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as ExtensionRow[];
    }

    private _searchText(item: ExtensionRow): string {
        return [item.alias, item.type, item.kind, item.name, item.package, item.conditions, item.meta].join(" ").toLowerCase();
    }

    private _typeCounts(): Map<string, number> {
        const counts = new Map<string, number>();
        for (const item of this._items) counts.set(item.type, (counts.get(item.type) ?? 0) + 1);
        return counts;
    }

    private _toggleExpanded(alias: string) {
        const expanded = new Set(this._expanded);
        expanded.has(alias) ? expanded.delete(alias) : expanded.add(alias);
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
        const types = Array.from(new Set(this._items.map((item) => item.type))).sort();
        const packages = Array.from(new Set(this._items.map((item) => item.package))).sort();
        const typeCounts = this._typeCounts();
        const results = this._filtered();

        return html`
            <godmode-page
                heading="Backoffice Extension Explorer"
                description="Inspect extension manifests registered in the current backoffice runtime."
                show-reload
                @reload=${() => (this._items = umbExtensionsRegistry.getAllExtensions().map((manifest) => this._toRow(manifest as ExtensionManifest)))}
            >
                <div class="summary">
                    <button class=${`summary-card ${this._featuredOnly ? "active" : ""}`} @click=${() => (this._featuredOnly = !this._featuredOnly)}>
                        <strong>${this._items.filter((item) => FEATURE_TYPES.includes(item.type)).length}</strong>
                        <span>Core UI Types</span>
                    </button>
                    ${FEATURE_TYPES.map(
                        (type) => html`
                            <button class=${`summary-card ${this._type === type ? "active" : ""}`} @click=${() => (this._type = this._type === type ? "" : type)}>
                                <strong>${typeCounts.get(type) ?? 0}</strong>
                                <span>${type}</span>
                            </button>
                        `
                    )}
                </div>

                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Alias, type, package, condition, meta"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Type</label>
                            <select @change=${(e: Event) => (this._type = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${types.map((type) => html`<option value=${type}>${type}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Source</label>
                            <select @change=${(e: Event) => (this._package = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${packages.map((pkg) => html`<option value=${pkg}>${pkg}</option>`)}
                            </select>
                        </div>
                    </div>
                    <p class="help">
                        Source package is inferred from manifest package fields, aliases and script paths when available. Expand a row to inspect the raw manifest JSON.
                    </p>
                </uui-box>

                <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                <div class="manifest-table">
                    <div class="table-head">
                        <button @click=${() => this._sortBy("type")}>${this._sortLabel("type", "Type")}</button>
                        <button @click=${() => this._sortBy("alias")}>${this._sortLabel("alias", "Alias")}</button>
                        <button @click=${() => this._sortBy("name")}>${this._sortLabel("name", "Name")}</button>
                        <button @click=${() => this._sortBy("weight")}>${this._sortLabel("weight", "Weight")}</button>
                        <button @click=${() => this._sortBy("package")}>${this._sortLabel("package", "Source")}</button>
                        <span>Conditions</span>
                    </div>
                    ${results.map((item) => this._renderRow(item))}
                </div>
            </godmode-page>
        `;
    }

    private _renderRow(item: ExtensionRow) {
        return html`
            <button class="manifest-row" @click=${() => this._toggleExpanded(item.alias)}>
                <span><span class="pill">${item.type}</span>${item.kind ? html`<span class="kind">${item.kind}</span>` : ""}</span>
                <code>${item.alias}</code>
                <span>${item.name}</span>
                <span>${item.weight ?? ""}</span>
                <small>${item.package}</small>
                <span>
                    ${item.conditionCount ? html`<span class="condition-count">${item.conditionCount}</span>` : ""}
                    <small>${item.conditions}</small>
                </span>
            </button>
            ${this._expanded.has(item.alias)
                ? html`
                      <div class="details">
                          <dl>
                              <dt>Meta</dt>
                              <dd>${item.meta || "None"}</dd>
                              <dt>Raw manifest</dt>
                              <dd><pre>${JSON.stringify(item.manifest, this._jsonReplacer, 2)}</pre></dd>
                          </dl>
                      </div>
                  `
                : ""}
        `;
    }

    private _jsonReplacer(_key: string, value: unknown) {
        return typeof value === "function" ? "[Function]" : value;
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
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
            font-size: 1.3rem;
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
        small {
            color: var(--uui-color-text-alt);
        }
        .help {
            margin: var(--uui-size-space-4) 0 0;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
        }
        .manifest-table {
            display: grid;
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            overflow: hidden;
        }
        .table-head,
        .manifest-row {
            display: grid;
            grid-template-columns: minmax(190px, 1.1fr) minmax(260px, 1.6fr) minmax(180px, 1fr) minmax(80px, 0.45fr) minmax(140px, 0.8fr) minmax(220px, 1.4fr);
            align-items: start;
            column-gap: var(--uui-size-space-4);
            padding: var(--uui-size-space-3);
        }
        .table-head {
            font-weight: 700;
            background: var(--uui-color-surface-alt);
            border-bottom: 1px solid var(--uui-color-border);
        }
        .table-head button,
        .manifest-row {
            border: 0;
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
            font: inherit;
            text-align: left;
        }
        .table-head button {
            cursor: pointer;
            font-weight: 700;
            padding: 0;
            background: transparent;
        }
        .manifest-row {
            cursor: pointer;
            border-bottom: 1px solid var(--uui-color-border);
            width: 100%;
            box-shadow: inset 0 1px 0 color-mix(in srgb, var(--uui-color-surface) 72%, var(--uui-color-border));
            transition: background-color 120ms ease;
        }
        .manifest-row:nth-of-type(odd) {
            background: color-mix(in srgb, var(--uui-color-surface-alt) 38%, var(--uui-color-surface));
        }
        .manifest-row:hover {
            background: color-mix(in srgb, var(--uui-color-interactive) 7%, var(--uui-color-surface-alt));
        }
        .pill,
        .kind,
        .condition-count {
            display: inline-flex;
            border-radius: 3px;
            padding: 1px var(--uui-size-space-1);
            font-size: 0.75rem;
            font-weight: 600;
        }
        .pill {
            background: var(--uui-color-text);
            color: var(--uui-color-surface);
        }
        .kind,
        .condition-count {
            margin-left: var(--uui-size-space-1);
            background: var(--uui-color-border);
            color: var(--uui-color-text);
        }
        .details {
            grid-column: 1 / -1;
            background: var(--uui-color-surface);
            border-bottom: 1px solid var(--uui-color-border);
            padding: var(--uui-size-space-4);
        }
        dl {
            display: grid;
            grid-template-columns: 120px minmax(0, 1fr);
            gap: var(--uui-size-space-3);
            margin: 0;
            padding: var(--uui-size-space-4);
            background: var(--uui-color-surface-alt);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }
        dt {
            font-weight: 700;
            color: var(--uui-color-text-alt);
        }
        dd {
            margin: 0;
            min-width: 0;
            overflow-wrap: anywhere;
        }
        pre {
            max-height: 420px;
            overflow: auto;
            margin: 0;
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }
        @media (max-width: 900px) {
            .filters {
                grid-template-columns: 1fr;
            }
            .table-head {
                display: none;
            }
            .manifest-row {
                grid-template-columns: 1fr;
                row-gap: var(--uui-size-space-2);
            }
        }
    `;
}

export default GodModeExtensionExplorerElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-extension-explorer": GodModeExtensionExplorerElement;
    }
}
