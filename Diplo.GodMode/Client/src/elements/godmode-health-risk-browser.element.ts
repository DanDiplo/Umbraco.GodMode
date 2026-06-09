import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import { uniqueBy } from "../shared/format";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import "../shared";
import type { HealthRiskFinding, ReferenceEdge } from "../shared/types";
import { openLazyEvidenceDrawer } from "../shared/evidence-drawer";

@customElement("godmode-health-risk-browser")
export class GodModeHealthRiskBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _findings: HealthRiskFinding[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _severity = "";
    @state() private _category = "";
    @state() private _sort: SortState = { column: "score", reverse: true };

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            this._findings = await godmodeGet<HealthRiskFinding[]>("health-risk-findings");
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): HealthRiskFinding[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._findings.filter((finding) => {
            if (this._severity && finding.severity !== this._severity) return false;
            if (this._category && finding.category !== this._category) return false;
            if (!q) return true;
            return [
                finding.severity,
                finding.category,
                finding.title,
                finding.detail,
                finding.entityType,
                finding.entityName,
                finding.entityAlias,
                finding.recommendation
            ].some((value) => value.toLowerCase().includes(q));
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as HealthRiskFinding[];
    }

    private _count(severity: string): number {
        return this._findings.filter((finding) => finding.severity === severity).length;
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const results = this._filtered();
        const severities = ["High", "Medium", "Low", "Info"].filter((severity) => this._count(severity) > 0);
        const categories = uniqueBy(this._findings, "category").map((x) => x.category).sort();

        return html`
            <godmode-page
                heading="Health & Risk Findings"
                description="Opinionated checks that highlight schema drift, broken references and cleanup opportunities."
                show-reload
                @reload=${() => void this._load()}
            >
                <div class="summary">
                    ${["High", "Medium", "Low"].map(
                        (severity) => html`
                            <button class=${`summary-card ${this._severity === severity ? "active" : ""}`} @click=${() => (this._severity = this._severity === severity ? "" : severity)}>
                                <span class=${`dot ${severity.toLowerCase()}`}></span>
                                <strong>${this._count(severity)}</strong>
                                <span>${severity}</span>
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
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by issue, entity or recommendation"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Severity</label>
                            <select @change=${(e: Event) => (this._severity = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${severities.map((severity) => html`<option value=${severity} ?selected=${severity === this._severity}>${severity}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Category</label>
                            <select @change=${(e: Event) => (this._category = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${categories.map((category) => html`<option value=${category} ?selected=${category === this._category}>${category}</option>`)}
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._findings.length}</strong> findings</p>
                          ${results.length ? this._renderTable(results) : html`<uui-box><p>No findings match the current filters.</p></uui-box>`}
                      `}
            </godmode-page>
        `;
    }

    private _renderTable(results: HealthRiskFinding[]) {
        return html`
            <uui-table @sort-change=${this._onSortChange}>
                <uui-table-head>
                    <godmode-sort-header column="score" .sort=${this._sort}>Risk</godmode-sort-header>
                    <godmode-sort-header column="category" .sort=${this._sort}>Category</godmode-sort-header>
                    <godmode-sort-header column="entityName" .sort=${this._sort}>Entity</godmode-sort-header>
                    <godmode-sort-header column="title" .sort=${this._sort}>Finding</godmode-sort-header>
                    <uui-table-head-cell>Recommendation</uui-table-head-cell>
                    <uui-table-head-cell>Actions</uui-table-head-cell>
                </uui-table-head>
                ${results.map(
                    (finding) => html`
                        <uui-table-row>
                            <uui-table-cell>
                                <span class=${`badge ${finding.severity.toLowerCase()}`}>${finding.severity}</span>
                            </uui-table-cell>
                            <uui-table-cell>${finding.category}</uui-table-cell>
                            <uui-table-cell>
                                <strong>${finding.entityName}</strong>
                                <small>${finding.entityType}${finding.entityAlias ? html` · <code>${finding.entityAlias}</code>` : ""}</small>
                            </uui-table-cell>
                            <uui-table-cell>
                                <strong>${finding.title}</strong>
                                <small>${finding.detail}</small>
                            </uui-table-cell>
                            <uui-table-cell><small>${finding.recommendation}</small></uui-table-cell>
                            <uui-table-cell class="action-cell">
                                <uui-button compact look="secondary" label="Evidence" @click=${(e: Event) => void this._openEvidence(finding, e)}>
                                    <uui-icon name="icon-search"></uui-icon>
                                    Evidence
                                </uui-button>
                                <godmode-ai-explain-host .subject=${this._explainSubject(finding)}></godmode-ai-explain-host>
                            </uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    private _openEvidence(finding: HealthRiskFinding, e: Event) {
        openLazyEvidenceDrawer(
            this,
            {
                title: `Evidence: ${finding.title}`,
                subtitle: finding.detail,
                sections: [],
                load: async () => {
                    const hasReferenceTarget = !!finding.entityType && !!finding.entityKey;
                    const [usedBy, uses] = hasReferenceTarget
                        ? await Promise.all([
                              godmodeGet<ReferenceEdge[]>("references/used-by", { targetType: finding.entityType, targetKey: finding.entityKey }),
                              godmodeGet<ReferenceEdge[]>("references/uses", { sourceType: finding.entityType, sourceKey: finding.entityKey })
                          ])
                        : [[], []];

                    return {
                        title: `Evidence: ${finding.title}`,
                        subtitle: finding.detail,
                        summary: [
                            { label: "Severity", value: finding.severity },
                            { label: "Score", value: finding.score },
                            { label: "Category", value: finding.category },
                            { label: "Entity", value: finding.entityName },
                            { label: "Type", value: finding.entityType },
                            { label: "Alias", value: finding.entityAlias }
                        ],
                        sections: [
                            {
                                heading: "Finding",
                                items: {
                                    title: finding.title,
                                    detail: finding.detail,
                                    recommendation: finding.recommendation,
                                    entityKey: finding.entityKey
                                }
                            },
                            {
                                heading: "Used By",
                                description: "Reference graph edges where this entity is the target.",
                                items: usedBy
                            },
                            {
                                heading: "Uses",
                                description: "Reference graph edges where this entity is the source.",
                                items: uses
                            }
                        ]
                    };
                }
            },
            e
        );
    }

    private _explainSubject(finding: HealthRiskFinding) {
        return {
            subjectType: "God Mode health and risk finding",
            title: finding.title,
            data: {
                severity: finding.severity,
                score: finding.score,
                category: finding.category,
                title: finding.title,
                detail: finding.detail,
                entityType: finding.entityType,
                entityName: finding.entityName,
                entityAlias: finding.entityAlias,
                recommendation: finding.recommendation
            }
        };
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .summary-card {
            display: grid;
            grid-template-columns: auto auto 1fr;
            align-items: center;
            gap: var(--uui-size-space-2);
            min-height: 52px;
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
            font-size: 1.4rem;
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
        .filters uui-input {
            width: 100%;
        }
        select {
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
        small {
            display: block;
            color: var(--uui-color-text-alt);
            margin-top: var(--uui-size-space-1);
        }
        .badge {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            min-width: 64px;
            border-radius: 999px;
            padding: 2px var(--uui-size-space-2);
            color: white;
            font-weight: 700;
            font-size: 12px;
        }
        .dot {
            display: inline-block;
            width: 10px;
            height: 10px;
            border-radius: 50%;
        }
        .high {
            background: #c92a2a;
        }
        .medium {
            background: #f08c00;
        }
        .low {
            background: #2f80ed;
        }
        .info {
            background: #6c757d;
        }
        .action-cell {
            display: flex;
            gap: var(--uui-size-space-2);
            justify-content: flex-end;
            width: 13rem;
        }
        .action-cell uui-button,
        .action-cell godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
        @media (max-width: 900px) {
            .summary,
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeHealthRiskBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-health-risk-browser": GodModeHealthRiskBrowserElement;
    }
}
