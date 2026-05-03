import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";
import "../shared";

const PAGES = [
    { name: "Surface Controllers", id: "surfaceControllers", desc: "Browse Umbraco Surface Controllers" },
    { name: "API Controllers", id: "apiControllers", desc: "Browse Umbraco Web API Controllers" },
    { name: "Render Controllers", id: "renderControllers", desc: "Browse Umbraco Render MVC Controllers" },
    { name: "Content Models", id: "publishedContentModels", desc: "List generated published model types" },
    { name: "Composers", id: "composers", desc: "Browse Umbraco Composers (DI bootstrappers)" },
    { name: "Value Converters", id: "valueConverters", desc: "Configured Property Value Converters" },
    { name: "View Components", id: "viewComponents", desc: "All View Components used in your site" },
    { name: "Tag Helpers", id: "tagHelpers", desc: "Available Tag Helpers" },
    { name: "Content Finders", id: "contentFinders", desc: "Registered IContentFinder implementations" },
    { name: "URL Providers", id: "urlProviders", desc: "Registered IUrlProvider implementations" },
    { name: "Interface Browser", id: "typeBrowser", desc: "Interrogate C# interfaces and derived types" }
];

@customElement("godmode-types-intro")
export class GodModeTypesIntroElement extends UmbElementMixin(LitElement) {
    @state() private _pathname?: string;

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

    private _hrefFor(id: string): string {
        return this._pathname ? `section/${this._pathname}/workspace/${GODMODE_ENTITY_TYPE_PREFIX}-${id}` : "";
    }

    override render() {
        return html`
            <godmode-page heading="Types" description="Reflection-based explorers for your application's types and Umbraco's plumbing.">
                <uui-box>
                    <uui-table>
                        <uui-table-head>
                            <uui-table-head-cell style="width:30%">Page</uui-table-head-cell>
                            <uui-table-head-cell>Description</uui-table-head-cell>
                        </uui-table-head>
                        ${PAGES.map(
                            (p) => html`
                                <uui-table-row>
                                    <uui-table-cell>
                                        <a href=${this._hrefFor(p.id)}><strong>${p.name}</strong></a>
                                    </uui-table-cell>
                                    <uui-table-cell>${p.desc}</uui-table-cell>
                                </uui-table-row>
                            `
                        )}
                    </uui-table>
                </uui-box>
            </godmode-page>
        `;
    }

    static override styles = css`
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    `;
}

export default GodModeTypesIntroElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-types-intro": GodModeTypesIntroElement;
    }
}
