import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UmbNotificationContext, UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import type { UmbNotificationColor } from "@umbraco-cms/backoffice/notification";
import { godmodeGet, godmodePost } from "../api/client";
import "../shared";
import type { Lang, ServerResponse, UtilityDiagnostics } from "../shared/types";

interface WarmupResult {
    url: string;
    status: number | null;
    duration: number;
    ok: boolean;
}

function responseKind(r: ServerResponse): { color: UmbNotificationColor; label: string } {
    // C# enum order is Success=0, Error=1, Warning=2 (see ServerResponseType.cs)
    if (r.response === "Success" || r.responseType === 0) return { color: "positive", label: "Success" };
    if (r.response === "Warning" || r.responseType === 2) return { color: "warning", label: "Warning" };
    return { color: "danger", label: "Error" };
}

@customElement("godmode-utility-browser")
export class GodModeUtilityBrowserElement extends UmbElementMixin(LitElement) {
    private _notificationContext?: UmbNotificationContext;

    @state() private _languages: Lang[] = [];
    @state() private _selectedCulture: string | null = null;
    @state() private _busy = false;
    @state() private _warmupCurrent = 0;
    @state() private _warmupTotal = 0;
    @state() private _warmupCurrentUrl = "";
    @state() private _diagnostics: UtilityDiagnostics | null = null;
    @state() private _warmupResults: WarmupResult[] = [];
    @state() private _lastResponse: { color: UmbNotificationColor; headline: string; message: string } | null = null;

