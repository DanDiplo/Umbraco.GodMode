import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";

interface ChildEntry {
    id: string;
    label: string;
    icon: string;
    children?: ReadonlyArray<ChildEntry>;
}

const CHILDREN: ReadonlyArray<ChildEntry> = [
    { id: "docTypeBrowser", label: "DocType Browser", icon: "icon-item-arrangement" },
    { id: "templateBrowser", label: "Template Browser", icon: "icon-newspaper-alt" },
    { id: "partialBrowser", label: "Partial Browser", icon: "icon-article" },
    { id: "dataTypeBrowser", label: "DataType Browser", icon: "icon-autofill" },
    { id: "contentBrowser", label: "Content Browser", icon: "icon-umb-content" },
    { id: "usageBrowser", label: "Usage Browser", icon: "icon-chart-curve" },
    { id: "mediaBrowser", label: "Media Browser", icon: "icon-picture" },
    { id: "memberBrowser", label: "Member Browser", icon: "icon-umb-members" },
    { id: "tagBrowser", label: "Tag Browser", icon: "icon-tags" },
    { id: "referenceGraph", label: "Reference Graph", icon: "icon-link" },
    { id: "healthRisk", label: "Health & Risk", icon: "icon-alert" },
    { id: "configurationDrift", label: "Configuration Drift", icon: "icon-merge" },
    { id: "extensionExplorer", label: "Extension Explorer", icon: "icon-code" },
    {
        id: "typesIntro",
        label: "Types",
        icon: "icon-folder",
        children: [
            { id: "surfaceControllers", label: "Surface Controllers", icon: "icon-planet" },
            { id: "apiControllers", label: "API Controllers", icon: "icon-rocket" },
            { id: "renderControllers", label: "Render Controllers", icon: "icon-satellite-dish" },
            { id: "publishedContentModels", label: "Content Models", icon: "icon-binarycode" },
            { id: "composers", label: "Composers", icon: "icon-music" },
            { id: "valueConverters", label: "Value Converters", icon: "icon-wand" },
            { id: "viewComponents", label: "View Components", icon: "icon-code" },
            { id: "tagHelpers", label: "Tag Helpers", icon: "icon-tags" },
            { id: "contentFinders", label: "Content Finders", icon: "icon-directions-alt" },
            { id: "urlProviders", label: "URL Providers", icon: "icon-link" },
            { id: "typeBrowser", label: "Interface Browser", icon: "icon-molecular-network" }
        ]
    },
    { id: "serviceBrowser", label: "Services", icon: "icon-console" },
    { id: "diagnosticBrowser", label: "Diagnostics", icon: "icon-settings" },
    { id: "utilityBrowser", label: "Utilities", icon: "icon-wrench" }
];

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
            ${this._open ? html`<div class="children">${CHILDREN.map((child) => this._renderChild(child))}</div>` : ""}
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
