import { LitElement, css, customElement, html, property, svg } from "@umbraco-cms/backoffice/external/lit";
import type { GodModeEvidenceDrawerData, GodModeEvidenceSection } from "../shared/evidence-drawer";

interface DatabaseRelationshipGraphData {
    center: {
        name: string;
        category?: string;
        rowCount?: number;
        countSucceeded?: boolean;
        warning?: string;
    };
    incoming: Array<{
        constraintName: string;
        fromTable: string;
        fromColumn: string;
        toTable: string;
        toColumn: string;
    }>;
    outgoing: Array<{
        constraintName: string;
        fromTable: string;
        fromColumn: string;
        toTable: string;
        toColumn: string;
    }>;
}

@customElement("godmode-evidence-drawer")
export class GodModeEvidenceDrawerElement extends LitElement {
    @property({ attribute: false }) modalContext?: { reject: () => void };
    @property({ type: Object, attribute: false }) data?: GodModeEvidenceDrawerData;

    override connectedCallback(): void {
        super.connectedCallback();
        window.addEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
    }

    override disconnectedCallback(): void {
        window.removeEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
        super.disconnectedCallback();
    }

    private _close = () => {
        this.modalContext?.reject();
    };

    private _onOutsidePointerDown = (e: PointerEvent) => {
        if (e.composedPath().includes(this)) {
            return;
        }

        this._close();
    };

    override render() {
        const data = this.data;
        return html`
            <uui-dialog-layout headline=${data?.title || "Evidence"}>
                <uui-button class="top-close" compact look="secondary" label="Close" @click=${this._close}>
                    <uui-icon name="icon-wrong"></uui-icon>
                </uui-button>
                ${data?.subtitle ? html`<p class="subtitle">${data.subtitle}</p>` : ""}
                ${data?.summary?.length ? this._renderSummary(data.summary) : ""}
                <div class="sections">
                    ${(data?.sections ?? []).map((section) => this._renderSection(section))}
                </div>
            </uui-dialog-layout>
        `;
    }

    private _renderSummary(summary: Array<{ label: string; value: unknown }>) {
        return html`
            <dl class="summary">
                ${summary.map(
                    (item) => html`
                        <div>
                            <dt>${item.label}</dt>
                            <dd>${this._formatScalar(item.value)}</dd>
                        </div>
                    `
                )}
            </dl>
        `;
    }

    private _renderSection(section: GodModeEvidenceSection) {
        return html`
            <uui-box headline=${section.heading}>
                ${section.description ? html`<p class="section-description">${section.description}</p>` : ""}
                ${section.visual === "database-relationship-graph" ? this._renderDatabaseRelationshipGraph(section.items) : this._renderItems(section.items)}
            </uui-box>
        `;
    }

