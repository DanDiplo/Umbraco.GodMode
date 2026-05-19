import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import { isGodModeAiExplainAvailable, observeGodModeAiExplainAvailability } from "../shared/ai-availability";
import type { RegisteredService } from "../shared/types";
import { applySort, toggleSort, type SortState } from "../shared/sort";

@customElement("godmode-service-browser")
export class GodModeServiceBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _items: RegisteredService[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _lifetime = "";
    @state() private _source = "";
    @state() private _visibility = "";
    @state() private _keyedOnly = false;
    @state() private _multiRegisteredOnly = false;
    @state() private _overridesOnly = false;
    @state() private _expanded = new Set<string>();
    @state() private _sort: SortState = { column: "name", reverse: false };
    @state() private _isAiExplainAvailable = isGodModeAiExplainAvailable();
    private _disposeAvailabilityObserver?: () => void;

    override connectedCallback(): void {
        super.connectedCallback();
        this._disposeAvailabilityObserver = observeGodModeAiExplainAvailability(() => {
            this._isAiExplainAvailable = true;
        });
        void this._load();
    }

    override disconnectedCallback(): void {
        this._disposeAvailabilityObserver?.();
        this._disposeAvailabilityObserver = undefined;
        super.disconnectedCallback();
    }

    private async _load() {
        this._loading = true;
        try {
            this._items = await godmodeGet<RegisteredService[]>("reflection/services");
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): RegisteredService[] {
        const q = this._search.trim().toLowerCase();
        const multiRegisteredNames = this._multiRegisteredNames();
        const possibleOverrideNames = this._possibleOverrideNames();
        const matched = this._items.filter((s) => {
            if (q && !this._searchText(s).includes(q)) return false;
            if (this._lifetime && s.lifetime !== this._lifetime) return false;
            if (this._source && this._sourceFor(s) !== this._source) return false;
            if (this._visibility === "public" && !s.isPublic) return false;
            if (this._visibility === "internal" && s.isPublic) return false;
            if (this._keyedOnly && !s.key) return false;
            if (this._multiRegisteredOnly && !multiRegisteredNames.has(s.name ?? "")) return false;
            if (this._overridesOnly && !possibleOverrideNames.has(s.name ?? "")) return false;
            return true;
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as RegisteredService[];
    }

    private _searchText(s: RegisteredService): string {
        return [
            s.name,
            s.namespace,
            s.fullName,
            s.implementName,
            s.implementNamespace,
            s.implementFullName,
            s.lifetime,
            s.key,
            this._sourceFor(s)
        ]
            .filter(Boolean)
            .join(" ")
            .toLowerCase();
    }

    private _sourceFor(s: RegisteredService): string {
        const ns = `${s.namespace ?? ""} ${s.implementNamespace ?? ""}`;
        if (ns.includes("Diplo.GodMode")) return "GodMode";
        if (ns.includes("Umbraco.")) return "Umbraco";
        if (ns.includes("Microsoft.") || ns.includes("System.")) return "Microsoft";
        if (!ns.trim()) return "Factory/Instance";
        return "Custom/Package";
    }

    private _multiRegisteredNames(): Set<string> {
        const counts = new Map<string, number>();
        for (const service of this._items) {
            if (!service.name) continue;
            counts.set(service.name, (counts.get(service.name) ?? 0) + 1);
        }
        return new Set([...counts].filter(([, count]) => count > 1).map(([name]) => name));
    }

    private _possibleOverrideNames(): Set<string> {
        const byName = new Map<string, RegisteredService[]>();
        for (const service of this._items) {
            if (!service.name || this._isExpectedMultiRegistration(service)) continue;
            byName.set(service.name, [...(byName.get(service.name) ?? []), service]);
        }

        return new Set(
            [...byName]
                .filter(([, services]) => {
                    if (services.length < 2) return false;
                    const implementations = new Set(services.map((s) => s.implementName ?? "Factory / instance"));
                    const lifetimes = new Set(services.map((s) => s.lifetime));
                    return implementations.size > 1 || lifetimes.size > 1;
                })
                .map(([name]) => name)
        );
    }

    private _isExpectedMultiRegistration(service: RegisteredService): boolean {
        const name = service.name ?? "";
        const implementation = service.implementName ?? "";

        return [
            "IConfigureOptions<",
            "IPostConfigureOptions<",
            "IValidateOptions<",
            "INotificationHandler<",
            "IEventHandler<",
            "IHostedService",
            "IHealthCheck",
            "IValidator<",
            "IEnumerable<",
            "ConsoleFormatter"
        ].some((pattern) => name.includes(pattern) || implementation.includes(pattern));
    }

    private _lifetimeCounts(): Map<string, number> {
        const counts = new Map<string, number>();
        for (const service of this._items) {
            counts.set(service.lifetime, (counts.get(service.lifetime) ?? 0) + 1);
        }
        return counts;
    }

    private _toggleExpanded(id: string) {
        const expanded = new Set(this._expanded);
        expanded.has(id) ? expanded.delete(id) : expanded.add(id);
        this._expanded = expanded;
    }

    private _rowId(s: RegisteredService, index: number): string {
        return `${s.name ?? ""}|${s.implementName ?? ""}|${s.lifetime}|${s.key ?? ""}|${index}`;
    }

    private _onSortChange = (e: CustomEvent<string>) => {
        this._sort = toggleSort(this._sort, e.detail);
    };

    override render() {
        const lifetimes = Array.from(new Set(this._items.map((s) => s.lifetime).filter(Boolean))).sort();
        const sources = Array.from(new Set(this._items.map((s) => this._sourceFor(s)))).sort();
        const multiRegisteredNames = this._multiRegisteredNames();
        const possibleOverrideNames = this._possibleOverrideNames();
        const lifetimeCounts = this._lifetimeCounts();
        const results = this._filtered();
        return html`
            <godmode-page heading="Services" description="Browse services registered with the DI container." show-reload @reload=${() => void this._load()}>
                <div class="summary">
                    ${lifetimes.map(
                        (lifetime) => html`
                            <button class=${`summary-card ${this._lifetime === lifetime ? "active" : ""}`} @click=${() => (this._lifetime = this._lifetime === lifetime ? "" : lifetime)}>
                                <strong>${lifetimeCounts.get(lifetime) ?? 0}</strong>
                                <span>${lifetime}</span>
                            </button>
                        `
                    )}
                    <button
                        class=${`summary-card ${this._multiRegisteredOnly ? "active" : ""}`}
                        @click=${() => (this._multiRegisteredOnly = !this._multiRegisteredOnly)}
                    >
                        <strong>${multiRegisteredNames.size}</strong>
                        <span>Multi-registered</span>
                    </button>
                    <button class=${`summary-card ${this._overridesOnly ? "active" : ""}`} @click=${() => (this._overridesOnly = !this._overridesOnly)}>
                        <strong>${possibleOverrideNames.size}</strong>
                        <span>Possible Overrides</span>
                    </button>
                    <button class=${`summary-card ${this._keyedOnly ? "active" : ""}`} @click=${() => (this._keyedOnly = !this._keyedOnly)}>
                        <strong>${this._items.filter((s) => s.key).length}</strong>
                        <span>Keyed</span>
                    </button>
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
                                placeholder="Filter by service or implementation type"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Lifetime</label>
                            <select @change=${(e: Event) => (this._lifetime = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${lifetimes.map((l) => html`<option value=${l}>${l}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Source</label>
                            <select @change=${(e: Event) => (this._source = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${sources.map((s) => html`<option value=${s}>${s}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Visibility</label>
                            <select @change=${(e: Event) => (this._visibility = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                <option value="public">Public</option>
                                <option value="internal">Internal / non-public</option>
                            </select>
                        </div>
                    </div>
                    <p class="help">
                        Multi-registered services appear more than once in the container and are often intentional extension points. Possible overrides exclude common
                        options, notification, hosted-service, formatter and validator patterns, then highlight repeated service types where resolving one service may use
                        the final registration.
                    </p>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                          <uui-table @sort-change=${this._onSortChange}>
                              <uui-table-head>
                                  <godmode-sort-header class="service-col" column="name" .sort=${this._sort}>Service</godmode-sort-header>
                                  <godmode-sort-header class="implementation-col" column="implementName" .sort=${this._sort}>Implementation</godmode-sort-header>
                                  <godmode-sort-header class="lifetime-col" column="lifetime" .sort=${this._sort}>Lifetime</godmode-sort-header>
                                  <uui-table-head-cell class="flags-col">Flags</uui-table-head-cell>
                                  <godmode-sort-header class="key-col" column="key" .sort=${this._sort}>Key</godmode-sort-header>
                                  ${this._isAiExplainAvailable ? html`<uui-table-head-cell class="action-head">Actions</uui-table-head-cell>` : ""}
                              </uui-table-head>
                              ${results.map((s, index) => this._renderRow(s, index, multiRegisteredNames, possibleOverrideNames))}
                          </uui-table>
                      `}
            </godmode-page>
        `;
    }

    private _renderRow(s: RegisteredService, index: number, multiRegisteredNames: Set<string>, possibleOverrideNames: Set<string>) {
        const id = this._rowId(s, index);
        const isMultiRegistered = multiRegisteredNames.has(s.name ?? "");
        const isPossibleOverride = possibleOverrideNames.has(s.name ?? "");
        const source = this._sourceFor(s);

        return html`
            <uui-table-row class=${this._expanded.has(id) ? "expanded" : ""} @click=${() => this._toggleExpanded(id)}>
                <uui-table-cell class="type-cell service-col" title=${s.fullName ?? s.name ?? ""}><code>${s.name}</code></uui-table-cell>
                <uui-table-cell class="type-cell implementation-col" title=${s.implementFullName ?? s.implementName ?? ""}>
                    <code>${s.implementName ?? "Factory / instance"}</code>
                </uui-table-cell>
                <uui-table-cell class="lifetime-col">${s.lifetime}</uui-table-cell>
                <uui-table-cell class="flags-col">
                    <span class="flags">
                        <span class="pill">${source}</span>
                        ${s.key ? html`<span class="pill accent">Keyed</span>` : ""}
                        ${isPossibleOverride ? html`<span class="pill danger">Possible override</span>` : ""}
                        ${isMultiRegistered ? html`<span class="pill warning">Multi</span>` : ""}
                        ${s.isPublic ? "" : html`<span class="pill">Internal</span>`}
                    </span>
                </uui-table-cell>
                <uui-table-cell class="key-col" title=${s.key ?? ""}><small>${s.key ?? ""}</small></uui-table-cell>
                ${this._isAiExplainAvailable
                    ? html`
                          <uui-table-cell class="action-cell" @click=${(e: Event) => e.stopPropagation()}>
                              <godmode-ai-explain-host .subject=${this._explainSubject(s, source, isMultiRegistered, isPossibleOverride)}></godmode-ai-explain-host>
                          </uui-table-cell>
                      `
                    : ""}
            </uui-table-row>
            ${this._expanded.has(id)
                ? html`
                      <uui-table-row class="details">
                          <uui-table-cell class="detail-item">
                              <strong>Service namespace</strong>
                              <code>${s.namespace ?? ""}</code>
                          </uui-table-cell>
                          <uui-table-cell class="detail-item">
                              <strong>Implementation namespace</strong>
                              <code>${s.implementNamespace ?? ""}</code>
                          </uui-table-cell>
                          <uui-table-cell class="detail-item">
                              <strong>Lifetime</strong>
                              <span>${s.lifetime}</span>
                          </uui-table-cell>
                          <uui-table-cell class="detail-item">
                              <strong>Source</strong>
                              <span>${source}</span>
                          </uui-table-cell>
                          <uui-table-cell class="detail-item">
                              <strong>Key</strong>
                              <code>${s.key ?? ""}</code>
                          </uui-table-cell>
                          ${this._isAiExplainAvailable ? html`<uui-table-cell></uui-table-cell>` : ""}
                      </uui-table-row>
                      <uui-table-row class="details details-full">
                          <uui-table-cell class="detail-item">
                              <strong>Service full name</strong>
                              <code>${s.fullName ?? ""}</code>
                          </uui-table-cell>
                          <uui-table-cell class="detail-item">
                              <strong>Implementation full name</strong>
                              <code>${s.implementFullName ?? ""}</code>
                          </uui-table-cell>
                          ${this._isAiExplainAvailable ? html`<uui-table-cell></uui-table-cell>` : ""}
                          <uui-table-cell></uui-table-cell>
                          <uui-table-cell></uui-table-cell>
                          <uui-table-cell></uui-table-cell>
                      </uui-table-row>
                  `
                : ""}
        `;
    }

    private _explainSubject(s: RegisteredService, source: string, isMultiRegistered: boolean, isPossibleOverride: boolean) {
        return {
            subjectType: "ASP.NET Core dependency injection registration",
            title: s.name ?? s.implementName ?? "Registered service",
            data: {
                serviceName: s.name,
                serviceNamespace: s.namespace,
                serviceFullName: s.fullName,
                implementationName: s.implementName,
                implementationNamespace: s.implementNamespace,
                implementationFullName: s.implementFullName,
                lifetime: s.lifetime,
                key: s.key,
                source,
                isPublic: s.isPublic,
                isKeyed: !!s.key,
                isMultiRegistered,
                isPossibleOverride
            }
        };
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
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
            font-size: 1.35rem;
        }
        .filters {
            display: grid;
            grid-template-columns: 2fr 1fr 1fr 1fr;
            gap: var(--uui-size-space-4);
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
        uui-table {
            width: 100%;
            table-layout: fixed;
        }
        .service-col {
            width: 29%;
        }
        .implementation-col {
            width: 31%;
        }
        .lifetime-col {
            width: 8rem;
        }
        .flags-col {
            width: 13rem;
        }
        .key-col {
            width: 9rem;
        }
        .action-head:empty,
        .action-cell:empty {
            display: none;
        }
        .action-head,
        .action-cell {
            width: 5.5rem;
        }
        .type-cell {
            min-width: 0;
        }
        .type-cell code {
            display: inline;
            white-space: normal;
            overflow-wrap: anywhere;
            word-break: break-word;
            line-height: 1.35;
        }
        .key-col small {
            display: block;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        .help {
            color: var(--uui-color-text-alt);
            margin: var(--uui-size-space-4) 0 0;
            max-width: 920px;
        }
        uui-table-row {
            cursor: pointer;
        }
        .flags {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-1);
            max-width: 100%;
        }
        .pill {
            border-radius: 3px;
            background: var(--uui-color-border);
            color: var(--uui-color-text);
            padding: 1px var(--uui-size-space-1);
            font-size: 0.75rem;
            font-weight: 600;
            max-width: 100%;
            overflow-wrap: anywhere;
        }
        .pill.accent {
            background: var(--uui-color-positive);
            color: var(--uui-color-surface);
        }
        .pill.warning {
            background: var(--uui-color-warning);
            color: var(--uui-color-text);
        }
        .pill.danger {
            background: var(--uui-color-danger);
            color: var(--uui-color-surface);
        }
        .details {
            cursor: default;
            background: var(--uui-color-surface-alt);
        }
        .detail-item {
            vertical-align: top;
        }
        .detail-item strong {
            display: block;
            margin-bottom: var(--uui-size-space-1);
            color: var(--uui-color-text-alt);
            font-size: 0.78rem;
        }
        .action-cell {
            cursor: default;
            text-align: right;
        }
        .action-cell godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
        .detail-item code,
        .detail-item span {
            display: block;
            white-space: normal;
            overflow-wrap: anywhere;
            word-break: break-word;
            line-height: 1.35;
        }
        @media (max-width: 1000px) {
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeServiceBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-service-browser": GodModeServiceBrowserElement;
    }
}
