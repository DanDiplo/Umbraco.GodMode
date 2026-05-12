import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import type { GodModeEvidenceDrawerData, GodModeEvidenceSection } from "../shared/evidence-drawer";

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
                ${this._renderItems(section.items)}
            </uui-box>
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
    `;
}

export default GodModeEvidenceDrawerElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-evidence-drawer": GodModeEvidenceDrawerElement;
    }
}
