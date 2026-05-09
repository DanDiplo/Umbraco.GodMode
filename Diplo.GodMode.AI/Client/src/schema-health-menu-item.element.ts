import { LitElement, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";

@customElement("godmode-ai-schema-health-menu-item")
export class GodModeAiSchemaHealthMenuItemElement extends UmbElementMixin(LitElement) {
  @property({ type: Object, attribute: false }) manifest?: any;

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

  override render() {
    if (!this.manifest) return html``;
    const href = this._pathname ? `section/${this._pathname}/workspace/${this.manifest.meta.entityType}` : undefined;

    return html`
      <umb-menu-item-layout
        .href=${href}
        .iconName=${this.manifest.meta.icon ?? "icon-wand"}
        .label=${this.manifest.meta.label ?? this.manifest.name}
      ></umb-menu-item-layout>
    `;
  }
}

export default GodModeAiSchemaHealthMenuItemElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-schema-health-menu-item": GodModeAiSchemaHealthMenuItemElement;
  }
}
