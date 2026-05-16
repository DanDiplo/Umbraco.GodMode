import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { GodModeAiExplainSubject } from "../shared/godmode-ai-explain-host.element";
import type { GodModeLogEvent, GodModeLogInsight, GodModeLogLevelCount, GodModeLogOverview, GodModeSavedLogQuery, Page } from "../shared/types";

const LOG_LEVELS = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"] as const;

@customElement("godmode-log-browser")
export class GodModeLogBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _overview?: GodModeLogOverview;
    @state() private _page?: Page<GodModeLogEvent>;
    @state() private _insights: GodModeLogInsight[] = [];
    @state() private _levelCounts: GodModeLogLevelCount[] = [];
    @state() private _savedQueries: GodModeSavedLogQuery[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _level = "";
    @state() private _savedQueryId = "";
    @state() private _fromDate = this._dateInputValue(new Date(Date.now() - 24 * 60 * 60 * 1000));
    @state() private _toDate = this._dateInputValue(new Date());
    @state() private _currentPage = 1;
    @state() private _expandedId = "";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load(page = this._currentPage) {
        this._loading = true;
        this._currentPage = page;

        try {
            const queryExpression = this._selectedSavedQuery()?.query;
            const [overview, logs, insights, levelCounts, savedQueries] = await Promise.all([
                godmodeGet<GodModeLogOverview>("logs/overview"),
                godmodeGet<Page<GodModeLogEvent>>("logs", {
                    page,
                    pageSize: 25,
                    from: this._startOfDay(this._fromDate),
                    to: this._endOfDay(this._toDate),
                    level: this._level,
                    search: this._search,
                    queryExpression
                }),
                godmodeGet<GodModeLogInsight[]>("logs/insights", {
                    from: this._startOfDay(this._fromDate),
                    to: this._endOfDay(this._toDate),
                    take: 8
                }),
                godmodeGet<GodModeLogLevelCount[]>("logs/level-counts", {
                    from: this._startOfDay(this._fromDate),
                    to: this._endOfDay(this._toDate),
                    search: this._search,
                    queryExpression
                }),
                this._savedQueries.length ? Promise.resolve(this._savedQueries) : godmodeGet<GodModeSavedLogQuery[]>("logs/saved-queries")
            ]);

            this._overview = overview;
            this._page = logs;
            this._insights = insights;
            this._levelCounts = levelCounts;
            this._savedQueries = savedQueries;
        } finally {
            this._loading = false;
        }
    }

    private _submitFilters(e: Event) {
        e.preventDefault();
        void this._load(1);
    }

    private _toggle(log: GodModeLogEvent) {
        this._expandedId = this._expandedId === log.id ? "" : log.id;
    }

    private _onRowKeydown(log: GodModeLogEvent, e: KeyboardEvent) {
        if (e.key !== "Enter" && e.key !== " ") return;

        e.preventDefault();
        this._toggle(log);
    }

    private _go(page: number) {
        const totalPages = this._page?.totalPages ?? 1;
        if (page < 1 || page > totalPages || page === this._currentPage) return;
        void this._load(page);
    }

    private _selectedSavedQuery(): GodModeSavedLogQuery | undefined {
        return this._savedQueries.find((query) => query.id === this._savedQueryId);
    }

    private _onLevelChange(e: Event) {
        this._level = (e.target as HTMLSelectElement).value;
        void this._load(1);
    }

    private _onSavedQueryChange(e: Event) {
        this._savedQueryId = (e.target as HTMLSelectElement).value;
        if (this._savedQueryId) {
            this._search = "";
            this._level = "";
        }

        void this._load(1);
    }

    private _properties(log: GodModeLogEvent) {
        return Object.entries(log.properties ?? {}).filter(([key]) => !key.startsWith("@"));
    }

    private _explainSubject(log: GodModeLogEvent): GodModeAiExplainSubject {
        const stackFrames = this._extractStackFrames(log.exception);
        const applicationFrames = stackFrames.filter((frame) => frame.type.startsWith("Diplo.") || frame.file?.includes("\\Diplo.GodMode\\") || frame.file?.includes("/Diplo.GodMode/"));
        const firstActionableFrame = applicationFrames[0] ?? stackFrames.find((frame) => frame.file) ?? stackFrames[0];
        const isErrorLike = Boolean(log.exception) || stackFrames.length > 0 || log.level === "Error" || log.level === "Fatal";

        return {
            subjectType: "Umbraco Log Event",
            title: `${log.level}: ${log.message || log.messageTemplate}`,
            data: {
                timestamp: log.timestamp,
                level: log.level,
                message: log.message,
                messageTemplate: log.messageTemplate,
                exception: log.exception,
                sourceContext: log.sourceContext,
                requestId: log.requestId,
                requestPath: log.requestPath,
                machineName: log.machineName,
                processId: log.processId,
                threadId: log.threadId,
                logFile: log.logFile,
                properties: log.properties,
                stackFrames,
                applicationFrames,
                firstActionableFrame,
                isErrorLike
            },
            context: {
                tool: "GodMode Log Browser",
                logFolder: this._overview?.logFolder,
                explanationMode: isErrorLike ? "exception-diagnostic" : "operational-log-event",
                firstActionableFrame,
                note: "This is a Serilog JSON event from Umbraco logs. Explain the likely cause, operational impact, and practical next steps."
            }
        };
    }

    private _insightSubject(insight: GodModeLogInsight): GodModeAiExplainSubject {
        const sample = insight.sample ?? insight.samples[0];
        const sampleFrames = sample ? this._extractStackFrames(sample.exception) : [];
        const applicationFrames = sampleFrames.filter((frame) => frame.type.startsWith("Diplo.") || frame.file?.includes("\\Diplo.GodMode\\") || frame.file?.includes("/Diplo.GodMode/"));
        const firstActionableFrame = applicationFrames[0] ?? sampleFrames.find((frame) => frame.file) ?? sampleFrames[0];

        return {
            subjectType: "Umbraco Log Insight",
            title: `${insight.level}: ${insight.title}`,
            data: {
                level: insight.level,
                title: insight.title,
                count: insight.count,
                firstSeen: insight.firstSeen,
                lastSeen: insight.lastSeen,
                sourceContext: insight.sourceContext,
                requestPaths: insight.requestPaths,
                exceptionType: insight.exceptionType,
                normalizedMessage: insight.normalizedMessage,
                sample,
                samples: insight.samples,
                sampleStackFrames: sampleFrames,
                applicationFrames,
                firstActionableFrame,
                isErrorLike: insight.level === "Error" || insight.level === "Fatal" || Boolean(insight.exceptionType)
            },
            context: {
                tool: "GodMode Log Browser",
                explanationMode: "grouped-log-pattern",
                firstActionableFrame,
                note: "This is a grouped pattern of repeated Umbraco log events. Explain the shared cause, impact, priority, and the most useful next action."
            }
        };
    }

    private _renderExpanded(log: GodModeLogEvent) {
        const rows = [
            ["Timestamp", this._formatDate(log.timestamp)],
            ["@MessageTemplate", log.messageTemplate || "None"],
            ...this._properties(log).map(([key, value]) => [this._formatLabel(key), this._formatValue(value)])
        ];

        return html`
            <div class="details" @click=${(e: Event) => e.stopPropagation()}>
                <div class="detail-actions">
                    <godmode-ai-explain-host .subject=${this._explainSubject(log)}></godmode-ai-explain-host>
                </div>
                ${log.exception ? html`<pre class="exception">${log.exception}</pre>` : ""}
                <div class="properties">
                    ${rows.map(
                        ([key, value]) => html`
                            <div class="property-label">${key}</div>
                            <div class="property-value">${value}</div>
                        `
                    )}
                </div>
            </div>
        `;
    }

    override render() {
        const logs = this._page?.items ?? [];
        const total = this._page?.totalItems ?? 0;
        const totalPages = Math.max(1, this._page?.totalPages ?? 1);

        return html`
            <godmode-page heading="Log Browser" description="Inspect Umbraco JSON logs and explain individual entries with AI." show-reload @reload=${() => void this._load()}>
                <div class="summary">
                    <div>
                        <span>Log Files</span>
                        <strong>${this._overview?.exists ? this._overview.fileCount.toLocaleString() : "Missing"}</strong>
                    </div>
                    <div>
                        <span>Matching Events</span>
                        <strong>${total.toLocaleString()}</strong>
                    </div>
                    <div>
                        <span>Folder</span>
                        <code>${this._overview?.logFolder ?? "Loading..."}</code>
                    </div>
                </div>
                ${this._renderLevelCounts()}

                <uui-box>
                    <form class="filters" @submit=${this._submitFilters}>
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                name="godmode-log-search"
                                placeholder="Message, exception, source, request"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Level</label>
                            <select @change=${(e: Event) => this._onLevelChange(e)}>
                                <option value="">All</option>
                                ${LOG_LEVELS.map((level) => html`<option value=${level} ?selected=${level === this._level}>${level}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Saved Query</label>
                            <select @change=${(e: Event) => this._onSavedQueryChange(e)}>
                                <option value="">None</option>
                                ${this._savedQueries.map((query) => html`<option value=${query.id} ?selected=${query.id === this._savedQueryId}>${query.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>From</label>
                            <input type="date" .value=${this._fromDate} @input=${(e: Event) => (this._fromDate = (e.target as HTMLInputElement).value)} />
                        </div>
                        <div>
                            <label>To</label>
                            <input type="date" .value=${this._toDate} @input=${(e: Event) => (this._toDate = (e.target as HTMLInputElement).value)} />
                        </div>
                        <uui-button type="submit" look="primary" label="Apply filters">
                            <uui-icon name="icon-search"></uui-icon>
                            Apply
                        </uui-button>
                    </form>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          ${this._renderInsights()}
                          <div class="results">
                              <span>Page ${this._currentPage} of ${totalPages}</span>
                              <span>${logs.length} shown</span>
                          </div>
                          <div class="log-table">
                              <div class="log-head">
                                  <span>Timestamp</span>
                                  <span>Level</span>
                                  <span>Machine</span>
                                  <span>Message</span>
                              </div>
                              ${logs.map(
                                  (log) => html`
                                      <div
                                          class=${this._expandedId === log.id ? "log-row expanded" : "log-row"}
                                          role="button"
                                          tabindex="0"
                                          aria-expanded=${this._expandedId === log.id ? "true" : "false"}
                                          @click=${() => this._toggle(log)}
                                          @keydown=${(e: KeyboardEvent) => this._onRowKeydown(log, e)}
                                      >
                                          <div>
                                              <time>${this._formatDate(log.timestamp)}</time>
                                              <small>${log.logFile}</small>
                                          </div>
                                          <div><uui-tag color=${this._levelColor(log.level)}>${log.level}</uui-tag></div>
                                          <div>${log.machineName || "Unknown"}</div>
                                          <div>
                                              <div class="message">${log.message || log.messageTemplate}</div>
                                          </div>
                                          ${this._expandedId === log.id ? this._renderExpanded(log) : ""}
                                      </div>
                                  `
                              )}
                          </div>
                          <div class="pager">
                              <uui-button look="secondary" label="Previous page" ?disabled=${this._currentPage <= 1} @click=${() => this._go(this._currentPage - 1)}>Previous</uui-button>
                              <uui-button look="secondary" label="Next page" ?disabled=${this._currentPage >= totalPages} @click=${() => this._go(this._currentPage + 1)}>Next</uui-button>
                          </div>
                      `}
            </godmode-page>
        `;
    }

    private _renderInsights() {
        if (!this._insights.length) {
            return html`
                <uui-box headline="Most Common Issues" class="insights">
                    <p class="muted">No repeated warnings or errors found for this range.</p>
                </uui-box>
            `;
        }

        return html`
            <uui-box headline="Most Common Issues" class="insights">
                <div class="insight-table">
                    <div class="insight-head">
                        <span>Count</span>
                        <span>Level</span>
                        <span>Pattern</span>
                        <span>Last Seen</span>
                        <span>Analyze</span>
                    </div>
                    ${this._insights.map(
                        (insight) => html`
                            <div class="insight-row">
                                <strong>${insight.count.toLocaleString()}</strong>
                                <uui-tag color=${this._levelColor(insight.level)}>${insight.level}</uui-tag>
                                <div>
                                    <strong>${insight.title}</strong>
                                    <small>${insight.sourceContext || "Unknown source"}${insight.requestPaths.length ? ` · ${insight.requestPaths[0]}` : ""}</small>
                                </div>
                                <time>${this._formatDate(insight.lastSeen)}</time>
                                <div @click=${(e: Event) => e.stopPropagation()}>
                                    <godmode-ai-explain-host .subject=${this._insightSubject(insight)}></godmode-ai-explain-host>
                                </div>
                            </div>
                        `
                    )}
                </div>
            </uui-box>
        `;
    }

    private _renderLevelCounts() {
        if (!this._levelCounts.length) return "";

        return html`
            <div class="level-counts">
                ${this._levelCounts.map(
                    (item) => html`
                        <button type="button" class=${item.level === this._level ? "level-count active" : "level-count"} @click=${() => { this._level = item.level; void this._load(1); }}>
                            <uui-tag color=${this._levelColor(item.level)}>${item.level}</uui-tag>
                            <strong>${item.count.toLocaleString()}</strong>
                        </button>
                    `
                )}
            </div>
        `;
    }

    private _dateInputValue(date: Date): string {
        return date.toISOString().slice(0, 10);
    }

    private _startOfDay(value: string): string | undefined {
        return value ? new Date(`${value}T00:00:00`).toISOString() : undefined;
    }

    private _endOfDay(value: string): string | undefined {
        return value ? new Date(`${value}T23:59:59.999`).toISOString() : undefined;
    }

    private _formatDate(value: string | null): string {
        return value ? new Date(value).toLocaleString() : "Unknown";
    }

    private _formatValue(value: unknown): string {
        return typeof value === "string" ? value : JSON.stringify(value);
    }

    private _formatLabel(value: string): string {
        return value.endsWith(":") ? value : `${value}:`;
    }

    private _extractStackFrames(exception: string): Array<{ member: string; type: string; method: string; file?: string; line?: number; raw: string }> {
        return exception
            .split(/\r?\n/)
            .map((line) => line.trim())
            .filter((line) => line.startsWith("at "))
            .map((line) => {
                const withoutPrefix = line.slice(3);
                const [memberPart, locationPart] = withoutPrefix.split(" in ");
                const member = memberPart.trim();
                const memberWithoutArgs = member.replace(/\(.*/, "");
                const lastDot = memberWithoutArgs.lastIndexOf(".");
                const type = lastDot > 0 ? memberWithoutArgs.slice(0, lastDot) : memberWithoutArgs;
                const method = lastDot > 0 ? memberWithoutArgs.slice(lastDot + 1) : "";
                const location = locationPart?.match(/^(.*):line (\d+)$/);

                return {
                    member,
                    type,
                    method,
                    file: location?.[1],
                    line: location?.[2] ? Number(location[2]) : undefined,
                    raw: line
                };
            });
    }

    private _levelColor(level: string): "default" | "warning" | "danger" | "positive" {
        if (level === "Error" || level === "Fatal") return "danger";
        if (level === "Warning") return "warning";
        if (level === "Information") return "positive";
        return "default";
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: minmax(8rem, 0.6fr) minmax(8rem, 0.6fr) minmax(0, 2fr);
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .summary div {
            display: grid;
            gap: var(--uui-size-space-1);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            padding: var(--uui-size-space-4);
            min-width: 0;
        }
        .summary span,
        small,
        .results {
            color: var(--uui-color-text-alt);
        }
        .summary code {
            overflow-wrap: anywhere;
        }
        .level-counts {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-2);
            margin: 0 0 var(--uui-size-space-4);
        }
        .level-count {
            display: inline-flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-1) var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
            cursor: pointer;
        }
        .level-count:hover,
        .level-count.active {
            border-color: var(--uui-color-selected);
            background: var(--uui-color-surface-alt);
        }
        .filters {
            display: grid;
            grid-template-columns: minmax(220px, 2fr) minmax(130px, 0.6fr) minmax(180px, 1fr) minmax(130px, 0.6fr) minmax(130px, 0.6fr) max-content;
            gap: var(--uui-size-space-3);
            align-items: end;
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input,
        select,
        input {
            width: 100%;
            box-sizing: border-box;
        }
        select,
        input {
            min-height: 32px;
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .results,
        .pager {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: var(--uui-size-space-3);
            margin: var(--uui-size-space-3) 0;
        }
        .muted {
            color: var(--uui-color-text-alt);
            margin: 0;
        }
        .insights {
            margin-top: var(--uui-size-space-4);
            margin-bottom: var(--uui-size-space-4);
        }
        .insight-table {
            display: grid;
            gap: 0;
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            overflow: hidden;
        }
        .insight-head,
        .insight-row {
            display: grid;
            grid-template-columns: 5rem 7rem minmax(0, 1fr) 11rem 9rem;
            align-items: center;
            gap: var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            border-bottom: 1px solid var(--uui-color-border);
        }
        .insight-head {
            font-weight: 700;
            background: var(--uui-color-surface-alt);
        }
        .insight-row:last-child {
            border-bottom: 0;
        }
        .insight-row strong,
        .insight-row small {
            display: block;
            overflow-wrap: anywhere;
        }
        time,
        small {
            display: block;
        }
        .log-table {
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            overflow: hidden;
        }
        .log-head,
        .log-row {
            display: grid;
            grid-template-columns: minmax(160px, 0.8fr) minmax(110px, 0.45fr) minmax(140px, 0.8fr) minmax(260px, 2fr);
            align-items: center;
        }
        .log-head {
            min-height: 4rem;
            padding: 0 var(--uui-size-space-4);
            border-bottom: 1px solid var(--uui-color-border);
            font-weight: 700;
        }
        .log-row {
            cursor: pointer;
            padding: var(--uui-size-space-3) var(--uui-size-space-4);
            border-bottom: 1px solid var(--uui-color-border);
            gap: var(--uui-size-space-3);
        }
        .log-row:last-child {
            border-bottom: 0;
        }
        .log-row:hover,
        .log-row:focus-visible {
            background: var(--uui-color-surface-alt);
            outline: none;
        }
        .log-row.expanded {
            align-items: start;
            background: var(--uui-color-surface);
        }
        .message {
            overflow-wrap: anywhere;
        }
        .details {
            grid-column: 1 / -1;
            margin-top: var(--uui-size-space-3);
            padding-left: var(--uui-size-space-1);
            cursor: default;
        }
        .detail-actions {
            display: flex;
            justify-content: flex-end;
            margin-bottom: var(--uui-size-space-2);
        }
        .exception {
            white-space: pre-wrap;
            overflow-wrap: anywhere;
            margin: 0 0 var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            border-left: 4px solid var(--uui-color-danger);
            background: var(--uui-color-surface-alt);
        }
        .properties {
            display: grid;
            grid-template-columns: max-content minmax(0, 1fr);
            border: 1px solid var(--uui-color-border);
            border-bottom: 0;
            border-radius: var(--uui-border-radius);
            margin: 0;
            overflow: hidden;
        }
        .property-label,
        .property-value {
            min-width: 0;
            padding: var(--uui-size-space-3);
            border-bottom: 1px solid var(--uui-color-border);
            background: var(--uui-color-surface-alt);
            overflow-wrap: anywhere;
        }
        .property-label {
            font-weight: 700;
            white-space: nowrap;
        }
        .property-value {
            background: var(--uui-color-surface);
        }
        @media (max-width: 1000px) {
            .summary,
            .filters {
                grid-template-columns: 1fr;
            }
            .filters uui-button {
                justify-self: start;
            }
            .log-head {
                display: none;
            }
            .log-row {
                grid-template-columns: 1fr;
            }
            .insight-head {
                display: none;
            }
            .insight-row {
                grid-template-columns: 1fr;
            }
            .properties {
                grid-template-columns: 1fr;
            }
            .property-label {
                padding-bottom: var(--uui-size-space-1);
            }
            .property-value {
                padding-top: var(--uui-size-space-1);
            }
        }
    `;
}

export default GodModeLogBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-log-browser": GodModeLogBrowserElement;
    }
}
