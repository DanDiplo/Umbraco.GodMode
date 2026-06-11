import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";
import { browsers } from "../manifests/catalog";

interface ChildEntry {
    id: string;
    label: string;
    icon: string;
    children?: ReadonlyArray<ChildEntry>;
}

interface ChildGroup {
    label: string;
    ids: ReadonlyArray<string>;
}

const TYPES_BROWSER_ID = "typesIntro";

const CHILD_GROUPS: ReadonlyArray<ChildGroup> = [
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
        ids: ["typesIntro", "serviceBrowser", "extensionExplorer", "nugetPackages"]
    },
    {
        label: "Data & actions",
        ids: ["keyValueBrowser", "utilityBrowser"]
    }
];

const TYPE_CHILDREN: ReadonlyArray<ChildEntry> = browsers
    .filter((browser) => browser.skipMenuItem)
    .sort((a, b) => b.weight - a.weight)
    .map((browser) => ({
        id: browser.id,
        label: browser.label,
        icon: browser.icon
    }));

const CHILDREN_BY_ID = new Map<string, ChildEntry>(browsers
    .filter((browser) => browser.id !== "intro" && !browser.skipMenuItem)
    .map((browser) => ({
        id: browser.id,
        label: browser.label,
        icon: browser.icon,
        children: browser.id === TYPES_BROWSER_ID ? TYPE_CHILDREN : undefined
    }))
    .map((child): [string, ChildEntry] => [child.id, child]));

const GROUPS = CHILD_GROUPS.map((group) => ({
    ...group,
    children: group.ids.map((id) => CHILDREN_BY_ID.get(id)).filter((child): child is ChildEntry => !!child)
})).filter((group) => group.children.length);

@customElement("godmode-root-menu-item")
export class GodModeRootMenuItemElement extends UmbElementMixin(LitElement) {
    @property({ type: Object, attribute: false }) manifest: { meta: { label?: string; icon?: string }; name: string } | null = null;

    @state() private _pathname?: string;
    @state() private _open = false;
    @state() private _typesOpen = false;

    constructor() {
        super();
        this.consumeContext(UMB_SECTION_CONTEXT, (ctx) => {
            this.observe(
                ctx?.pathname,
                (pathname: string | undefined) => {
                    this._pathname = pathname;
                },
                "observePathname"
            );
        });
    }

    private _hrefFor(id: string): string | undefined {
        if (!this._pathname) return undefined;
        return `section/${this._pathname}/workspace/${GODMODE_ENTITY_TYPE_PREFIX}-${id}`;
    }

    private _toggleRoot = () => {
        this._open = !this._open;
    };

    private _toggleTypes = () => {
        this._typesOpen = !this._typesOpen;
    };

    override render() {
        if (!this.manifest) return html``;

        return html`
            <umb-menu-item-layout
                .href=${this._hrefFor("intro")}
                .iconName=${this.manifest.meta.icon ?? "icon-science"}
                .label=${this.manifest.meta.label ?? this.manifest.name}
                ?has-children=${true}
                @click=${this._toggleRoot}
            ></umb-menu-item-layout>
            ${this._open ? html`<div class="children">${GROUPS.map((group) => this._renderGroup(group))}</div>` : ""}
        `;
    }

    private _renderGroup(group: { label: string; children: ReadonlyArray<ChildEntry> }) {
        return html`
            <div class="group-label">${group.label}</div>
            ${group.children.map((child) => this._renderChild(child))}
        `;
    }

    private _renderChild(child: ChildEntry) {
        if (!child.children?.length) {
            return html`
                <umb-menu-item-layout
                    .href=${this._hrefFor(child.id)}
                    .iconName=${child.icon}
                    .label=${child.label}
                ></umb-menu-item-layout>
            `;
        }

        return html`
            <umb-menu-item-layout
                .href=${this._hrefFor(child.id)}
                .iconName=${child.icon}
                .label=${child.label}
                ?has-children=${true}
                @click=${this._toggleTypes}
            ></umb-menu-item-layout>
            ${this._typesOpen
                ? html`<div class="children nested">
                      ${child.children.map((grandchild) => html`
                          <umb-menu-item-layout
                              .href=${this._hrefFor(grandchild.id)}
                              .iconName=${grandchild.icon}
                              .label=${grandchild.label}
                          ></umb-menu-item-layout>
                      `)}
                  </div>`
                : ""}
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }

        .children {
            display: block;
            padding-left: var(--uui-size-space-4);
        }

        .group-label {
            margin: var(--uui-size-space-3) 0 var(--uui-size-space-1);
            color: var(--uui-color-text-alt);
            font-size: 0.75rem;
            font-weight: 700;
            text-transform: uppercase;
        }

        .nested {
            padding-left: var(--uui-size-space-5);
        }
    `;
}

export default GodModeRootMenuItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-root-menu-item": GodModeRootMenuItemElement;
    }
}
