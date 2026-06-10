import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import { godmodeGet } from "../api/client";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";
import { browsers } from "../manifests/catalog";
import { isGodModeAiExplainAvailable, observeGodModeAiExplainAvailability } from "../shared";
import "../shared";

interface GodModeConfigResponse {
    featuresToHide?: string[];
}

interface PageInfo {
    name: string;
    id: string;
    desc: string;
}

interface PageGroup {
    label: string;
    ids: ReadonlyArray<string>;
}

const PAGE_GROUPS: ReadonlyArray<PageGroup> = [
    {
        label: "Overview",
        ids: ["informationBrowser"]
    },
    {
        label: "Health & operations",
        ids: ["healthRisk", "diagnosticBrowser", "logBrowser", "databaseBrowser"]
    },
    {
        label: "Content & schema",
        ids: ["contentBrowser", "docTypeBrowser", "dataTypeBrowser", "templateBrowser", "partialBrowser", "mediaBrowser", "memberBrowser", "tagBrowser"]
    },
    {
        label: "Relationships & usage",
        ids: ["referenceGraph", "usageBrowser"]
    },
    {
        label: "Developer & runtime",
        ids: ["typesIntro", "serviceBrowser", "extensionExplorer"]
    },
    {
        label: "Data & actions",
        ids: ["keyValueBrowser", "utilityBrowser"]
    }
];

const PAGES_BY_ID = new Map(browsers
    .filter((browser) => browser.id !== "intro" && !browser.skipMenuItem)
    .map((browser) => ({
        name: browser.label,
        id: browser.id,
        desc: browser.description ?? ""
    }))
    .map((page) => [page.id, page] as const));

const PAGE_SECTIONS = PAGE_GROUPS.map((group) => ({
    ...group,
    pages: group.ids.map((id) => PAGES_BY_ID.get(id)).filter((page): page is PageInfo => !!page)
})).filter((group) => group.pages.length);

const PAGES: ReadonlyArray<PageInfo> = PAGE_SECTIONS.flatMap((group) => group.pages);

@customElement("godmode-intro")
export class GodModeIntroElement extends UmbElementMixin(LitElement) {
    @state() private _pages: PageInfo[] = [...PAGES];
    @state() private _pathname?: string;
    @state() private _aiAvailable = isGodModeAiExplainAvailable();
    private _disposeAiAvailabilityObserver?: () => void;

    constructor() {
        super();
        this.consumeContext(UMB_SECTION_CONTEXT, (ctx) => {
            this.observe(
                ctx?.pathname,
                (p: string | undefined) => {
                    this._pathname = p;
                },
                "observePathname"
            );
        });
    }

    override async connectedCallback() {
        super.connectedCallback();
        this._disposeAiAvailabilityObserver = observeGodModeAiExplainAvailability(() => {
            this._aiAvailable = true;
        });

        try {
            const cfg = await godmodeGet<GodModeConfigResponse>("config");
            const hidden = new Set(cfg.featuresToHide ?? []);
            this._pages = PAGES.filter((p) => !hidden.has(p.id) && !hidden.has(p.name));
        } catch {
            // Config call failed — fall back to all pages.
        }
    }

    override disconnectedCallback() {
        this._disposeAiAvailabilityObserver?.();
        this._disposeAiAvailabilityObserver = undefined;
        super.disconnectedCallback();
    }

    private _hrefFor(id: string): string {
        return this._pathname ? `section/${this._pathname}/workspace/${GODMODE_ENTITY_TYPE_PREFIX}-${id}` : "";
    }

    override render() {
        return html`
            <godmode-page heading="Welcome to God Mode" description="The indispensable Umbraco tool that makes developers invincible!">
                ${this._renderAiInstallCallToAction()}
                <uui-box>
                    <uui-table>
                        <uui-table-head>
                            <uui-table-head-cell style="width: 30%">Action</uui-table-head-cell>
                            <uui-table-head-cell>Description</uui-table-head-cell>
                        </uui-table-head>
                        ${PAGE_SECTIONS.map((section) => {
                            const pages = section.pages.filter((page) => this._pages.some((visiblePage) => visiblePage.id === page.id));
                            if (!pages.length) return "";

                            return html`
                                <uui-table-row class="section-row">
                                    <uui-table-cell>${section.label}</uui-table-cell>
                                    <uui-table-cell></uui-table-cell>
                                </uui-table-row>
                                ${pages.map(
                                    (p) => html`
                                        <uui-table-row>
                                            <uui-table-cell>
                                                <a href=${this._hrefFor(p.id)}><strong>${p.name}</strong></a>
                                            </uui-table-cell>
                                            <uui-table-cell>${p.desc}</uui-table-cell>
                                        </uui-table-row>
                                    `
                                )}
                            `;
                        })}
                    </uui-table>
                </uui-box>
                <uui-box style="margin-top: var(--uui-size-space-4)">
                    <p class="muted">
                        Made with love by Dan 'Diplo' Booth —
                        <a href="https://www.diplo.co.uk/" target="_blank" rel="noopener">diplo.co.uk</a>.
                        Report issues on the
                        <a href="https://github.com/DanDiplo/Umbraco.GodMode/issues" target="_blank" rel="noopener">GitHub Issue Tracker</a>.
                    </p>
                </uui-box>
            </godmode-page>
        `;
    }

    private _renderAiInstallCallToAction() {
        if (this._aiAvailable) {
            return "";
        }

        return html`
            <uui-box class="ai-cta">
                <div class="ai-cta-layout">
                    <div>
                        <h2>Add AI explanations</h2>
                        <p>
                            Install the optional God Mode AI package to add contextual Explain buttons powered by Umbraco.AI.
                        </p>
                    </div>
                    <uui-button
                        look="primary"
                        color="positive"
                        href="https://www.nuget.org/packages/Diplo.GodMode.AI/"
                        target="_blank"
                        rel="noopener">
                        View on NuGet
                    </uui-button>
                </div>
            </uui-box>
        `;
    }

    static override styles = css`
        .ai-cta {
            margin-bottom: var(--uui-size-space-4);
        }
        .ai-cta-layout {
            align-items: center;
            display: flex;
            gap: var(--uui-size-space-5);
            justify-content: space-between;
        }
        .ai-cta h2 {
            font-size: 1.125rem;
            margin: 0 0 var(--uui-size-space-2);
        }
        .ai-cta p {
            color: var(--uui-color-text-alt);
            margin: 0;
        }
        .muted {
            color: var(--uui-color-text-alt);
        }
        .section-row {
            background: var(--uui-color-surface-alt);
            color: var(--uui-color-text-alt);
            font-size: 0.8125rem;
            font-weight: 700;
            text-transform: uppercase;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        @media (max-width: 640px) {
            .ai-cta-layout {
                align-items: flex-start;
                flex-direction: column;
            }
        }
    `;
}

export default GodModeIntroElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-intro": GodModeIntroElement;
    }
}
