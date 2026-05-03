import { LitElement, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";

interface GodModeMenuItemManifest {
    meta: {
        label?: string;
        icon?: string;
        entityType: string;
    };
    name: string;
}

@customElement("godmode-menu-item")
export class GodModeMenuItemElement extends UmbElementMixin(LitElement) {
    @property({ type: Object, attribute: false }) manifest: GodModeMenuItemManifest | null = null;

    @state() private _pathname?: string;

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

    private _href(): string | undefined {
        if (!this._pathname || !this.manifest) return undefined;
        return `section/${this._pathname}/workspace/${this.manifest.meta.entityType}`;
    }

    override render() {
        if (!this.manifest) return html``;
        return html`
            <umb-menu-item-layout
                .href=${this._href()}
                .iconName=${this.manifest.meta.icon ?? ""}
                .label=${this.manifest.meta.label ?? this.manifest.name}
            ></umb-menu-item-layout>
        `;
    }
}

export default GodModeMenuItemElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-menu-item": GodModeMenuItemElement;
    }
}