    private _renderDatabaseRelationshipGraph(items: unknown) {
        if (!this._isDatabaseRelationshipGraphData(items)) {
            return this._renderItems(items);
        }

        const incoming = items.incoming.slice(0, 8);
        const outgoing = items.outgoing.slice(0, 8);
        const rowGap = 58;
        const leftCount = Math.max(incoming.length, 1);
        const rightCount = Math.max(outgoing.length, 1);
        const height = Math.max(230, Math.max(leftCount, rightCount) * rowGap + 92);
        const centerY = height / 2;
        const center = { x: 520, y: centerY };
        const leftX = 150;
        const rightX = 890;
        const leftStartY = centerY - ((incoming.length - 1) * rowGap) / 2;
        const rightStartY = centerY - ((outgoing.length - 1) * rowGap) / 2;
        const rowText = items.center.countSucceeded === false ? "Rows unknown" : `${(items.center.rowCount ?? 0).toLocaleString()} rows`;
        const hasMore = items.incoming.length > incoming.length || items.outgoing.length > outgoing.length;

        if (!items.incoming.length && !items.outgoing.length) {
            return html`<p class="empty">No declared database relationships found for this table.</p>`;
        }

        return html`
            <div class="db-graph-wrap">
                ${svg`
                    <svg viewBox=${`0 0 1040 ${height}`} role="img" aria-label=${`Database relationships for ${items.center.name}`}>
                        <defs>
                            <marker id="godmode-db-arrow" markerWidth="8" markerHeight="8" refX="7" refY="3" orient="auto" markerUnits="strokeWidth">
                                <path d="M0,0 L0,6 L7,3 z"></path>
                            </marker>
                        </defs>
                        ${incoming.map((relation, index) => {
                            const y = leftStartY + index * rowGap;
                            return svg`
                                <line class="db-edge incoming" x1=${leftX + 130} y1=${y} x2=${center.x - 130} y2=${center.y} marker-end="url(#godmode-db-arrow)">
                                    <title>${relation.fromTable}.${relation.fromColumn} -> ${relation.toTable}.${relation.toColumn}</title>
                                </line>
                                ${this._renderGraphNode(leftX, y, relation.fromTable, relation.fromColumn, relation.constraintName, "incoming")}
                            `;
                        })}
                        ${outgoing.map((relation, index) => {
                            const y = rightStartY + index * rowGap;
                            return svg`
                                <line class="db-edge outgoing" x1=${center.x + 130} y1=${center.y} x2=${rightX - 130} y2=${y} marker-end="url(#godmode-db-arrow)">
                                    <title>${relation.fromTable}.${relation.fromColumn} -> ${relation.toTable}.${relation.toColumn}</title>
                                </line>
                                ${this._renderGraphNode(rightX, y, relation.toTable, relation.toColumn, relation.constraintName, "outgoing")}
                            `;
                        })}
                        <g class=${items.center.warning ? "db-node center warning" : "db-node center"} transform=${`translate(${center.x}, ${center.y})`}>
                            <rect x="-132" y="-36" width="264" height="72" rx="6"></rect>
                            <text class="name" y="-8">${items.center.name}</text>
                            <text class="meta" y="12">${items.center.category || "Table"} · ${rowText}</text>
                            ${items.center.warning ? svg`<text class="warning-text" y="28">${items.center.warning}</text>` : ""}
                            <title>${items.center.name}${items.center.warning ? `: ${items.center.warning}` : ""}</title>
                        </g>
                    </svg>
                `}
            </div>
            ${hasMore ? html`<p class="section-description">Showing the first ${incoming.length} incoming and ${outgoing.length} outgoing relationships. Full details are listed below.</p>` : ""}
        `;
    }

    private _renderGraphNode(x: number, y: number, name: string, column: string, constraintName: string, direction: "incoming" | "outgoing") {
        return svg`
            <g class=${`db-node ${direction}`} transform=${`translate(${x}, ${y})`}>
                <rect x="-130" y="-25" width="260" height="50" rx="6"></rect>
                <text class="name" y="-4">${name}</text>
                <text class="meta" y="13">${column}</text>
                <title>${constraintName}</title>
            </g>
        `;
    }

    private _renderItems(items: unknown) {
        if (Array.isArray(items)) {
            if (!items.length) return html`<p class="empty">No evidence found.</p>`;
            if (items.every((item) => this._isRecord(item))) {
                const columns = this._columns(items as Array<Record<string, unknown>>);
                return html`
                    <uui-table>
                        <uui-table-head>
                            ${columns.map((column) => html`<uui-table-head-cell>${this._label(column)}</uui-table-head-cell>`)}
                        </uui-table-head>
                        ${(items as Array<Record<string, unknown>>).map(
                            (item) => html`
                                <uui-table-row>
                                    ${columns.map((column) => html`<uui-table-cell>${this._renderValue(item[column])}</uui-table-cell>`)}
                                </uui-table-row>
                            `
                        )}
                    </uui-table>
                `;
            }
            return html`<ul>${items.map((item) => html`<li>${this._renderValue(item)}</li>`)}</ul>`;
        }

        if (this._isRecord(items)) {
            return html`
                <dl class="key-values">
                    ${Object.entries(items).map(
                        ([key, value]) => html`
                            <div>
                                <dt>${this._label(key)}</dt>
                                <dd>${this._renderValue(value)}</dd>
                            </div>
                        `
                    )}
                </dl>
            `;
        }

        return html`<p>${this._formatScalar(items)}</p>`;
    }

    private _renderValue(value: unknown): unknown {
        if (Array.isArray(value)) {
            return value.length ? html`<ul class="compact">${value.map((item): unknown => html`<li>${this._renderValue(item)}</li>`)}</ul>` : html`<span class="muted">None</span>`;
        }

        if (this._isRecord(value)) {
            return html`<pre>${JSON.stringify(value, null, 2)}</pre>`;
        }

        if (typeof value === "string") {
            return html`<code>${this._formatScalar(value)}</code>`;
        }

        return html`<span>${this._formatScalar(value)}</span>`;
    }

