import { browserManifests, type BrowserDef } from "./browsers";
import { menuManifests } from "./menu";
import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import { GODMODE_USED_BY_MODAL_ALIAS } from "../shared/used-by-modal";

/**
 * The full set of browsers GodMode exposes. Each entry creates a menuItem +
 * workspace + workspaceView via {@link browserManifests}. Add a new browser
 * here and the manifest plumbing happens automatically.
 *
 * Reflection sub-pages all use the shared <godmode-reflection-browser>
 * element wrapped in a tiny per-page custom element from `reflection-pages`.
 */
export const browsers: BrowserDef[] = [
    {
        id: "intro",
        label: "Welcome",
        icon: "icon-science",
        description: "Start here and jump to any GodMode tool",
        weight: 1000,
        element: () => import("../elements/godmode-intro.element")
    },
    {
        id: "docTypeBrowser",
        label: "DocType Browser",
        icon: "icon-item-arrangement",
        description: "Browse, filter and search document types and see where they are used",
        weight: 990,
        element: () => import("../elements/godmode-doc-type-browser.element")
    },
    {
        id: "templateBrowser",
        label: "Template Browser",
        icon: "icon-newspaper-alt",
        description: "Filter, browse and search the template hierarchy and see what partials they use",
        weight: 980,
        element: () => import("../elements/godmode-template-browser.element")
    },
    {
        id: "partialBrowser",
        label: "Partial Browser",
        icon: "icon-article",
        description: "Browse partial views and see whether they are cached",
        weight: 970,
        element: () => import("../elements/godmode-partial-browser.element")
    },
    {
        id: "dataTypeBrowser",
        label: "DataType Browser",
        icon: "icon-autofill",
        description: "Browse data types, see whether they are used and by which editor",
        weight: 960,
        element: () => import("../elements/godmode-data-type-browser.element")
    },
    {
        id: "contentBrowser",
        label: "Content Browser",
        icon: "icon-umb-content",
        description: "Browse, search and filter all your content pages",
        weight: 950,
        element: () => import("../elements/godmode-content-browser.element")
    },
    {
        id: "mediaBrowser",
        label: "Media Browser",
        icon: "icon-picture",
        description: "Search your media and filter by type",
        weight: 940,
        element: () => import("../elements/godmode-media-browser.element")
    },
    {
        id: "memberBrowser",
        label: "Member Browser",
        icon: "icon-umb-members",
        description: "Search members and see what groups they have been assigned to",
        weight: 930,
        element: () => import("../elements/godmode-member-browser.element")
    },
    {
        id: "tagBrowser",
        label: "Tag Browser",
        icon: "icon-tags",
        description: "View all tags and see what content they are assigned to",
        weight: 920,
        element: () => import("../elements/godmode-tag-browser.element")
    },
    {
        id: "usageBrowser",
        label: "Usage Browser",
        icon: "icon-chart-curve",
        description: "See how your content types are used and how many instances have been made",
        weight: 910,
        element: () => import("../elements/godmode-usage-browser.element")
    },
    {
        id: "referenceGraph",
        label: "Reference Graph",
        icon: "icon-link",
        description: "Trace relationships between types, templates, data types and block elements",
        weight: 905,
        element: () => import("../elements/godmode-reference-graph.element")
    },
    {
        id: "healthRisk",
        label: "Health & Risk",
        icon: "icon-alert",
        description: "Surface schema drift, broken references and cleanup opportunities",
        weight: 902,
        element: () => import("../elements/godmode-health-risk-browser.element")
    },
    {
        id: "configurationDrift",
        label: "Configuration Drift",
        icon: "icon-merge",
        description: "Compare similar data types, content models and property aliases for suspicious drift",
        weight: 901.5,
        element: () => import("../elements/godmode-configuration-drift.element")
    },
    {
        id: "extensionExplorer",
        label: "Extension Explorer",
        icon: "icon-code",
        description: "Inspect registered backoffice extension manifests, conditions, weights and sources",
        weight: 901,
        element: () => import("../elements/godmode-extension-explorer.element")
    },
    {
        id: "typesIntro",
        label: "Types",
        icon: "icon-folder",
        description: "See how controllers, composers and models are made up and browse interfaces",
        weight: 800,
        element: () => import("../elements/godmode-types-intro.element"),
        customMenuItemElement: () => import("../elements/godmode-types-menu-item.element")
    },
    {
        id: "surfaceControllers",
        label: "Surface Controllers",
        icon: "icon-planet",
        weight: 790,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-surface-controllers")! };
        }
    },
    {
        id: "apiControllers",
        label: "API Controllers",
        icon: "icon-rocket",
        weight: 785,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-api-controllers")! };
        }
    },
    {
        id: "renderControllers",
        label: "Render Controllers",
        icon: "icon-satellite-dish",
        weight: 780,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-render-controllers")! };
        }
    },
    {
        id: "publishedContentModels",
        label: "Content Models",
        icon: "icon-binarycode",
        weight: 775,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-published-content-models")! };
        }
    },
    {
        id: "composers",
        label: "Composers",
        icon: "icon-music",
        weight: 770,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-composers")! };
        }
    },
    {
        id: "valueConverters",
        label: "Value Converters",
        icon: "icon-wand",
        weight: 765,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-value-converters")! };
        }
    },
    {
        id: "viewComponents",
        label: "View Components",
        icon: "icon-code",
        weight: 760,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-view-components")! };
        }
    },
    {
        id: "tagHelpers",
        label: "Tag Helpers",
        icon: "icon-tags",
        weight: 755,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-tag-helpers")! };
        }
    },
    {
        id: "contentFinders",
        label: "Content Finders",
        icon: "icon-directions-alt",
        weight: 750,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-content-finders")! };
        }
    },
    {
        id: "urlProviders",
        label: "URL Providers",
        icon: "icon-link",
        weight: 745,
        skipMenuItem: true,
        element: async () => {
            await import("../elements/reflection-pages");
            return { default: customElements.get("godmode-url-providers")! };
        }
    },
    {
        id: "typeBrowser",
        label: "Interface Browser",
        icon: "icon-molecular-network",
        weight: 740,
        skipMenuItem: true,
        element: () => import("../elements/godmode-type-browser.element")
    },
    {
        id: "serviceBrowser",
        label: "Services",
        icon: "icon-console",
        description: "Browse injected services registered with the IOC container.",
        weight: 700,
        element: () => import("../elements/godmode-service-browser.element")
    },
    {
        id: "diagnosticBrowser",
        label: "Diagnostics",
        icon: "icon-settings",
        description: "View Umbraco settings and configuration, Server settings and much more...",
        weight: 600,
        element: () => import("../elements/godmode-diagnostic-browser.element")
    },
    {
        id: "keyValueBrowser",
        label: "Key Values",
        icon: "icon-key",
        description: "Edit and delete rows in the umbracoKeyValue table",
        weight: 550,
        element: () => import("../elements/godmode-key-value-browser.element")
    },
    {
        id: "utilityBrowser",
        label: "Utilities",
        icon: "icon-wrench",
        description: "Clear caches, restart application pool and warm-up your little templates",
        weight: 500,
        element: () => import("../elements/godmode-utility-browser.element")
    }
];

export const allManifests: ManifestBase[] = [
    ...menuManifests,
    {
        type: "modal",
        alias: GODMODE_USED_BY_MODAL_ALIAS,
        name: "GodMode Used By Modal",
        element: () => import("../elements/godmode-used-by-modal.element")
    } as ManifestBase,
    ...browsers.flatMap(browserManifests)
];
