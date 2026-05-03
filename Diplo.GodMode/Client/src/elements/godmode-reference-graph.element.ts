import { LitElement, css, customElement, html, state, svg } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ReferenceEdge } from "../shared/types";
import { uniqueBy } from "../shared/format";

interface GraphNode {
    key: string;
    type: string;
    name: string;
    alias: string;
    x: number;
    y: number;
}

type LookupMode = "all" | "used-by" | "uses";

@customElement("godmode-reference-graph")
export class GodModeReferenceGraphElement extends UmbElementMixin(LitElement) {
    private readonly _arrowMarkerId = `godmode-reference-arrow-${crypto.randomUUID()}`;
    @state() private _edges: ReferenceEdge[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _sourceType = "";
    @state() private _relation = "";
    @state() private _targetType = "";
    @state() private _mode: LookupMode = "all";
    @state() private _focusType = "";
    @state() private _focusKey = "";
    @state() private _view: "graph" | "table" = "table";

    override connectedCallback(): void {
        super.connectedCallback();
        this._readRouteFilters();
        void this._load();
    }

    private _readRouteFilters() {
        const params = new URLSearchParams(window.location.search);
        this._mode = (params.get("mode") as LookupMode) || "all";
        this._focusType = params.get("targetType") || params.get("sourceType") || "";
        this._focusKey = params.get("targetKey") || params.get("sourceKey") || "";
        if (this._mode === "used-by") {
            this._targetType = this._focusType;
        }
        if (this._mode === "uses") {
            this._sourceType = this._focusType;
        }
    }

    private async _load() {
        this._loading = true;
        try {
            if (this._mode === "used-by" && this._focusType && this._focusKey) {
                this._edges = await godmodeGet<ReferenceEdge[]>("references/used-by", {
                    targetType: this._focusType,
                    targetKey: this._focusKey
                });
            } else if (this._mode === "uses" && this._focusType && this._focusKey) {
                this._edges = await godmodeGet<ReferenceEdge[]>("references/uses", {
                    sourceType: this._focusType,
                    sourceKey: this._focusKey
                });
            } else {
                this._edges = await godmodeGet<ReferenceEdge[]>("reference-graph");
            }
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): ReferenceEdge[] {
        const q = this._search.trim().toLowerCase();
        return this._edges.filter((edge) => {
            if (this._sourceType && edge.sourceType !== this._sourceType) return false;
            if (this._relation && edge.relation !== this._relation) return false;
            if (this._targetType && edge.targetType !== this._targetType) return false;
            if (!q) return true;
            return [
                edge.sourceType,
                edge.sourceName,
                edge.sourceAlias,
                edge.relation,
                edge.targetType,
                edge.targetName,
                edge.targetAlias,
                edge.context ?? ""
            ].some((value) => value.toLowerCase().includes(q));
        });
    }

    private _setMode(mode: LookupMode) {
        this._mode = mode;
        if (mode === "all") {
            this._focusType = "";
            this._focusKey = "";
        }
        void this._load();
    }

    override render() {
        const results = this._filtered();
        const sourceTypes = uniqueBy(this._edges, "sourceType").map((x) => x.sourceType).sort();
        const relations = uniqueBy(this._edges, "relation").map((x) => x.relation).sort();
        const targetTypes = uniqueBy(this._edges, "targetType").map((x) => x.targetType).sort();

        return html`
            <godmode-page
                heading="Reverse Lookup Everywhere"
                description="Find what an item uses, what uses it, and how document types, data types, templates, partials and block elements relate."
                show-reload
                @reload=${() => void this._load()}
            >
                <uui-box>
                    <div class="mode-switch">
                        <uui-button compact look=${this._mode === "all" ? "primary" : "secondary"} @click=${() => this._setMode("all")}>All relationships</uui-button>
                        <uui-button compact look=${this._mode === "used-by" ? "primary" : "secondary"} @click=${() => this._setMode("used-by")}>Used by</uui-button>
                        <uui-button compact look=${this._mode === "uses" ? "primary" : "secondary"} @click=${() => this._setMode("uses")}>Uses</uui-button>
                    </div>
                    ${this._focusType && this._focusKey
                        ? html`<p class="focus">Focused on <strong>${this._focusType}</strong> <code>${this._focusKey}</code></p>`
                        : html`<p class="focus">Open an item from a browser with <strong>Used by</strong>, or filter the full relationship graph below.</p>`}
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by name, alias, relation or context"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Source</label>
                            <select @change=${(e: Event) => (this._sourceType = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${sourceTypes.map((type) => html`<option value=${type} ?selected=${type === this._sourceType}>${type}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Relation</label>
                            <select @change=${(e: Event) => (this._relation = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${relations.map((relation) => html`<option value=${relation}>${relation}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Target</label>
                            <select @change=${(e: Event) => (this._targetType = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${targetTypes.map((type) => html`<option value=${type} ?selected=${type === this._targetType}>${type}</option>`)}
                            </select>
                        </div>
                    </div>
                    <div class="view-switch">
                        <uui-button compact look=${this._view === "table" ? "primary" : "secondary"} @click=${() => (this._view = "table")}>Table</uui-button>
                        <uui-button compact look=${this._view === "graph" ? "primary" : "secondary"} @click=${() => (this._view = "graph")}>Graph</uui-button>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._edges.length}</strong> relationships</p>
                          ${results.length
                              ? this._view === "graph"
                                  ? this._renderGraph(results)
                                  : this._renderTable(results)
                              : html`<uui-box><p>No relationships match the current filters.</p></uui-box>`}
                      `}
            </godmode-page>
        `;
    }

    private _renderGraph(edges: ReferenceEdge[]) {
        const { nodes, visibleEdges } = this._layoutGraph(edges);
        const width = 1200;
        const height = Math.max(420, nodes.length * 30);
        const arrowUrl = `url(#${this._arrowMarkerId})`;

        return html`
            <uui-box>
                <div class="graph-wrap">
                    ${svg`
                        <svg viewBox=${`0 0 ${width} ${height}`} role="img" aria-label="Reference graph">
                            <defs>
                                <marker id=${this._arrowMarkerId} markerWidth="8" markerHeight="8" refX="7" refY="3" orient="auto" markerUnits="strokeWidth">
                                    <path d="M0,0 L0,6 L7,3 z"></path>
                                </marker>
                            </defs>
                            ${visibleEdges.map((edge) => {
                                const source = nodes.find((node) => node.key === this._nodeKey(edge.sourceType, edge.sourceKey));
                                const target = nodes.find((node) => node.key === this._nodeKey(edge.targetType, edge.targetKey));
                                if (!source || !target) return "";
                                return svg`
                                    <line class="edge" x1=${source.x} y1=${source.y} x2=${target.x} y2=${target.y} marker-end=${arrowUrl}>
                                        <title>${source.name} ${edge.relation} ${target.name}${edge.context ? ` (${edge.context})` : ""}</title>
                                    </line>
                                `;
                            })}
                            ${nodes.map(
                                (node) => svg`
                                    <g class=${`graph-node ${this._typeClass(node.type)}`} transform=${`translate(${node.x}, ${node.y})`}>
                                        <circle r="9"></circle>
                                        <text x="14" y="-2">${node.name}</text>
                                        <text x="14" y="12" class="alias">${node.type}${node.alias ? ` · ${node.alias}` : ""}</text>
                                        <title>${node.type}: ${node.name}${node.alias ? ` (${node.alias})` : ""}</title>
                                    </g>
                                `
                            )}
                        </svg>
                    `}
                </div>
                <div class="legend">
                    ${uniqueBy(nodes, "type").map((node) => html`<span class=${`legend-item ${this._typeClass(node.type)}`}><i></i>${node.type}</span>`)}
                </div>
            </uui-box>
        `;
    }

    private _renderTable(results: ReferenceEdge[]) {
        return html`
            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Source</uui-table-head-cell>
                    <uui-table-head-cell>Relation</uui-table-head-cell>
                    <uui-table-head-cell>Target</uui-table-head-cell>
                    <uui-table-head-cell>Context</uui-table-head-cell>
                </uui-table-head>
                ${results.map(
                    (edge) => html`
                        <uui-table-row>
                            <uui-table-cell>${this._renderNode(edge.sourceType, edge.sourceName, edge.sourceAlias, edge.sourceKey)}</uui-table-cell>
                            <uui-table-cell><span class="relation">${edge.relation}</span></uui-table-cell>
                            <uui-table-cell>${this._renderNode(edge.targetType, edge.targetName, edge.targetAlias, edge.targetKey)}</uui-table-cell>
                            <uui-table-cell>${edge.context ? html`<small>${edge.context}</small>` : ""}</uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    private _layoutGraph(edges: ReferenceEdge[]): { nodes: GraphNode[]; visibleEdges: ReferenceEdge[] } {
        const maxEdges = 260;
        const visibleEdges = edges.slice(0, maxEdges);
        const byKey = new Map<string, GraphNode>();

        for (const edge of visibleEdges) {
            this._ensureNode(byKey, edge.sourceType, edge.sourceName, edge.sourceAlias, edge.sourceKey);
            this._ensureNode(byKey, edge.targetType, edge.targetName, edge.targetAlias, edge.targetKey);
        }

        const typeOrder = ["Content Node", "Document Type", "Element Type", "Media Type", "Member Type", "Data Type", "Template", "Partial"];
        const grouped = Array.from(byKey.values()).sort((a, b) => {
            const typeDiff = this._typeIndex(typeOrder, a.type) - this._typeIndex(typeOrder, b.type);
            return typeDiff || a.name.localeCompare(b.name);
        });
        const groups = uniqueBy(grouped, "type").map((node) => node.type);
        const columnWidth = 1100 / Math.max(groups.length, 1);
        const rowGap = 38;

        for (const type of groups) {
            const groupNodes = grouped.filter((node) => node.type === type);
            const column = groups.indexOf(type);
            groupNodes.forEach((node, index) => {
                node.x = 60 + column * columnWidth;
                node.y = 42 + index * rowGap;
            });
        }

        return { nodes: grouped, visibleEdges };
    }

    private _ensureNode(nodes: Map<string, GraphNode>, type: string, name: string, alias: string, key: string) {
        const nodeKey = this._nodeKey(type, key);
        if (nodes.has(nodeKey)) return;
        nodes.set(nodeKey, { key: nodeKey, type, name, alias, x: 0, y: 0 });
    }

    private _nodeKey(type: string, key: string): string {
        return `${type}:${key}`;
    }

    private _typeIndex(typeOrder: string[], type: string): number {
        const index = typeOrder.indexOf(type);
        return index === -1 ? typeOrder.length : index;
    }

    private _typeClass(type: string): string {
        return `type-${type.toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
    }

    private _renderNode(type: string, name: string, alias: string, key: string) {
        return html`
            <div class="node-summary">
                <strong>${name}</strong>
                <small>${type}${alias ? html` · <code>${alias}</code>` : ""}</small>
                <small><code>${key}</code></small>
            </div>
        `;
    }

    static override styles = css`
        .mode-switch,
        .view-switch {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
        }
        .focus {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        .filters {
            display: grid;
            grid-template-columns: 2fr repeat(3, 1fr);
            gap: var(--uui-size-space-4);
        }
        .view-switch {
            margin-top: var(--uui-size-space-4);
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
        .node-summary {
            display: grid;
            gap: 2px;
        }
        .node-summary small {
            color: var(--uui-color-text-alt);
        }
        .relation {
            font-weight: 600;
        }
        .graph-wrap {
            overflow: auto;
            max-height: 70vh;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }
        svg {
            width: 100%;
            min-width: 980px;
            display: block;
        }
        marker path {
            fill: var(--uui-color-border-emphasis);
        }
        .edge {
            stroke: var(--uui-color-border-emphasis);
            stroke-width: 1.2;
            opacity: 0.55;
        }
        .graph-node circle {
            stroke: var(--uui-color-surface);
            stroke-width: 2;
            fill: #607d8b;
        }
        .graph-node text {
            font-size: 13px;
            fill: var(--uui-color-text);
            paint-order: stroke;
            stroke: var(--uui-color-surface);
            stroke-width: 3px;
            stroke-linejoin: round;
        }
        .graph-node .alias {
            font-size: 10px;
            fill: var(--uui-color-text-alt);
        }
        .type-document-type circle,
        .type-document-type i {
            fill: #2f80ed;
        }
        .type-element-type circle,
        .type-element-type i {
            fill: #219653;
        }
        .type-data-type circle,
        .type-data-type i {
            fill: #9b51e0;
        }
        .type-template circle,
        .type-template i {
            fill: #f2994a;
        }
        .type-partial circle,
        .type-partial i {
            fill: #d9480f;
        }
        .legend {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-4);
            margin-top: var(--uui-size-space-3);
        }
        .legend-item {
            display: inline-flex;
            align-items: center;
            gap: var(--uui-size-space-1);
            color: var(--uui-color-text-alt);
        }
        .legend i {
            display: inline-block;
            width: 10px;
            height: 10px;
            border-radius: 50%;
        }
        @media (max-width: 900px) {
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeReferenceGraphElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-reference-graph": GodModeReferenceGraphElement;
    }
}