    private _formatScalar(value: unknown): string {
        if (value === null || value === undefined || value === "") return "None";
        if (typeof value === "boolean") return value ? "Yes" : "No";
        return String(value);
    }

    private _isRecord(value: unknown): value is Record<string, unknown> {
        return typeof value === "object" && value !== null && !Array.isArray(value);
    }

    private _isDatabaseRelationshipGraphData(value: unknown): value is DatabaseRelationshipGraphData {
        return (
            this._isRecord(value) &&
            this._isRecord(value.center) &&
            typeof value.center.name === "string" &&
            Array.isArray(value.incoming) &&
            Array.isArray(value.outgoing)
        );
    }

    private _columns(items: Array<Record<string, unknown>>): string[] {
        return Array.from(new Set(items.flatMap((item) => Object.keys(item)))).slice(0, 8);
    }

    private _label(value: string): string {
        return value.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/^./, (match) => match.toUpperCase());
    }

    static override styles = css`
        uui-dialog-layout {
            position: relative;
            width: min(1120px, 94vw);
            max-height: 84vh;
        }
        .top-close {
            position: absolute;
            top: var(--uui-size-space-4);
            right: var(--uui-size-space-4);
            z-index: 1;
        }
        .subtitle,
        .section-description,
        .empty,
        .muted {
            color: var(--uui-color-text-alt);
        }
        .subtitle {
            margin: 0 0 var(--uui-size-space-4);
        }
        .sections {
            display: grid;
            gap: var(--uui-size-space-4);
        }
        .summary,
        .key-values {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: var(--uui-size-space-3);
            margin: 0 0 var(--uui-size-space-4);
        }
        .summary div,
        .key-values div {
            min-width: 0;
        }
        dt {
            font-size: 0.78rem;
            color: var(--uui-color-text-alt);
            margin-bottom: var(--uui-size-space-1);
        }
        dd {
            margin: 0;
            font-weight: 600;
            overflow-wrap: anywhere;
        }
        code,
        pre {
            font-family: Consolas, "Liberation Mono", Menlo, Monaco, "Courier New", monospace;
        }
        code {
            color: var(--uui-color-text);
            font-size: 0.92em;
            overflow-wrap: anywhere;
        }
        uui-table {
            display: block;
            overflow-x: auto;
        }
        ul {
            margin: 0;
            padding-left: var(--uui-size-space-5);
        }
        .compact {
            display: grid;
            gap: var(--uui-size-space-1);
        }
        pre {
            margin: 0;
            max-width: 32rem;
            white-space: pre-wrap;
            overflow-wrap: anywhere;
            color: var(--uui-color-text-alt);
            font-size: 0.78rem;
        }
        .db-graph-wrap {
            overflow-x: auto;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }
        .db-graph-wrap svg {
            display: block;
            min-width: 820px;
            width: 100%;
        }
        marker path {
            fill: var(--uui-color-border-emphasis);
        }
        .db-edge {
            stroke: var(--uui-color-border-emphasis);
            stroke-width: 1.5;
            opacity: 0.58;
        }
        .db-edge.outgoing {
            stroke: var(--uui-color-selected);
        }
        .db-node rect {
            fill: var(--uui-color-surface-alt);
            stroke: var(--uui-color-border);
        }
        .db-node.center rect {
            fill: var(--uui-color-selected);
            stroke: var(--uui-color-selected);
        }
        .db-node.warning rect {
            fill: var(--uui-color-warning-emphasis);
            stroke: var(--uui-color-warning-emphasis);
        }
        .db-node text {
            text-anchor: middle;
            paint-order: stroke;
            stroke: var(--uui-color-surface);
            stroke-width: 3px;
            stroke-linejoin: round;
        }
        .db-node.center text {
            fill: var(--uui-color-selected-contrast);
            stroke: transparent;
        }
        .db-node.warning text {
            fill: var(--uui-color-warning-contrast);
            stroke: transparent;
        }
        .db-node .name {
            font-size: 13px;
            font-weight: 700;
        }
        .db-node .meta,
        .db-node .warning-text {
            font-size: 10px;
        }
        .db-node .meta {
            fill: var(--uui-color-text-alt);
        }
        .db-node.center .meta,
        .db-node.center .warning-text {
            fill: currentColor;
        }
    `;
}

export default GodModeEvidenceDrawerElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-evidence-drawer": GodModeEvidenceDrawerElement;
    }
}
