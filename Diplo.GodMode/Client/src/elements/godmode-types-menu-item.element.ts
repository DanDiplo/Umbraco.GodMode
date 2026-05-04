import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";

interface ChildEntry {
    id: string;
    label: string;
    icon: string;
}

const CHILDREN: ReadonlyArray<ChildEntry> = [
    { id: "typesIntro", label: "Overview", icon: "icon-folder" },
    { id: "surfaceControllers", label: "Surface Controllers", icon: "icon-planet" },
    { id: "apiControllers", label: "API Controllers", icon: "icon-rocket" },
    { id: "renderControllers", label: "Render Controllers", icon: "icon-satellite-dish" },
    { id: "publishedContentModels", label: "Content Models", icon: "icon-binarycode" },
    { id: "composers", label: "Composers", icon: "icon-music" },
    { id: "notificationHandlers", label: "Notification Handlers", icon: "icon-bell" },
    { id: "hostedServices", label: "Hosted Services", icon: "icon-server" },
    { id: "middleware", label: "Middleware", icon: "icon-autofill" },
    { id: "valueConverters", label: "Value Converters", icon: "icon-wand" },
    { id: "viewComponents", label: "View Components", icon: "icon-code" },
    { id: "tagHelpers", label: "Tag Helpers", icon: "icon-tags" },
    { id: "contentFinders", label: "Content Finders", icon: "icon-directions-alt" },
    { id: "urlProviders", label: "URL Providers", icon: "icon-link" },
    { id: "typeBrowser", label: "Interface Browser", icon: "icon-molecular-network" }
];

/**
 * Custom menuItem element that renders the "Types" parent plus its 11
 * sub-pages as nested <umb-menu-item-layout> entries — the v17 way to get
 * the v13 "Types" tree-folder UX.
 *
 * Manifest assigns the parent's own entityType (typesIntro) so clicking
 * the parent label still navigates to the overview.
 */
@customElement("godmode-types-menu-item")
export class GodModeTypesMenuItemElement extends UmbElementMixin(LitElement) {
    @property({ type: Object, attribute: false }) manifest: { meta: { label?: string; icon?: string; entityType: string }; name: string } | null = null;

    @state() private _pathname?: string;
    @state() private _open = true;

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

    private _hrefFor(entityType: string): string | undefined {
        if (!this._pathname) return undefined;
        return `section/${this._pathname}/workspace/${entityType}`;
    }

    private _toggle = () => {
        this._open = !this._open;
    };

    override render() {
        if (!this.manifest) return html``;
        const parentLabel = this.manifest.meta.label ?? this.manifest.name;
        return html`
            <umb-menu-item-layout
                .iconName=${this.manifest.meta.icon ?? "icon-folder"}
                .label=${parentLabel}
                ?has-children=${true}
                @click=${this._toggle}
            ></umb-menu-item-layout>
            ${this._open
                ? html`<div class="children">
                      ${CHILDREN.map((c) => {
                          const entityType = `${GODMODE_ENTITY_TYPE_PREFIX}-${c.id}`;
                          return html`<umb-menu-item-layout
                              .href=${this._hrefFor(entityType)}
                              .iconName=${c.icon}
                              .label=${c.label}
                          ></umb-menu-item-layout>`;
                      })}
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
    `;
}

export default GodModeTypesMenuItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-types-menu-item": GodModeTypesMenuItemElement;
    }
}
