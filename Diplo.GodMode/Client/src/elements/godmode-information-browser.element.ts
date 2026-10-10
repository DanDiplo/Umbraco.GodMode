import { LitElement, css, customElement, html, state, svg } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import "../shared/godmode-live-clock.element";
import { browserTimeZone, formatOffset, loadServerTime, parseApiDate, zoneOffsetMinutes } from "../shared/date-time";
import type { DeliveryApiDiagnostics, ServerTimeInfo, SystemDateEvidence, UtilityDiagnostics } from "../shared/types";

/** Clock differences smaller than this are treated as normal network jitter. */
const DRIFT_WARNING_MS = 30_000;

@customElement("godmode-information-browser")
export class GodModeInformationBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _diagnostics: UtilityDiagnostics | null = null;
    @state() private _deliveryApiDiagnostics: DeliveryApiDiagnostics | null = null;
    @state() private _serverTime: ServerTimeInfo | null = null;
    /** Server clock minus browser clock, in milliseconds. */
    @state() private _driftMs: number | null = null;
    @state() private _roundTripMs = 0;
    @state() private _evidence: SystemDateEvidence | null = null;
    @state() private _evidenceLoading = false;
    @state() private _evidenceError = "";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _loadServerTime() {
        const started = Date.now();
        const info = await loadServerTime(true);
        const finished = Date.now();
        this._serverTime = info;
        const serverNow = parseApiDate(info?.serverUtcNow)?.getTime();
        this._roundTripMs = finished - started;
        this._driftMs = serverNow === undefined ? null : serverNow - (started + finished) / 2;
    }

    private async _loadEvidence() {
        this._evidenceLoading = true;
        this._evidenceError = "";
        try {
            this._evidence = await godmodeGet<SystemDateEvidence>("utilities/system-date-evidence");
        } catch (e) {
            console.error(e);
            this._evidenceError = "Evidence could not be loaded. Try again; no checks have been confirmed.";
        } finally {
            this._evidenceLoading = false;
        }
    }

    private async _load() {
        void this._loadServerTime();
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
                machineName: diagnostics?.app.machineName,
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
                cacheDatabaseRows: diagnostics?.cache.databaseRows,
                serverStats: diagnostics?.serverStats,
                serverTime: this._serverTime,
                browserTimeZone: browserTimeZone(),
                clockDriftMs: this._driftMs,
                deliveryApi: this._deliveryApiDiagnostics
            },
            context: {
                diagnostics,
                guidance: [
                    "Explain the current Umbraco environment and operational signals shown in the Information browser.",
                    "Call out missing package assets or folders and unusual cache folder state.",
                    "Explain the server time zone, any clock drift, and whether the Umbraco 17 UTC system date migration settings look right.",
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
            <h5>Cache</h5>
            <dl class="compact-dl">
                <dt>Published cache</dt>
                <dd>${d.cache.publishedContentCacheType || "Unavailable"}</dd>
                <dt>NuCache serializer</dt>
                <dd>${d.cache.nuCacheSerializerType || "Default"}</dd>
            </dl>
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
            <h5 class="subheading">Database Cache</h5>
            <ul class="plain offset">
                ${d.cache.databaseRows.map(
                    (row) => html`
                        <li title=${row.table}>
                            <uui-tag color=${row.exists ? "default" : "warning"}>${row.exists ? row.count.toLocaleString() : "Missing"}</uui-tag>
                            <span>${row.label}</span>
                            <small>${row.table}</small>
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
                ${this._renderHeroCards(d)}
                <section class="dashboard-section">
                    <div class="section-heading">
                        <h4>Resources</h4>
                        <span>Runtime pressure and disk capacity</span>
                    </div>
                    ${this._renderServerStats(d)}
                </section>
                <div class="dashboard-grid">
                    ${this._renderInfoPanel(d)}
                    <section class="panel">
                        <div class="section-heading compact">
                            <h4>Package Assets</h4>
                            <span>Manifest and bundle checks</span>
                        </div>
                        <ul class="plain asset-list">
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
                    </section>
                    <section class="panel">
                        <div class="section-heading compact">
                            <h4>Folder Sizes</h4>
                            <span>Local storage used by package-adjacent folders</span>
                        </div>
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
                    </section>
                    ${this._renderDateTimePanel()}
                    <section class="panel operations-panel">
                        <div class="section-heading compact">
                            <h4>Cache & Database</h4>
                            <span>Cache configuration and important Umbraco tables</span>
                        </div>
                        <div class="operations-grid">
                            <div>${this._renderCacheDiagnostics(d)}</div>
                            <div>
                                <h5>Database</h5>
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
                    </section>
                </div>
            </uui-box>
        `;
    }

    private _renderHeroCards(d: UtilityDiagnostics) {
        const umbracoVersion = this._splitVersion(d.app.umbracoSemanticVersion || d.app.umbracoVersion);
        const godModeVersion = this._splitVersion(d.app.godModeVersion);

        return html`
            <div class="hero-grid">
                ${this._renderKeyCard("Environment", d.app.environmentName, d.app.debugMode ? "Debug mode enabled" : "Debug mode off", d.app.debugMode ? "warning" : "positive")}
                ${this._renderKeyCard("Machine", d.app.machineName || "Unavailable", d.app.runtimeIdentifier, "default")}
                ${this._renderTimeZoneCard()}
                ${this._renderKeyCard("App", `Umbraco ${umbracoVersion.major}`, [umbracoVersion.detail, d.app.applicationMainUrl || "Main URL not configured"], "default")}
                ${this._renderKeyCard("GodMode", godModeVersion.major, [godModeVersion.detail, `Process ${d.app.processId} - ${d.app.uptime}`], "positive")}
            </div>
        `;
    }

    private _renderInfoPanel(d: UtilityDiagnostics) {
        return html`
            <section class="panel info-panel">
                <div class="section-heading compact">
                    <h4>Application</h4>
                    <span>Runtime and hosting identity</span>
                </div>
                <dl>
                    <dt>.NET</dt>
                    <dd>${d.app.dotNetVersion}</dd>
                    <dt>OS</dt>
                    <dd>${d.app.operatingSystem}</dd>
                    <dt>Architecture</dt>
                    <dd>${d.app.processArchitecture}</dd>
                    <dt>Web server</dt>
                    <dd>${d.app.webServer}</dd>
                    <dt>Content root</dt>
                    <dd>${d.app.contentRootPath}</dd>
                    <dt>Started</dt>
                    <dd><godmode-date .value=${d.app.startedAt}></godmode-date> (${d.app.uptime})</dd>
                </dl>
            </section>
        `;
    }

    private _renderTimeZoneCard() {
        const t = this._serverTime;
        if (!t) return this._renderKeyCard("Time Zone", "Loading", "", "default");

        const browserZone = browserTimeZone();
        const browserOffset = zoneOffsetMinutes(browserZone);
        const sameOffset = browserOffset === t.utcOffsetMinutes;
        const label = t.isUtc ? "UTC" : t.ianaId || t.timeZoneId;

        return this._renderKeyCard(
            "Time Zone",
            label,
            [
                `${t.utcOffset}${t.isDaylightSavingTime ? " (daylight saving)" : ""}`,
                sameOffset ? "Same offset as your browser" : `Your browser: ${browserZone} ${formatOffset(browserOffset)}`
            ],
            this._isDriftWarning() ? "warning" : "default"
        );
    }

    private _isDriftWarning() {
        return this._driftMs !== null && Math.abs(this._driftMs) > DRIFT_WARNING_MS;
    }

    private _renderDateTimePanel() {
        const t = this._serverTime;
        if (!t) {
            return html`<section class="panel datetime-panel"><uui-loader></uui-loader></section>`;
        }

        const serverZone = t.ianaId || (t.isUtc ? "UTC" : "");
        const browserZone = browserTimeZone();
        const browserOffset = zoneOffsetMinutes(browserZone);
        const migration = t.systemDateMigration;

        return html`
            <section class="panel datetime-panel">
                <div class="section-heading compact">
                    <h4>Date & Time</h4>
                    <span>Umbraco 17 stores system dates in UTC</span>
                </div>
                <div class="datetime-grid">
                    <div>
                        <h5>Server</h5>
                        <dl class="compact-dl">
                            <dt>Server time</dt>
                            <dd><strong><godmode-live-clock .timeZone=${serverZone || "UTC"} .driftMs=${this._driftMs ?? 0} .offsetMinutes=${serverZone ? 0 : t.utcOffsetMinutes}></godmode-live-clock></strong></dd>
                            <dt>UTC</dt>
                            <dd><godmode-live-clock timeZone="UTC" .driftMs=${this._driftMs ?? 0}></godmode-live-clock></dd>
                            <dt>Time zone</dt>
                            <dd>${t.timeZoneId}${t.ianaId && t.ianaId !== t.timeZoneId ? html` <small>(${t.ianaId})</small>` : ""}</dd>
                            ${t.windowsId && t.windowsId !== t.timeZoneId ? html`<dt>Windows id</dt><dd>${t.windowsId}</dd>` : ""}
                            <dt>Offset</dt>
                            <dd>${t.utcOffset} ${t.isDaylightSavingTime ? html`<uui-tag look="secondary">${t.daylightName || "Daylight saving"}</uui-tag>` : ""}</dd>
                            ${t.nextTransition
                                ? html`<dt>Next change</dt><dd><godmode-date .value=${t.nextTransition} relative></godmode-date> to ${t.nextTransitionUtcOffset}</dd>`
                                : ""}
                            <dt>TZ variable</dt>
                            <dd>${t.tzEnvironmentVariable || html`<span class="muted">Not set</span>`}</dd>
                            <dt>Culture</dt>
                            <dd>${t.cultureName || "Invariant"} <small>(${t.shortDatePattern} ${t.longTimePattern})</small></dd>
                        </dl>
                    </div>
                    <div>
                        <h5>You</h5>
                        <dl class="compact-dl">
                            <dt>Browser time</dt>
                            <dd><strong><godmode-live-clock .timeZone=${browserZone}></godmode-live-clock></strong></dd>
                            <dt>Time zone</dt>
                            <dd>${browserZone} <small>(${formatOffset(browserOffset)})</small></dd>
                            <dt>Difference</dt>
                            <dd>${this._renderOffsetDifference(browserOffset - t.utcOffsetMinutes)}</dd>
                            <dt>Clock drift</dt>
                            <dd>${this._renderDrift()}</dd>
                        </dl>
                        <p class="muted">
                            GodMode shows dates in your browser's time zone by default. Switch to server time or UTC here; the choice applies to every GodMode
                            view.
                        </p>
                        <godmode-date-mode-toggle></godmode-date-mode-toggle>
                    </div>
                    <div>
                        <h5>UTC Date Migration</h5>
                        <dl class="compact-dl">
                            <dt>Enabled</dt>
                            <dd>${migration.enabled ? "Yes" : "No"} <small>(current setting)</small></dd>
                            <dt>Time zone</dt>
                            <dd>
                                ${migration.configuredTimeZone
                                    ? html`${migration.configuredTimeZone}
                                           <uui-tag color=${migration.configuredTimeZoneValid === false ? "warning" : "default"}>
                                               ${migration.configuredTimeZoneValid === null ? "Not validated" : migration.configuredTimeZoneValid ? "Valid for database" : "Not valid for database"}
                                          </uui-tag>`
                                    : html`<span class="muted">Not configured, uses ${migration.effectiveTimeZone}</span>`}
                            </dd>
                            <dt>Validation</dt><dd>${migration.timeZoneValidationMessage}</dd>
                            <dt>Rules match server</dt><dd>${migration.effectiveTimeZoneResolved ? (migration.effectiveTimeZoneMatchesServer ? "Yes" : "No") : "Unknown"}</dd>
                            <dt>Database</dt>
                            <dd>${migration.databaseType}${migration.usesBaseOffsetOnly ? html` <small>(converts with base offset, ignores daylight saving)</small>` : ""}</dd>
                            <dt>Migrations</dt>
                            <dd><uui-tag color=${migration.upgradeComplete ? "positive" : "warning"}>${migration.upgradeComplete ? "All applied" : "Pending"}</uui-tag></dd>
                        </dl>
                        <p class="muted">Current settings do not establish what a completed upgrade used. Fresh v17 installs need no historical conversion; changing settings now does not rerun the migration.</p>
                        ${this._renderEvidence()}
                    </div>
                </div>
            </section>
        `;
    }

    private _renderOffsetDifference(minutes: number) {
        if (minutes === 0) return html`<uui-tag color="positive">Same as server</uui-tag>`;
        const hours = Math.abs(minutes) / 60;
        const amount = Number.isInteger(hours) ? `${hours}h` : `${Math.floor(hours)}h ${Math.abs(minutes) % 60}m`;
        return html`<uui-tag color="warning">You are ${amount} ${minutes > 0 ? "ahead of" : "behind"} the server</uui-tag>`;
    }

    private _renderDrift() {
        if (this._driftMs === null) return html`<span class="muted">Unknown</span>`;
        const seconds = Math.round(this._driftMs / 1000);
        const accuracy = Math.max(1, Math.round(this._roundTripMs / 2000));
        const text = seconds === 0 ? "In sync" : `Server is ${Math.abs(seconds)}s ${seconds > 0 ? "ahead" : "behind"}`;
        return html`<uui-tag color=${this._isDriftWarning() ? "danger" : "positive"} title="Measured against your browser's clock, accurate to about ±${accuracy}s"
                >${text}</uui-tag
            >${this._isDriftWarning() ? html` <small>Large drift can break sign-in tokens and scheduled publishing.</small>` : ""}`;
    }

    private _renderEvidence() {
        const evidence = this._evidence;
        if (!evidence) {
            return html`${this._evidenceError ? html`<p>${this._evidenceError}</p>` : ""}<uui-button compact look="secondary" label="Check migration evidence" ?disabled=${this._evidenceLoading} @click=${() => void this._loadEvidence()}>
                ${this._evidenceLoading ? "Checking..." : "Check migration evidence"}
            </uui-button>`;
        }

        const future = evidence.futureDatedRows.filter((row) => row.checkSucceeded && row.count > 0);

        return html`
            <h5 class="subheading">Evidence</h5>
            <p class="muted">Checked <godmode-date .value=${evidence.checkedAtUtc}></godmode-date>; cached for up to two minutes.</p>
            <p>${evidence.logCheckMessage}</p>
            <p>${evidence.databaseCheckMessage}</p>
            <ul class="plain">
                ${evidence.migrationLogEntries.length
                    ? evidence.migrationLogEntries.map(
                          (entry) => html`<li>
                              <uui-tag color=${entry.level === "Error" || entry.level === "Fatal" ? "danger" : "default"}>${entry.level}</uui-tag>
                              <span>${entry.message} <small><godmode-date .value=${entry.timestamp}></godmode-date></small></span>
                          </li>`
                      )
                     : html`<li><span class="muted">${evidence.logCheckSucceeded ? "No migration entries in the checked local log files." : "Log evidence is unavailable or partial."}</span></li>`}
                ${future.length
                    ? future.map(
                          (row) => html`<li>
                              <uui-tag color="danger">${row.count.toLocaleString()}</uui-tag>
                              <span>future dated rows in <code>${row.table}.${row.column}</code></span>
                          </li>`
                      )
                     : evidence.databaseCheckSucceeded ? html`<li><span>No future dated rows in the four checked columns.</span></li>` : html`<li><uui-tag color="warning">Incomplete</uui-tag><span>Future date checks are unavailable or partial.</span></li>`}
                ${evidence.futureDatedRows.filter((row) => !row.checkSucceeded).map((row) => html`<li>${row.table}.${row.column}: ${row.checkMessage}</li>`)}
            </ul>
            <p class="muted">Future date cutoff: <godmode-date .value=${evidence.futureDateCutoffUtc}></godmode-date>. Older shifts and shifts into the past are not detected. No matches does not prove a correct migration; future dates may also come from clock differences or imports.</p>
        `;
    }

    private _renderKeyCard(label: string, value: string, detail: string | string[], tone: "default" | "positive" | "warning") {
        const details = Array.isArray(detail) ? detail.filter(Boolean) : [detail];

        return html`
            <div class="key-card ${tone}">
                <span class="metric-label">${label}</span>
                <strong>${value}</strong>
                <small>${details.map((item, index) => html`${index ? html`<br />` : ""}${item}`)}</small>
            </div>
        `;
    }

    private _splitVersion(version: string) {
        const [major, ...detailParts] = version.split("+");
        return {
            major: major || version || "Unavailable",
            detail: detailParts.length ? `+${detailParts.join("+")}` : ""
        };
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
        .dashboard-section,
        .panel {
            min-width: 0;
            padding: var(--uui-size-space-5);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }
        .dashboard-section {
            margin-bottom: var(--uui-size-space-4);
        }
        .dashboard-grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-4);
            align-items: start;
        }
        .operations-panel,
        .datetime-panel {
            grid-column: span 3;
        }
        .datetime-grid {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-6);
        }
        .datetime-grid h5 {
            margin: var(--uui-size-space-3) 0 var(--uui-size-space-2);
        }
        .operations-grid {
            display: grid;
            grid-template-columns: minmax(0, 1.25fr) minmax(0, 1fr);
            gap: var(--uui-size-space-6);
        }
        .hero-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .key-card {
            min-width: 0;
            min-height: 8rem;
            display: grid;
            align-content: space-between;
            gap: var(--uui-size-space-2);
            padding: var(--uui-size-space-5);
            border: 1px solid var(--uui-color-border);
            border-left: 0.35rem solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
        }
        .key-card.positive {
            border-left-color: var(--uui-color-positive);
        }
        .key-card.warning {
            border-left-color: var(--uui-color-warning);
        }
        .key-card strong {
            min-width: 0;
            font-size: 1.35rem;
            line-height: 1.2;
            overflow-wrap: anywhere;
        }
        .key-card small {
            min-width: 0;
            color: var(--uui-color-text-alt);
            overflow-wrap: anywhere;
        }
        .section-heading {
            display: flex;
            align-items: baseline;
            justify-content: space-between;
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-4);
        }
        .section-heading.compact {
            display: block;
            margin-bottom: var(--uui-size-space-3);
        }
        .section-heading h4 {
            margin: 0;
        }
        .section-heading span {
            min-width: 0;
            color: var(--uui-color-text-alt);
            font-size: 0.875rem;
            overflow-wrap: anywhere;
        }
        .stat-grid {
            display: grid;
            grid-template-columns: repeat(4, minmax(0, 1fr));
            gap: var(--uui-size-space-3);
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
        h5 {
            margin: 0 0 var(--uui-size-space-2);
            font-size: 0.875rem;
        }
        .subheading {
            margin-top: var(--uui-size-space-4);
        }
        dl {
            display: grid;
            grid-template-columns: max-content minmax(0, 1fr);
            gap: var(--uui-size-space-1) var(--uui-size-space-3);
            margin: 0;
        }
        .compact-dl {
            margin-bottom: var(--uui-size-space-3);
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
        .asset-list li {
            align-items: flex-start;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        @media (max-width: 1100px) {
            .dashboard-grid,
            .hero-grid,
            .operations-grid,
            .datetime-grid,
            .stat-grid {
                grid-template-columns: 1fr;
            }
            .operations-panel,
            .datetime-panel {
                grid-column: auto;
            }
            .section-heading {
                display: block;
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
