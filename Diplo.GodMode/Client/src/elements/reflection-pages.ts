import { LitElement, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import "./godmode-reflection-browser.element";

/**
 * Thin wrappers around <godmode-reflection-browser> — one per reflection
 * sub-section. Each wrapper is the lazy-imported entry point referenced by
 * the catalog, so the manifest layer doesn't have to know about props.
 *
 * They MUST extend UmbElementMixin(LitElement) so that v17's default
 * workspace context (which calls `host.addUmbController(...)` on element
 * mount) can attach itself.
 */

interface Spec {
    tag: string;
    endpoint: string;
    heading: string;
    description: string;
}

const SPECS: Spec[] = [
    {
        tag: "godmode-surface-controllers",
        endpoint: "reflection/surface-controllers",
        heading: "Surface Controllers",
        description: "Browse Umbraco Surface Controllers."
    },
    {
        tag: "godmode-api-controllers",
        endpoint: "reflection/api-controllers",
        heading: "API Controllers",
        description: "Browse Umbraco API Controllers."
    },
    {
        tag: "godmode-render-controllers",
        endpoint: "reflection/render-controllers",
        heading: "Render Controllers",
        description: "Browse Umbraco Render MVC Controllers."
    },
    {
        tag: "godmode-published-content-models",
        endpoint: "reflection/published-content-models",
        heading: "Content Models",
        description: "Generated published content and element model types."
    },
    {
        tag: "godmode-composers",
        endpoint: "reflection/composers",
        heading: "Composers",
        description: "Umbraco IComposer implementations."
    },
    {
        tag: "godmode-value-converters",
        endpoint: "reflection/property-value-converters",
        heading: "Property Value Converters",
        description: "Registered IPropertyValueConverter implementations."
    },
    {
        tag: "godmode-view-components",
        endpoint: "reflection/view-components",
        heading: "View Components",
        description: "All registered View Components."
    },
    {
        tag: "godmode-tag-helpers",
        endpoint: "reflection/tag-helpers",
        heading: "Tag Helpers",
        description: "All registered ITagHelper implementations."
    },
    {
        tag: "godmode-content-finders",
        endpoint: "reflection/content-finders",
        heading: "Content Finders",
        description: "Registered IContentFinder implementations."
    },
    {
        tag: "godmode-url-providers",
        endpoint: "reflection/url-providers",
        heading: "URL Providers",
        description: "Registered IUrlProvider implementations."
    }
];

for (const spec of SPECS) {
    @customElement(spec.tag)
    class WrapperElement extends UmbElementMixin(LitElement) {
        override render() {
            return html`<godmode-reflection-browser
                endpoint=${spec.endpoint}
                heading=${spec.heading}
                description=${spec.description}
            ></godmode-reflection-browser>`;
        }
    }
    // Side-effect registration only — no need to export.
    void WrapperElement;
}