    constructor() {
        super();

        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this._notificationContext = context;
        });
    }

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadLanguages();
        void this._loadDiagnostics();
    }

    private async _loadLanguages() {
        try {
            this._languages = await godmodeGet<Lang[]>("languages");
        } catch {
            // non-fatal
        }
    }

    private async _loadDiagnostics() {
        try {
            this._diagnostics = await godmodeGet<UtilityDiagnostics>("utilities/diagnostics");
        } catch (e) {
            console.error(e);
        }
    }

    private async _notifyResponse(promise: Promise<ServerResponse>) {
        this._busy = true;
        try {
            const r = await promise;
            const kind = responseKind(r);
            this._lastResponse = { color: kind.color, headline: kind.label, message: r.message };
            this._notificationContext?.peek(kind.color, { data: { headline: kind.label, message: r.message } });
            console.log(`[${kind.label}] ${r.message}`);
        } catch (e) {
            const message = e instanceof Error ? e.message : "The action failed.";
            this._lastResponse = { color: "danger", headline: "Error", message };
            this._notificationContext?.peek("danger", { data: { headline: "Error", message } });
            console.error(e);
        } finally {
            this._busy = false;
        }
    }

    private _clearCache(cache: string) {
        return this._notifyResponse(godmodePost<ServerResponse>("cache/clear", { cache }));
    }

    private _purgeMediaCache() {
        if (!confirm("This will delete cached image crops on disk. Continue?")) return;
        return this._notifyResponse(godmodePost<ServerResponse>("cache/purge-media"));
    }

    private _restartAppPool() {
        if (!confirm("This will take the site offline (and won't restart it). Are you sure?")) return;
        return this._notifyResponse(godmodePost<ServerResponse>("app/restart"));
    }

    private async _warmupTemplates() {
        try {
            this._busy = true;
            const urls = await godmodeGet<string[]>("templates/urls-to-ping");
            await this._pingAll(urls);
        } finally {
            this._busy = false;
        }
    }

    private async _warmupAll() {
        try {
            this._busy = true;
            const urls = await godmodeGet<string[]>("urls-to-ping", {
                culture: this._selectedCulture ?? ""
            });
            await this._pingAll(urls);
        } finally {
            this._busy = false;
        }
    }

    private async _pingAll(urls: string[]) {
        if (!urls.length) {
            console.warn("URL list was empty");
            return;
        }
        this._warmupTotal = urls.length;
        this._warmupCurrent = 0;
        this._warmupCurrentUrl = "[waiting…]";
        this._warmupResults = [];
        for (const url of urls) {
            this._warmupCurrentUrl = url;
            const started = performance.now();
            let result: WarmupResult;
            try {
                const response = await fetch(url, { cache: "no-store" });
                result = {
                    url,
                    status: response.status,
                    duration: Math.round(performance.now() - started),
                    ok: response.ok
                };
            } catch (e) {
                result = {
                    url,
                    status: null,
                    duration: Math.round(performance.now() - started),
                    ok: false
                };
            }
            this._warmupResults = [...this._warmupResults, result];
            this._warmupCurrent++;
        }
        this._warmupCurrentUrl = "Done.";
    }

    private _formatBytes(bytes: number): string {
        if (!bytes) return "0 B";
        const units = ["B", "KB", "MB", "GB"];
        let value = bytes;
        let unit = 0;
        while (value >= 1024 && unit < units.length - 1) {
            value /= 1024;
            unit++;
        }
        return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
    }

    private _explainDiagnosticsSubject() {
        const diagnostics = this._diagnostics;
        const warmupFailures = this._warmupResults.filter((result) => !result.ok);

        return {
            subjectType: "God Mode utilities diagnostics",
            title: "Utilities environment",
            data: {
                environmentName: diagnostics?.app.environmentName,
                uptime: diagnostics?.app.uptime,
                processId: diagnostics?.app.processId,
                godModeVersion: diagnostics?.app.godModeVersion,
                assetCount: diagnostics?.assets.length ?? 0,
                missingAssets: diagnostics?.assets.filter((asset) => !asset.exists).map((asset) => asset.label) ?? [],
                folderCount: diagnostics?.folders.length ?? 0,
                missingFolders: diagnostics?.folders.filter((folder) => !folder.exists).map((folder) => folder.label) ?? [],
                cacheFolderCount: diagnostics?.cache.folders.length ?? 0,
                missingCacheFolders: diagnostics?.cache.folders.filter((folder) => !folder.exists).map((folder) => folder.label) ?? [],
                warmupUrlsChecked: this._warmupResults.length,
                warmupFailures: warmupFailures.length
            },
            context: {
                diagnostics,
                warmup: {
                    selectedCulture: this._selectedCulture,
                    total: this._warmupResults.length,
                    failures: warmupFailures,
                    slowest: [...this._warmupResults].sort((a, b) => b.duration - a.duration).slice(0, 10)
                },
                guidance: [
                    "Explain the current Umbraco environment and operational signals shown in the Utilities browser.",
                    "Call out missing package assets or folders, unusual cache folder state, and failed or slow warm-up URLs.",
                    "Explain risks and practical next steps before suggesting destructive actions such as restart or media cache purge."
                ]
            }
        };
    }

    override render() {
        return html`
            <godmode-page heading="Utilities" description="Clear caches, restart the application, warm up templates.">
                ${this._renderDiagnostics()}

                <uui-box headline="Caches">
                    <div class="row">
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._clearCache("all")}>Clear all caches</uui-button>
                        <uui-button look="secondary" ?disabled=${this._busy} @click=${() => void this._clearCache("Runtime")}>Clear runtime cache</uui-button>
                        <uui-button look="secondary" ?disabled=${this._busy} @click=${() => void this._clearCache("Isolated")}>Clear isolated caches</uui-button>
                        <uui-button look="secondary" color="danger" ?disabled=${this._busy} @click=${() => void this._purgeMediaCache()}>Purge media folder</uui-button>
                    </div>
                    ${this._renderLastResponse()}
                </uui-box>

                <uui-box headline="Application" style="margin-top: var(--uui-size-space-3)">
                    <p>Stops the app — your hosting platform should restart it automatically. Don't use on production unless you know what you're doing.</p>
                    <uui-button look="primary" color="danger" ?disabled=${this._busy} @click=${() => void this._restartAppPool()}>Stop application</uui-button>
                </uui-box>

                <uui-box headline="Warm-up" style="margin-top: var(--uui-size-space-3)">
                    <p>Compile every Razor template by visiting one URL per template, or visit every URL on the site for a chosen culture.</p>
                    <div class="row">
                        <select @change=${(e: Event) => (this._selectedCulture = (e.target as HTMLSelectElement).value || null)}>
                            <option value="">Default culture</option>
                            ${this._languages.map((l) => html`<option value=${l.culture}>${l.name}</option>`)}
                        </select>
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._warmupTemplates()}>Warm up templates</uui-button>
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._warmupAll()}>Ping every URL</uui-button>
                    </div>
                    ${this._warmupTotal
                        ? html`<p class="progress">${this._warmupCurrent} / ${this._warmupTotal} — <code>${this._warmupCurrentUrl}</code></p>`
                        : ""}
                    ${this._warmupResults.length ? this._renderWarmupResults() : ""}
                </uui-box>
            </godmode-page>
        `;
    }

    private _renderLastResponse() {
        if (!this._lastResponse) return "";

        return html`
            <div class="action-feedback ${this._lastResponse.color}" role="status">
                <strong>${this._lastResponse.headline}</strong>
                <span>${this._lastResponse.message}</span>
            </div>
        `;
    }

    private _renderCacheDiagnostics(d: UtilityDiagnostics) {
        const configuredSettings = d.cache.settings.filter((setting) => setting.value !== "Default");

        return html`
            <h4>Cache</h4>
            ${configuredSettings.length
                ? html`
                      <div class="cache-summary">
                          <uui-tag color="warning">${configuredSettings.length}</uui-tag>
                          <span>custom cache ${configuredSettings.length === 1 ? "setting" : "settings"}</span>
                      </div>
                      <ul class="plain offset">
                          ${configuredSettings.map(
                              (setting) => html`
                                  <li title=${setting.path}>
                                      <uui-tag color="warning">${setting.value}</uui-tag>
                                      <span>${setting.label}</span>
                                  </li>
                              `
                          )}
                      </ul>
                  `
                : html`
                      <div class="cache-summary">
                          <uui-tag color="positive">Default</uui-tag>
                          <span>All cache settings are using Umbraco defaults</span>
                      </div>
                  `}
            <ul class="plain offset">
                ${d.cache.folders.map(
                    (folder) => html`
                        <li title=${folder.path}>
                            <uui-tag color=${folder.exists ? "default" : "warning"}>${folder.exists ? this._formatBytes(folder.size) : "Missing"}</uui-tag>
                            <span>${folder.label}</span>
                            ${folder.exists ? html`<small>${folder.fileCount} files</small>` : ""}
                        </li>
                    `
                )}
            </ul>
        `;
    }

    private _renderDiagnostics() {
        const d = this._diagnostics;
        if (!d) {
            return html`<uui-box headline="System" class="section"><uui-loader></uui-loader></uui-box>`;
        }

        return html`
            <uui-box headline="System" class="section">
                <div class="box-actions">
                    <godmode-ai-explain-host .subjectProvider=${() => this._explainDiagnosticsSubject()}></godmode-ai-explain-host>
                </div>
                <div class="grid">
                    <div>
                        <h4>App</h4>
                        <dl>
                            <dt>Environment</dt>
                            <dd>${d.app.environmentName}</dd>
                            <dt>Uptime</dt>
                            <dd>${d.app.uptime}</dd>
                            <dt>Process</dt>
                            <dd>${d.app.processId}</dd>
                            <dt>GodMode</dt>
                            <dd>${d.app.godModeVersion}</dd>
                        </dl>
                    </div>
                    <div>
                        <h4>Package Assets</h4>
                        <ul class="plain">
                            ${d.assets.map(
                                (asset) => html`
                                    <li>
                                        <uui-tag color=${asset.exists ? "positive" : "danger"}>${asset.exists ? "Found" : "Missing"}</uui-tag>
                                        <a href=${asset.url} target="_blank" rel="noopener">${asset.label}</a>
                                        <span>${this._formatBytes(asset.size)}</span>
                                    </li>
                                `
                            )}
                        </ul>
                    </div>
                    <div>
                        <h4>Folder Sizes</h4>
                        <ul class="plain">
                            ${d.folders.map(
                                (folder) => html`
                                    <li title=${folder.path}>
                                        <uui-tag color=${folder.exists ? "default" : "warning"}>${folder.exists ? this._formatBytes(folder.size) : "Missing"}</uui-tag>
                                        <span>${folder.label}</span>
                                        ${folder.exists ? html`<small>${folder.fileCount} files</small>` : ""}
                                    </li>
                                `
                            )}
                        </ul>
                    </div>
                    <div>
                        ${this._renderCacheDiagnostics(d)}
                    </div>
                    <div>
                        <h4>Database</h4>
                        <ul class="plain">
                            ${d.database.map(
                                (row) => html`
                                    <li title=${row.table}>
                                        <uui-tag color=${row.exists ? "default" : "warning"}>${row.exists ? row.count.toLocaleString() : "Missing"}</uui-tag>
                                        <span>${row.label}</span>
                                        <small>${row.table}</small>
                                    </li>
                                `
                            )}
                        </ul>
                    </div>
                </div>
            </uui-box>
        `;
    }

    private _renderWarmupResults() {
        const failures = this._warmupResults.filter((result) => !result.ok).length;
        const average = Math.round(this._warmupResults.reduce((sum, result) => sum + result.duration, 0) / this._warmupResults.length);

        return html`
            <div class="warmup-results">
                <p><strong>${this._warmupResults.length}</strong> URLs pinged, <strong>${failures}</strong> failed, <strong>${average}ms</strong> average.</p>
                <uui-table>
                    ${this._warmupResults.slice(-20).map(
                        (result) => html`
                            <uui-table-row>
                                <uui-table-cell>
                                    <uui-tag color=${result.ok ? "positive" : "danger"}>${result.status ?? "Error"}</uui-tag>
                                </uui-table-cell>
                                <uui-table-cell><code>${result.duration}ms</code></uui-table-cell>
                                <uui-table-cell><a href=${result.url} target="_blank" rel="noopener">${result.url}</a></uui-table-cell>
                            </uui-table-row>
                        `
                    )}
                </uui-table>
            </div>
        `;
    }

    static override styles = css`
        .row {
            display: flex;
            gap: var(--uui-size-space-3);
            flex-wrap: wrap;
            align-items: center;
        }
        .action-feedback {
            display: flex;
            gap: var(--uui-size-space-2);
            align-items: center;
            margin-top: var(--uui-size-space-3);
            padding: var(--uui-size-space-2) var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
        }
        .action-feedback.positive {
            border-color: var(--uui-color-positive);
            color: var(--uui-color-positive);
        }
        .action-feedback.warning {
            border-color: var(--uui-color-warning);
            color: var(--uui-color-warning);
        }
        .action-feedback.danger {
            border-color: var(--uui-color-danger);
            color: var(--uui-color-danger);
        }
        .section {
            margin-bottom: var(--uui-size-space-3);
        }
        .box-actions {
            display: flex;
            justify-content: flex-end;
            margin-bottom: var(--uui-size-space-3);
        }
        .grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-5);
        }
        h4 {
            margin: 0 0 var(--uui-size-space-2);
        }
        dl {
            display: grid;
            grid-template-columns: max-content minmax(0, 1fr);
            gap: var(--uui-size-space-1) var(--uui-size-space-3);
            margin: 0;
        }
        dt {
            color: var(--uui-color-text-alt);
        }
        dd {
            margin: 0;
            min-width: 0;
            overflow-wrap: anywhere;
        }
        .plain {
            display: grid;
            gap: var(--uui-size-space-2);
            list-style: none;
            padding: 0;
            margin: 0;
        }
        .offset {
            margin-top: var(--uui-size-space-2);
        }
        .cache-summary {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            min-width: 0;
        }
        .cache-summary span {
            min-width: 0;
            overflow-wrap: anywhere;
        }
        .plain li {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            min-width: 0;
        }
        .plain span,
        .plain a {
            min-width: 0;
            overflow-wrap: anywhere;
        }
        .plain small {
            color: var(--uui-color-text-alt);
            white-space: nowrap;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .progress {
            margin-top: var(--uui-size-space-3);
            color: var(--uui-color-text-alt);
        }
        .warmup-results {
            margin-top: var(--uui-size-space-4);
        }
        .warmup-results p {
            color: var(--uui-color-text-alt);
        }
        .warmup-results uui-table-cell:first-child {
            width: 5rem;
        }
        .warmup-results uui-table-cell:nth-child(2) {
            width: 5rem;
        }
        @media (max-width: 1100px) {
            .grid {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeUtilityBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-utility-browser": GodModeUtilityBrowserElement;
    }
}
