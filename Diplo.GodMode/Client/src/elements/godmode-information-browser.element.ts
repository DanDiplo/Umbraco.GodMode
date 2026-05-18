import { LitElement, css, customElement, html, state, svg } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { DeliveryApiDiagnostics, UtilityDiagnostics } from "../shared/types";

@customElement("godmode-information-browser")
export class GodModeInformationBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _diagnostics: UtilityDiagnostics | null = null;
    @state() private _deliveryApiDiagnostics: DeliveryApiDiagnostics | null = null;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        try {
            this._diagnostics = await godmodeGet<UtilityDiagnostics>("utilities/diagnostics");
            this._deliveryApiDiagnostics = await godmodeGet<DeliveryApiDiagnostics>("delivery-api/diagnostics");
        } catch (e) {
            console.error(e);
        }
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

    private _explainSubject() {
        const diagnostics = this._diagnostics;

        return {
            subjectType: "God Mode information",
            title: "System and Content Delivery API",
            data: {
                environmentName: diagnostics?.app.environmentName,
                umbracoVersion: diagnostics?.app.umbracoVersion,
                dotNetVersion: diagnostics?.app.dotNetVersion,
                operatingSystem: diagnostics?.app.operatingSystem,
                webServer: diagnostics?.app.webServer,
                applicationMainUrl: diagnostics?.app.applicationMainUrl,
                debugMode: diagnostics?.app.debugMode,
                uptime: diagnostics?.app.uptime,
                processId: diagnostics?.app.processId,
                godModeVersion: diagnostics?.app.godModeVersion,
                assetCount: diagnostics?.assets.length ?? 0,
                missingAssets: diagnostics?.assets.filter((asset) => !asset.exists).map((asset) => asset.label) ?? [],
                folderCount: diagnostics?.folders.length ?? 0,
                missingFolders: diagnostics?.folders.filter((folder) => !folder.exists).map((folder) => folder.label) ?? [],
                cacheFolderCount: diagnostics?.cache.folders.length ?? 0,
                missingCacheFolders: diagnostics?.cache.folders.filter((folder) => !folder.exists).map((folder) => folder.label) ?? [],
                serverStats: diagnostics?.serverStats,
                deliveryApi: this._deliveryApiDiagnostics
            },
            context: {
                diagnostics,
                guidance: [
                    "Explain the current Umbraco environment and operational signals shown in the Information browser.",
                    "Call out missing package assets or folders and unusual cache folder state.",
                    "Explain Delivery API exposure, sensitive-looking aliases, and practical next steps."
                ]
            }
        };
    }

    override render() {
        return html`
            <godmode-page heading="Information" description="Key system, package and Content Delivery API information." show-reload @reload=${() => void this._load()}>
                ${this._renderSystem()}
                ${this._renderDeliveryApi()}
            </godmode-page>
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

    private _renderSystem() {
        const d = this._diagnostics;
        if (!d) {
            return html`<uui-box headline="System" class="section"><uui-loader></uui-loader></uui-box>`;
        }

        return html`
            <uui-box headline="System" class="section">
                <div class="box-actions">
                    <godmode-ai-explain-host .subjectProvider=${() => this._explainSubject()}></godmode-ai-explain-host>
                </div>
                ${this._renderServerStats(d)}
                <div class="grid">
                    <div>
                        <h4>App</h4>
                        <dl>
                            <dt>Umbraco</dt>
                            <dd>${d.app.umbracoSemanticVersion || d.app.umbracoVersion}</dd>
                            <dt>.NET</dt>
                            <dd>${d.app.dotNetVersion}</dd>
                            <dt>Environment</dt>
                            <dd>
                                ${d.app.environmentName}
                                ${d.app.debugMode ? html`<uui-tag color="warning">Debug</uui-tag>` : ""}
                            </dd>
                            <dt>Uptime</dt>
                            <dd>${d.app.uptime}</dd>
                            <dt>Process</dt>
                            <dd>${d.app.processId}</dd>
                            <dt>GodMode</dt>
                            <dd>${d.app.godModeVersion}</dd>
                        </dl>
                    </div>
                    <div>
                        <h4>Server</h4>
                        <dl>
                            <dt>OS</dt>
                            <dd>${d.app.operatingSystem}</dd>
                            <dt>Runtime</dt>
                            <dd>${d.app.runtimeIdentifier}</dd>
                            <dt>Architecture</dt>
                            <dd>${d.app.processArchitecture}</dd>
                            <dt>Web server</dt>
                            <dd>${d.app.webServer}</dd>
                            <dt>Main URL</dt>
                            <dd>${d.app.applicationMainUrl || "Not configured"}</dd>
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
                    <div>${this._renderCacheDiagnostics(d)}</div>
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

    private _renderDeliveryApi() {
        const d = this._deliveryApiDiagnostics;
        if (!d) {
            return html`<uui-box headline="Content Delivery API" class="section"><uui-loader></uui-loader></uui-box>`;
        }

        const exposed = d.contentTypes.filter((item) => item.isExposed);
        const sensitive = exposed.filter((item) => item.sensitiveAlias);

        return html`
            <uui-box headline="Content Delivery API" class="section">
                <div class="delivery-summary">
                    <uui-tag color=${d.enabled ? "positive" : "default"}>${d.enabled ? "Enabled" : "Disabled"}</uui-tag>
                    <uui-tag color=${d.publicAccess ? "warning" : "positive"}>${d.publicAccess ? "Public" : "API key required"}</uui-tag>
                    <uui-tag color=${d.apiKeyConfigured ? "positive" : "default"}>${d.apiKeyConfigured ? "API key configured" : "No API key"}</uui-tag>
                    <uui-tag color=${sensitive.length ? "danger" : "default"}>${sensitive.length} sensitive exposed</uui-tag>
                </div>
                <div class="grid delivery-grid">
                    <div>
                        <h4>Exposure</h4>
                        <dl>
                            <dt>Document types</dt>
                            <dd>${d.contentTypes.length}</dd>
                            <dt>Exposed</dt>
                            <dd>${exposed.length}</dd>
                            <dt>Disallowed</dt>
                            <dd>${d.disallowedContentTypeAliases.length}</dd>
                            <dt>Cultures</dt>
                            <dd>${d.availableCultures.join(", ") || "Invariant only"}</dd>
                        </dl>
                    </div>
                    <div>
                        <h4>Disallowed Aliases</h4>
                        ${d.disallowedContentTypeAliases.length
                            ? html`<ul class="plain">${d.disallowedContentTypeAliases.map((alias) => html`<li><code>${alias}</code></li>`)}</ul>`
                            : html`<p class="muted">None configured.</p>`}
                    </div>
                    <div>
                        <h4>Sample Endpoints</h4>
                        <ul class="plain endpoints">
                            ${d.sampleEndpoints.map((endpoint) => html`<li><code>${endpoint}</code></li>`)}
                        </ul>
                    </div>
                </div>
                ${d.findings.length
                    ? html`
                          <h4>Findings</h4>
                          <uui-table>
                              ${d.findings.map(
                                  (finding) => html`<uui-table-row>
                                      <uui-table-cell><uui-tag color=${finding.severity === "High" ? "danger" : finding.severity === "Medium" ? "warning" : "default"}>${finding.severity}</uui-tag></uui-table-cell>
                                      <uui-table-cell><strong>${finding.title}</strong><div class="muted">${finding.detail}</div></uui-table-cell>
                                  </uui-table-row>`
                              )}
                          </uui-table>
                      `
                    : ""}
            </uui-box>
        `;
    }

    private _renderServerStats(d: UtilityDiagnostics) {
        const memory = d.serverStats.memory;
        const totalMemory = memory.totalAvailableMemoryBytes;
        const memoryUsedPercent = totalMemory > 0 ? (memory.workingSetBytes / totalMemory) * 100 : 0;

        return html`
            <div class="stat-grid">
                ${this._renderGauge("Memory", memoryUsedPercent, this._formatBytes(memory.workingSetBytes), totalMemory > 0 ? `of ${this._formatBytes(totalMemory)} available` : "Process working set")}
                ${this._renderMetric("Private", this._formatBytes(memory.privateMemoryBytes), "Process memory")}
                ${this._renderMetric("Managed", this._formatBytes(memory.managedHeapBytes), "Managed heap")}
                ${this._renderMetric("CPU", String(d.serverStats.processorCount), "Logical processors")}
                ${this._renderMetric("Threads", d.serverStats.threadCount.toLocaleString(), "Process threads")}
                ${this._renderMetric("Handles", d.serverStats.handleCount ? d.serverStats.handleCount.toLocaleString() : "Unavailable", "Process handles")}
                ${d.serverStats.disks.map((disk) =>
                    this._renderGauge(
                        `Disk ${disk.name}`,
                        disk.usedPercentage,
                        `${disk.usedPercentage.toFixed(1)}% used`,
                        `${this._formatBytes(disk.freeBytes)} free of ${this._formatBytes(disk.totalBytes)}${disk.format ? ` (${disk.format})` : ""}`
                    )
                )}
            </div>
        `;
    }

    private _renderMetric(label: string, value: string, detail: string) {
        return html`
            <div class="metric-card">
                <span class="metric-label">${label}</span>
                <strong>${value}</strong>
                <small>${detail}</small>
            </div>
        `;
    }

    private _renderGauge(label: string, percentage: number, value: string, detail: string) {
        const safePercentage = Math.max(0, Math.min(100, Number.isFinite(percentage) ? percentage : 0));
        const colorClass = safePercentage >= 90 ? "danger" : safePercentage >= 75 ? "warning" : "ok";
        const radius = 36;
        const circumference = 2 * Math.PI * radius;
        const offset = circumference - (safePercentage / 100) * circumference;

        return html`
            <div class="gauge-card ${colorClass}">
                ${svg`
                    <svg viewBox="0 0 96 96" role="img" aria-label=${`${label}: ${value}`}>
                        <circle class="gauge-track" cx="48" cy="48" r=${radius}></circle>
                        <circle class="gauge-value" cx="48" cy="48" r=${radius} stroke-dasharray=${circumference} stroke-dashoffset=${offset}></circle>
                        <text class="gauge-text" x="48" y="53">${Math.round(safePercentage)}%</text>
                    </svg>
                `}
                <div>
                    <span class="metric-label">${label}</span>
                    <strong>${value}</strong>
                    <small>${detail}</small>
                </div>
            </div>
        `;
    }

    static override styles = css`
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
        .stat-grid {
            display: grid;
            grid-template-columns: repeat(4, minmax(0, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-5);
        }
        .gauge-card,
        .metric-card {
            min-width: 0;
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
        }
        .gauge-card {
            display: grid;
            grid-template-columns: 5.25rem minmax(0, 1fr);
            gap: var(--uui-size-space-3);
            align-items: center;
        }
        .metric-card {
            display: grid;
            align-content: center;
            gap: var(--uui-size-space-1);
            min-height: 6.75rem;
        }
        .gauge-card svg {
            width: 5.25rem;
            height: 5.25rem;
            overflow: visible;
        }
        .gauge-track,
        .gauge-value {
            fill: none;
            stroke-width: 9;
        }
        .gauge-track {
            stroke: var(--uui-color-border);
        }
        .gauge-value {
            stroke: var(--uui-color-positive);
            stroke-linecap: round;
            transform: rotate(-90deg);
            transform-origin: 48px 48px;
        }
        .gauge-card.warning .gauge-value {
            stroke: var(--uui-color-warning);
        }
        .gauge-card.danger .gauge-value {
            stroke: var(--uui-color-danger);
        }
        .gauge-text {
            fill: var(--uui-color-text);
            font-size: 1rem;
            font-weight: 700;
            text-anchor: middle;
        }
        .metric-label {
            display: block;
            color: var(--uui-color-text-alt);
            font-size: 0.8125rem;
            font-weight: 700;
            text-transform: uppercase;
        }
        .gauge-card strong,
        .metric-card strong {
            display: block;
            min-width: 0;
            overflow-wrap: anywhere;
            font-size: 1.125rem;
        }
        .gauge-card small,
        .metric-card small {
            display: block;
            min-width: 0;
            color: var(--uui-color-text-alt);
            overflow-wrap: anywhere;
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
        dt,
        .muted,
        .plain small {
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
        .cache-summary,
        .delivery-summary,
        .plain li {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            min-width: 0;
        }
        .delivery-summary {
            flex-wrap: wrap;
            margin-bottom: var(--uui-size-space-4);
        }
        .delivery-grid {
            grid-template-columns: repeat(3, minmax(0, 1fr));
        }
        .endpoints code,
        code {
            white-space: normal;
            overflow-wrap: anywhere;
        }
        .plain span,
        .plain a,
        .cache-summary span {
            min-width: 0;
            overflow-wrap: anywhere;
        }
        .plain small {
            white-space: nowrap;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        @media (max-width: 1100px) {
            .grid,
            .stat-grid {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeInformationBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-information-browser": GodModeInformationBrowserElement;
    }
}
