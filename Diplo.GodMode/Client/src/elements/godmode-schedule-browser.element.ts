import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet, godmodePost } from "../api/client";
import { UMB_CURRENT_USER_CONTEXT } from "@umbraco-cms/backoffice/current-user";
import "../shared";
import { parseApiDate } from "../shared/date-time";
import { isGodModeAiExplainAvailable, observeGodModeAiExplainAvailability } from "../shared/ai-availability";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { applySort, toggleSort, type SortState } from "../shared/sort";
import { buildSchedulingHealthSubject } from "../shared/scheduling-ai-context";
import type { ContentScheduleItem, ContentScheduleOverview, DistributedJobInfo } from "../shared/types";

type ScheduleFilter = "all" | "Release" | "Expire" | "overdue";

const PUBLISHING_JOB = "ScheduledPublishingJob";

interface ServerCleanupPreview {
    minimumAgeDays: number;
    maximumBatchSize: number;
    cutoffUtc: string;
    totalEligible: number;
    servers: Array<{ id: number; identity: string; address: string; registeredDate: string; lastNotifiedDate: string }>;
}
interface ServerCleanupResult { deletedIds: number[]; skippedIds: number[]; }

@customElement("godmode-schedule-browser")
export class GodModeScheduleBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _overview: ContentScheduleOverview | null = null;
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _filter: ScheduleFilter = "all";
    @state() private _sort: SortState = { column: "date", reverse: false };
    @state() private _aiAvailable = isGodModeAiExplainAvailable();
    private _disposeAiObserver?: () => void;
    @state() private _error = "";
    @state() private _isAdmin = false;
    @state() private _cleanupBusy = false;
    @state() private _cleanupPreview: ServerCleanupPreview | null = null;
    @state() private _cleanupStatus = "";
    @state() private _cleanupError = "";
    @state() private _selectedServerIds: number[] = [];
    private _confirmedIds: number[] = [];

    constructor() {
        super();
        this.consumeContext(UMB_CURRENT_USER_CONTEXT, (context) => {
            if (!context) return;
            this.observe(context.isAdmin, (value) => {
                this._isAdmin = value === true;
                if (!this._isAdmin) this._cleanupPreview = null;
            });
        });
    }

    private async _reviewServerCleanup() {
        this._cleanupBusy = true;
        this._cleanupError = "";
        this._cleanupStatus = "";
        this._cleanupPreview = null;
        this._selectedServerIds = [];
        try { this._cleanupPreview = await godmodeGet<ServerCleanupPreview>("server-registrations/cleanup"); }
        catch { this._cleanupError = "Old server registrations could not be reviewed. No registrations were deleted."; }
        finally { this._cleanupBusy = false; }
    }

    private _confirmServerCleanup() {
        this._confirmedIds = this._selectedServerIds.filter(id => this._cleanupPreview?.servers.some(x => x.id === id));
        if (!this._confirmedIds.length || !this._isAdmin || this._cleanupBusy) return;
        this.renderRoot.querySelector<HTMLDialogElement>("#server-cleanup-confirm")?.showModal();
    }

    private async _serverCleanupClosed(e: Event) {
        const dialog = e.target as HTMLDialogElement;
        const confirmed = dialog.returnValue === "delete";
        dialog.returnValue = "";
        const ids = this._confirmedIds;
        this._confirmedIds = [];
        if (!confirmed || !ids.length || !this._isAdmin || this._cleanupBusy) return;
        this._cleanupBusy = true;
        this._cleanupError = "";
        this._cleanupPreview = null;
        try {
            const result = await godmodePost<ServerCleanupResult>("server-registrations/cleanup", undefined, { ids, confirmed: true });
            this._cleanupStatus = `Deleted ${result.deletedIds.length} old registration(s). ${result.skippedIds.length} skipped because they were missing or no longer eligible.`;
            await this._load();
        } catch {
            this._cleanupError = "Cleanup could not be confirmed. Review the registrations again before retrying.";
        } finally { this._cleanupBusy = false; }
    }

    private _renderServerCleanup() {
        if (!this._isAdmin) return "";
        const preview = this._cleanupPreview;
        return html`
            <section class="server-cleanup" aria-labelledby="server-cleanup-heading">
                <h4 id="server-cleanup-heading">Clean up old registrations</h4>
                <p class="muted">Review inactive, non-publisher registrations with no check-in for over 30 days. The current instance and recently registered servers are protected. A returning instance can register again.</p>
                <uui-button look="secondary" label="Review old server registrations" ?disabled=${this._cleanupBusy}
                    @click=${() => void this._reviewServerCleanup()}>Review old registrations</uui-button>
                ${this._cleanupBusy ? html`<p role="status">Checking server registrations…</p>` : ""}
                ${this._cleanupError ? html`<p role="alert">${this._cleanupError}</p>` : ""}
                ${this._cleanupStatus ? html`<p role="status">${this._cleanupStatus}</p>` : ""}
                ${preview ? html`
                    <p>${preview.totalEligible} eligible registration(s). Showing ${preview.servers.length}; at most ${preview.maximumBatchSize} are removed per cleanup.</p>
                    ${preview.servers.length ? html`
                        <table class="cleanup-table">
                            <caption>Registrations proposed for deletion</caption>
                            <thead><tr><th scope="col">Select</th><th scope="col">Server</th><th scope="col">Address</th><th scope="col">Last check-in</th><th scope="col">Registered</th></tr></thead>
                            <tbody>${preview.servers.map(server => html`<tr>
                                <td><input type="checkbox" aria-label=${`Select server registration ${server.id}`}
                                    .checked=${this._selectedServerIds.includes(server.id)} ?disabled=${this._cleanupBusy}
                                    @change=${(e: Event) => this._selectedServerIds = (e.target as HTMLInputElement).checked
                                        ? [...this._selectedServerIds, server.id] : this._selectedServerIds.filter(id => id !== server.id)}></td>
                                <th scope="row">${server.identity}</th><td>${server.address}</td>
                                <td><godmode-date .value=${server.lastNotifiedDate}></godmode-date></td>
                                <td><godmode-date .value=${server.registeredDate}></godmode-date></td>
                            </tr>`)}</tbody>
                        </table>
                        <p><label><input type="checkbox" ?disabled=${this._cleanupBusy}
                            .checked=${this._selectedServerIds.length === preview.servers.length}
                            @change=${(e: Event) => this._selectedServerIds = (e.target as HTMLInputElement).checked ? preview.servers.map(x => x.id) : []}>
                            Select all reviewed registrations</label></p>
                        <uui-button look="primary" color="danger" label="Delete selected server registrations"
                            ?disabled=${this._cleanupBusy || !this._selectedServerIds.length} @click=${() => this._confirmServerCleanup()}>Delete selected registrations (${this._selectedServerIds.length})</uui-button>
                    ` : html`<p>No old inactive registrations qualify for cleanup.</p>`}
                ` : ""}
                <dialog id="server-cleanup-confirm" aria-labelledby="server-cleanup-title" @close=${(e: Event) => void this._serverCleanupClosed(e)}>
                    <h3 id="server-cleanup-title">Delete ${this._selectedServerIds.length} old server registration(s)?</h3>
                    <p>This permanently removes the selected registrations. Eligibility will be checked again; any server that is now active, a publisher, current or recent will be skipped.</p>
                    <form method="dialog" class="cleanup-dialog-actions">
                        <button value="cancel" autofocus>Cancel</button>
                        <button value="delete" class="cleanup-delete">Delete registrations</button>
                    </form>
                </dialog>
            </section>
        `;
    }

    override connectedCallback(): void {
        super.connectedCallback();
        this._disposeAiObserver = observeGodModeAiExplainAvailability(() => (this._aiAvailable = true));
        void this._load();
    }

    override disconnectedCallback(): void {
        this._disposeAiObserver?.();
        super.disconnectedCallback();
    }

    private async _load() {
        this._loading = true;
        this._error = "";
        try {
            this._overview = await godmodeGet<ContentScheduleOverview>("content/schedules");
        } catch {
            this._error = "Scheduled publishing information could not be refreshed. Any existing results are from the previous load.";
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): ContentScheduleItem[] {
        const q = this._search.trim().toLowerCase();
        const rows = (this._overview?.items ?? []).filter((item) => {
            if (q && !item.name.toLowerCase().includes(q) && !item.contentTypeAlias?.toLowerCase().includes(q)) return false;
            if (this._filter === "overdue") return item.isOverdue;
            if (this._filter !== "all") return item.action === this._filter;
            return true;
        });
        return applySort(rows as unknown as Array<Record<string, unknown>>, this._sort) as unknown as ContentScheduleItem[];
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        return html`
            <godmode-page
                heading="Scheduled Publishing"
                description="Pending scheduled publish and unpublish dates, and whether the background jobs that run them are healthy."
                show-reload
                @reload=${() => void this._load()}
            >
                ${this._error ? html`<p>${this._error}</p>` : ""}
                ${this._loading && !this._overview ? html`<uui-loader></uui-loader>` : this._renderContent()}
            </godmode-page>
        `;
    }

    private _renderContent() {
        const overview = this._overview;
        if (!overview) return html`<p class="muted">Scheduled publishing information could not be loaded.</p>`;

        return html`
            ${this._renderSummary(overview)}
            <uui-box headline="Schedules">
                <div class="toolbar">
                    <uui-input
                        type="search"
                        autocomplete="off"
                        spellcheck="false"
                        placeholder="Filter by name or document type"
                        .value=${this._search}
                        @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                    ></uui-input>
                    <select @change=${(e: Event) => (this._filter = (e.target as HTMLSelectElement).value as ScheduleFilter)}>
                        <option value="all" ?selected=${this._filter === "all"}>All schedules</option>
                        <option value="Release" ?selected=${this._filter === "Release"}>Publish</option>
                        <option value="Expire" ?selected=${this._filter === "Expire"}>Unpublish</option>
                        <option value="overdue" ?selected=${this._filter === "overdue"}>Overdue</option>
                    </select>
                    <godmode-date-mode-toggle></godmode-date-mode-toggle>
                </div>
                ${this._renderSchedules()}
            </uui-box>
            <uui-box headline="Background Jobs">
                <p class="muted">Last-run timestamps show job bookkeeping, including runs that failed or skipped work. A recent timestamp does not establish successful publishing.</p>
                ${overview.jobsCheckSucceeded ? this._renderJobs(overview.jobs) : html`<p>Job registrations could not be checked.</p>`}
            </uui-box>
            <uui-box headline="Servers">${this._renderServers(overview)}${this._renderServerCleanup()}</uui-box>
        `;
    }

    private _renderSummary(overview: ContentScheduleOverview) {
        const items = overview.items;
        const overdue = items.filter((x) => x.isOverdue && !x.trashed).length;
        const now = parseApiDate(overview.serverUtcNow)?.getTime() ?? Date.now();
        const next = items.find((x) => !x.trashed && (parseApiDate(x.date)?.getTime() ?? 0) >= now);
        const job = overview.jobs.find((x) => x.name === PUBLISHING_JOB);

        return html`
            <div class="summary">
                <div class="card">
                    <span class="label">Pending</span>
                    <strong>${items.length.toLocaleString()}</strong>
                    <small>${items.filter((x) => x.action === "Release").length} publish, ${items.filter((x) => x.action === "Expire").length} unpublish</small>
                </div>
                <div class="card ${overdue ? "danger" : "positive"}">
                    <span class="label">Overdue</span>
                    <strong>${overdue.toLocaleString()}</strong>
                    <small>More than ${overview.overdueAfterMinutes} minutes late</small>
                </div>
                <div class="card">
                    <span class="label">Next</span>
                    <strong>${next ? next.name : "Nothing scheduled"}</strong>
                    <small>${next ? html`<godmode-date .value=${next.date} relative></godmode-date>` : ""}</small>
                </div>
                <div class="card ${!overview.jobsCheckSucceeded || overview.scheduledPublishingSuspended || !job ? "warning" : job.isStale || overdue ? "danger" : ""}">
                    <span class="label">Publishing job</span>
                    <strong>${!overview.jobsCheckSucceeded ? "Unknown" : overview.scheduledPublishingSuspended ? "Suspended" : !job ? "Not registered" : job.isStale ? "Last run is old" : job.isRunning ? "Marked running" : "Recently ran"}</strong>
                    <small>${job ? html`Last ran <godmode-date .value=${job.lastRun} relative></godmode-date>` : "No ScheduledPublishingJob row found"}</small>
                </div>
            </div>
            ${this._aiAvailable ? html`<div class="scheduling-ai-actions">
                <godmode-ai-explain-host label="Explain scheduling health"
                    .subjectProvider=${() => buildSchedulingHealthSubject(overview)}></godmode-ai-explain-host>
            </div>` : ""}
        `;
    }

    private _renderSchedules() {
        const all = this._overview?.items ?? [];
        if (!all.length) return html`<p class="muted">No content is scheduled to publish or unpublish.</p>`;

        const rows = this._filtered();

        return html`
            <p class="results"><strong>${rows.length}</strong> / <strong>${all.length}</strong></p>
            <uui-table @sort-change=${this._onSortChange}>
                <uui-table-head>
                    <godmode-sort-header column="name" .sort=${this._sort}>Content</godmode-sort-header>
                    <godmode-sort-header column="action" .sort=${this._sort}>Action</godmode-sort-header>
                    <godmode-sort-header column="culture" .sort=${this._sort}>Culture</godmode-sort-header>
                    <godmode-sort-header column="date" .sort=${this._sort}>Scheduled For</godmode-sort-header>
                    <uui-table-head-cell>Status</uui-table-head-cell>
                    ${this._aiAvailable ? html`<uui-table-head-cell>AI</uui-table-head-cell>` : ""}
                </uui-table-head>
                ${rows.map(
                    (item) => html`
                        <uui-table-row>
                            <uui-table-cell>
                                <a href=${editUrl("content", item.nodeKey)} @click=${(e: Event) => openEditorModal(this, "content", item.nodeKey, e)}
                                    ><strong>${item.name}</strong></a
                                >
                                <small class="block"><code>${item.contentTypeAlias}</code></small>
                            </uui-table-cell>
                            <uui-table-cell>
                                <uui-tag color=${item.action === "Expire" ? "warning" : "positive"}>${item.action === "Expire" ? "Unpublish" : "Publish"}</uui-tag>
                            </uui-table-cell>
                            <uui-table-cell>${item.culture || html`<span class="muted">Invariant</span>`}</uui-table-cell>
                            <uui-table-cell><godmode-date .value=${item.date} relative></godmode-date></uui-table-cell>
                            <uui-table-cell>
                                ${item.isOverdue && !item.trashed ? html`<uui-tag color="danger">Overdue</uui-tag>` : html`<uui-tag look="secondary">${item.trashed ? "Inactive" : "Pending"}</uui-tag>`}
                                ${item.trashed ? html`<uui-tag color="warning">In recycle bin</uui-tag>` : ""}
                                ${item.published ? html`<uui-tag look="secondary">Published</uui-tag>` : ""}
                            </uui-table-cell>
                            ${this._aiAvailable ? html`<uui-table-cell class="action-cell">
                                <godmode-ai-explain-host .subject=${this._explainSubject(item)}></godmode-ai-explain-host>
                            </uui-table-cell>` : ""}
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    private _renderJobs(jobs: DistributedJobInfo[]) {
        if (!jobs.length) return html`<p class="muted">No distributed background jobs were found (umbracoDistributedJob is empty or missing).</p>`;

        return html`
            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>Job</uui-table-head-cell>
                    <uui-table-head-cell>Every</uui-table-head-cell>
                    <uui-table-head-cell>Last Run</uui-table-head-cell>
                    <uui-table-head-cell>State</uui-table-head-cell>
                </uui-table-head>
                ${jobs.map(
                    (job) => html`
                        <uui-table-row>
                            <uui-table-cell><strong>${job.name}</strong></uui-table-cell>
                            <uui-table-cell>${this._formatPeriod(job.periodSeconds)}</uui-table-cell>
                            <uui-table-cell>
                                <godmode-date .value=${job.lastRun} relative></godmode-date>
                                ${job.lastAttemptedRun !== job.lastRun
                                    ? html`<small class="block">Attempted <godmode-date .value=${job.lastAttemptedRun} relative></godmode-date></small>`
                                    : ""}
                            </uui-table-cell>
                            <uui-table-cell>
                                ${job.isStale ? html`<uui-tag color="warning">Last run is old</uui-tag>` : html`<uui-tag look="secondary">Recently ran</uui-tag>`}
                                ${job.isRunning ? html`<uui-tag look="secondary">Marked running</uui-tag>` : ""}
                            </uui-table-cell>
                        </uui-table-row>
                    `
                )}
            </uui-table>
        `;
    }

    private _renderServers(overview: ContentScheduleOverview) {
        if (!overview.serversCheckSucceeded) return html`<p>Server registrations could not be checked.</p>`;
        return html`
            <p class="muted">This server's role: <strong>${overview.currentServerRole}</strong></p>
            ${!overview.automaticServerRegistration ? html`<p class="muted">Automatic election/check-ins are disabled for this accessor. Existing rows may be historical; their age does not establish that a server is down, and automatic cleanup is not assured.</p>` : ""}
            ${overview.servers.length
                ? html`
                      <uui-table>
                          <uui-table-head>
                              <uui-table-head-cell>Server</uui-table-head-cell>
                              <uui-table-head-cell>Last Check-in</uui-table-head-cell>
                              <uui-table-head-cell>State</uui-table-head-cell>
                          </uui-table-head>
                          ${overview.servers.map(
                              (server) => html`
                                  <uui-table-row>
                                      <uui-table-cell>
                                          <strong>${server.computerName}</strong>
                                          <small class="block">${server.address}</small>
                                      </uui-table-cell>
                                      <uui-table-cell>
                                          <godmode-date .value=${server.lastNotifiedDate} relative></godmode-date>
                                          <small class="block">Registered <godmode-date .value=${server.registeredDate}></godmode-date></small>
                                      </uui-table-cell>
                                      <uui-table-cell>
                                          ${server.isActive ? html`<uui-tag color=${overview.automaticServerRegistration ? "positive" : "default"}>${overview.automaticServerRegistration ? "Active" : "Recorded active"}</uui-tag>` : html`<uui-tag look="secondary">Recorded inactive</uui-tag>`}
                                          ${server.isSchedulingPublisher ? html`<uui-tag look="secondary">Recorded publisher</uui-tag>` : ""}
                                          ${server.isStale ? html`<uui-tag color=${overview.automaticServerRegistration ? "warning" : "default"}>${overview.automaticServerRegistration ? "Stale" : "Historical check-in"}</uui-tag>` : ""}
                                      </uui-table-cell>
                                  </uui-table-row>
                              `
                          )}
                      </uui-table>
                  `
                : html`<p class="muted">No servers are registered in umbracoServer. This is normal for a single server set up with server election disabled.</p>`}
        `;
    }

    private _formatPeriod(seconds: number): string {
        if (seconds >= 86400 && seconds % 86400 === 0) return `${seconds / 86400}d`;
        if (seconds >= 3600 && seconds % 3600 === 0) return `${seconds / 3600}h`;
        if (seconds >= 60 && seconds % 60 === 0) return `${seconds / 60}m`;
        return `${seconds}s`;
    }

    private _explainSubject(item: ContentScheduleItem) {
        return {
            subjectType: "Umbraco scheduled publishing entry",
            title: item.name,
            data: {
                name: item.name,
                contentTypeAlias: item.contentTypeAlias,
                action: item.action === "Expire" ? "Unpublish" : "Publish",
                culture: item.culture || "invariant",
                scheduledForUtc: item.date,
                serverUtcNow: this._overview?.serverUtcNow,
                isOverdue: item.isOverdue,
                trashed: item.trashed,
                currentlyPublished: item.published
            },
            context: {
                publishingJob: this._overview?.jobs.find((x) => x.name === PUBLISHING_JOB),
                currentServerRole: this._overview?.currentServerRole,
                note: "Umbraco 17 stores schedule dates in UTC and runs ScheduledPublishingJob every minute as a distributed job."
            }
        };
    }

    static override styles = css`
        .scheduling-ai-actions { display: flex; justify-content: flex-end; margin-bottom: var(--uui-size-space-4); }
        .server-cleanup { margin-top: var(--uui-size-space-5); }
        .cleanup-table { width: 100%; border-collapse: collapse; margin-block: var(--uui-size-space-4); }
        .cleanup-table th, .cleanup-table td { text-align: start; padding: var(--uui-size-space-3); border-bottom: 1px solid var(--uui-color-border); overflow-wrap: anywhere; }
        dialog { color: var(--uui-color-text); background: var(--uui-color-surface); border: 1px solid var(--uui-color-border); border-radius: var(--uui-border-radius); max-inline-size: min(38rem, 85vw); }
        dialog:modal { overscroll-behavior: contain; }
        dialog::backdrop { background: rgb(0 0 0 / 40%); overflow: hidden; overscroll-behavior: contain; }
        .cleanup-dialog-actions { display: flex; justify-content: flex-end; gap: var(--uui-size-space-3); }
        .cleanup-dialog-actions button { font: inherit; padding: var(--uui-size-space-3); cursor: pointer; }
        .cleanup-delete { color: var(--uui-color-danger-contrast); background: var(--uui-color-danger); border: 1px solid var(--uui-color-danger); }
        uui-box {
            margin-bottom: var(--uui-size-space-4);
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .card {
            display: grid;
            gap: var(--uui-size-space-1);
            padding: var(--uui-size-space-4);
            border: 1px solid var(--uui-color-border);
            border-left: 0.35rem solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            min-width: 0;
        }
        .card.positive {
            border-left-color: var(--uui-color-positive);
        }
        .card.warning {
            border-left-color: var(--uui-color-warning);
        }
        .card.danger {
            border-left-color: var(--uui-color-danger);
        }
        .card strong {
            font-size: 1.25rem;
            overflow-wrap: anywhere;
        }
        .label,
        .card small,
        .muted,
        .results {
            color: var(--uui-color-text-alt);
        }
        .toolbar {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: var(--uui-size-space-3);
        }
        .toolbar uui-input {
            flex: 1 1 18rem;
        }
        .toolbar select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .results {
            margin: var(--uui-size-space-3) 0;
        }
        .block {
            display: block;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        .action-cell {
            display: flex;
            justify-content: flex-end;
        }
    `;
}

export default GodModeScheduleBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-schedule-browser": GodModeScheduleBrowserElement;
    }
}
