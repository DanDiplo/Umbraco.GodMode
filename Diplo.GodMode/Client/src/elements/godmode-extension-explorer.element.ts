import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestBase } from "@umbraco-cms/backoffice/extension-api";
import "../shared";
import { applySort, toggleSort, type SortState } from "../shared/sort";

type ExtensionManifest = ManifestBase & Record<string, unknown>;

interface ExtensionRow {
    alias: string;
    type: string;
    kind: string;
    name: string;
    weight: number | null;
    package: string;
    conditions: string;
    conditionCount: number;
    meta: string;
    manifest: ExtensionManifest;
}

const FEATURE_TYPES = ["section", "menu", "menuItem", "workspace", "workspaceView", "dashboard", "propertyEditorUi", "tree", "sectionSidebarApp"];
const GODMODE_MENU_ALIAS = "Diplo.Menu.GodMode";
const UMB_WORKSPACE_CONDITION_ALIAS = "Umb.Condition.WorkspaceAlias";
const UMBRACO_EXTENSION_TYPES_DOCS_URL = "https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-types";
const UMBRACO_EXTENSION_CONDITIONS_DOCS_URL = "https://docs.umbraco.com/umbraco-cms/customizing/extending-overview/extension-types/condition#built-in-conditions-types";
const EXTENSION_TYPE_REFERENCE: Record<string, string> = {
    appEntryPoint: "Runs JavaScript when an Umbraco app loads, including Login, Installer, Upgrader, and Backoffice. It is long-lived.",
    backofficeEntryPoint: "Runs JavaScript when the backoffice initializes and can register package extensions, custom elements, and lifecycle behavior.",
    blockEditorCustomView: "Defines a custom web component used to render blocks inside a block editor.",
    bundle: "Groups multiple extension manifests so they can be loaded together.",
    condition: "Controls when and where other UI extensions are available.",
    currentUserAction: "Adds an action to the current user view.",
    dashboard: "Adds an informational or functional view to an Umbraco section dashboard.",
    dashboardCollection: "Adds a dashboard-like view to a collection.",
    dynamicRootOrigin: "Adds an origin option for Dynamic Root selection.",
    dynamicRootQueryStep: "Adds a query step for Dynamic Root selection.",
    entityAction: "Adds an operation to an entity action menu, such as actions on content, media, members, or other entities.",
    entityBulkAction: "Adds an operation for selected entities in a collection bulk actions menu.",
    entryPoint: "Deprecated older name for backofficeEntryPoint.",
    fileUploadPreview: "Adds a component for previewing uploaded media files.",
    globalContext: "Provides shared state, data, or functions that can be consumed across the backoffice session.",
    granularUserPermissions: "Defines custom permission behavior for access control in the backoffice.",
    headerApp: "Adds a single-purpose component to the top-level backoffice header.",
    healthCheck: "Adds a check to Umbraco's health check area.",
    icons: "Registers a custom icon set for use in the backoffice and UI components.",
    localization: "Registers translation files and strings for backoffice UI extensions.",
    menu: "Defines a menu container that menu items can be placed inside.",
    menuItem: "Adds a navigation item to a menu, often used with an entity type to open a workspace.",
    mfaLoginProvider: "Adds a backoffice UI for enabling or disabling a two-factor authentication provider.",
    modal: "Registers a dialog or sidebar surface that other components can open.",
    monacoMarkdownEditorAction: "Adds an action to the Monaco Markdown editor toolbar.",
    packageView: "Adds a view shown in the Packages section for package information or management.",
    previewAppProvider: "Provides a preview app option for Save and Preview on documents.",
    propertyAction: "Adds an action to a property action menu.",
    propertyEditorSchema: "Describes a data editor and its configuration from the backend to the UI.",
    propertyEditorUi: "Provides the UI used to render a data editor on content types.",
    propertyValuePreset: "Customizes default property editor values and can use hooks for dynamic behavior.",
    searchProvider: "Provides search results for the backoffice search experience.",
    searchResultItem: "Provides custom rendering for a backoffice search result item.",
    section: "Adds a top-level backoffice navigation section alongside areas such as Content, Media, and Settings.",
    sectionSidebarApp: "Adds a sidebar contribution to a section, commonly to expose a menu.",
    theme: "Registers backoffice theme styles users can select.",
    tiptapExtension: "Adds functionality to the Tiptap rich text editor.",
    tiptapToolbarExtension: "Adds a toolbar control to the Tiptap rich text editor.",
    tree: "Adds a hierarchical node structure, such as a content, media, or custom tree.",
    workspace: "Provides a routed work area for editing, inspecting, or managing an entity type.",
    workspaceView: "Adds a view or tab inside a workspace, scoped by a workspace alias condition."
};
const BUILT_IN_CONDITION_REFERENCE: Record<string, string> = {
    "Umb.Condition.Switch": "Toggles availability on and off based on a configured frequency in seconds.",
    "Umb.Condition.MultipleAppLanguages": "Requires the app to have more than one language.",
    "Umb.Condition.SectionAlias": "Requires the current section alias to match the configured value.",
    "Umb.Condition.MenuAlias": "Requires the current menu alias to match the configured value.",
    "Umb.Condition.WorkspaceAlias": "Requires the current workspace alias to match the configured value.",
    "Umb.Condition.WorkspaceEntityType": "Requires the current workspace to work on the configured entity type, such as document, block, or user.",
    "Umb.Condition.WorkspaceContentTypeAlias": "Requires the current workspace to be based on a content type with the configured alias.",
    "Umb.Condition.WorkspaceContentTypeUnique": "Requires the current workspace to be based on a uniquely matching content type.",
    "Umb.Condition.Workspace.ContentHasProperties": "Requires the content type of the current workspace to have properties.",
    "Umb.Condition.WorkspaceHasCollection": "Requires the current workspace to have a collection.",
    "Umb.Condition.WorkspaceEntityIsNew": "Requires the current workspace data to be new and not yet persisted.",
    "Umb.Condition.EntityIsTrashed": "Requires the current entity to be trashed.",
    "Umb.Condition.EntityIsNotTrashed": "Requires the current entity not to be trashed.",
    "Umb.Condition.SectionUserPermission": "Requires the current user to have permissions for the configured section alias.",
    "Umb.Condition.UserPermission.Document": "Requires the current user to have specific document permissions.",
    "Umb.Condition.CurrentUser.GroupId": "Requires the current user to belong to matching user groups, using match, oneOf, allOf, or noneOf GUID values.",
    "Umb.Condition.CurrentUser.IsAdmin": "Requires the current user to be an administrator."
};

@customElement("godmode-extension-explorer")
export class GodModeExtensionExplorerElement extends UmbElementMixin(LitElement) {
    @state() private _items: ExtensionRow[] = [];
    @state() private _search = "";
    @state() private _type = "";
    @state() private _package = "";
    @state() private _featuredOnly = false;
    @state() private _expanded = new Set<string>();
    @state() private _sort: SortState = { column: "type", reverse: false };

    override connectedCallback(): void {
        super.connectedCallback();
        this.observe(
            umbExtensionsRegistry.extensions,
            (manifests) => {
                this._items = manifests.map((manifest) => this._toRow(manifest as ExtensionManifest));
            },
            "observeExtensionRegistry"
        );
        this._items = umbExtensionsRegistry.getAllExtensions().map((manifest) => this._toRow(manifest as ExtensionManifest));
    }

    private _toRow(manifest: ExtensionManifest): ExtensionRow {
        const meta = this._record(manifest.meta);
        const weight = this._number(meta.weight ?? manifest.weight);
        const conditions = this._conditions(manifest.conditions);

        return {
            alias: String(manifest.alias ?? ""),
            type: String(manifest.type ?? ""),
            kind: String(manifest.kind ?? ""),
            name: String(manifest.name ?? meta.label ?? ""),
            weight,
            package: this._sourcePackage(manifest),
            conditions,
            conditionCount: Array.isArray(manifest.conditions) ? manifest.conditions.length : 0,
            meta: this._summarizeMeta(meta),
            manifest
        };
    }

    private _record(value: unknown): Record<string, unknown> {
        return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : {};
    }

    private _number(value: unknown): number | null {
        return typeof value === "number" ? value : null;
    }

    private _sourcePackage(manifest: ExtensionManifest): string {
        const candidate = manifest.packageName ?? manifest.packageAlias ?? manifest.packageId ?? manifest.package ?? manifest.js;
        if (typeof candidate === "string" && candidate) return candidate;
        if (String(manifest.alias ?? "").startsWith("Diplo.")) return "Diplo.GodMode";
        if (String(manifest.alias ?? "").startsWith("Umb.")) return "Umbraco";
        return "Unknown";
    }

    private _conditions(value: unknown): string {
        if (!Array.isArray(value) || !value.length) return "";
        return value
            .map((condition) => {
                const c = this._record(condition);
                const details = Object.entries(c)
                    .filter(([key]) => key !== "alias")
                    .map(([key, val]) => `${key}: ${this._brief(val)}`)
                    .join(", ");
                return `${c.alias ?? "condition"}${details ? ` (${details})` : ""}`;
            })
            .join("; ");
    }

    private _conditionSummaries(value: unknown) {
        if (!Array.isArray(value)) return [];

        return value.map((condition) => {
            const config = this._record(condition);
            const alias = String(config.alias ?? "");
            return {
                alias,
                description: BUILT_IN_CONDITION_REFERENCE[alias] ?? "Custom or package-provided condition.",
                configuration: Object.fromEntries(Object.entries(config).filter(([key]) => key !== "alias")),
                isBuiltIn: alias in BUILT_IN_CONDITION_REFERENCE
            };
        });
    }

    private _summarizeMeta(meta: Record<string, unknown>): string {
        return Object.entries(meta)
            .filter(([, value]) => value !== undefined && value !== null && typeof value !== "function")
            .slice(0, 8)
            .map(([key, value]) => `${key}: ${this._brief(value)}`)
            .join("; ");
    }

    private _brief(value: unknown): string {
        if (Array.isArray(value)) return `[${value.map((v) => this._brief(v)).join(", ")}]`;
        if (value && typeof value === "object") return JSON.stringify(value);
        return String(value);
    }

    private _filtered(): ExtensionRow[] {
        const q = this._search.trim().toLowerCase();
        const matched = this._items.filter((item) => {
            if (q && !this._searchText(item).includes(q)) return false;
            if (this._type && item.type !== this._type) return false;
            if (this._package && item.package !== this._package) return false;
            if (this._featuredOnly && !FEATURE_TYPES.includes(item.type)) return false;
            return true;
        });
        return applySort(matched as unknown as Array<Record<string, unknown>>, this._sort) as unknown as ExtensionRow[];
    }

    private _searchText(item: ExtensionRow): string {
        return [item.alias, item.type, item.kind, item.name, item.package, item.conditions, item.meta].join(" ").toLowerCase();
    }

    private _typeCounts(): Map<string, number> {
        const counts = new Map<string, number>();
        for (const item of this._items) counts.set(item.type, (counts.get(item.type) ?? 0) + 1);
        return counts;
    }

    private _toggleExpanded(alias: string) {
        const expanded = new Set(this._expanded);
        expanded.has(alias) ? expanded.delete(alias) : expanded.add(alias);
        this._expanded = expanded;
    }

    private _sortBy(column: string) {
        this._sort = toggleSort(this._sort, column);
    }

    private _sortLabel(column: string, label: string): string {
        if (this._sort.column !== column) return label;
        return `${label} ${this._sort.reverse ? "▼" : "▲"}`;
    }

    override render() {
        const types = Array.from(new Set(this._items.map((item) => item.type))).sort();
        const packages = Array.from(new Set(this._items.map((item) => item.package))).sort();
        const typeCounts = this._typeCounts();
        const results = this._filtered();

        return html`
            <godmode-page
                heading="Backoffice Extension Explorer"
                description="Inspect extension manifests registered in the current backoffice runtime."
                show-reload
                @reload=${() => (this._items = umbExtensionsRegistry.getAllExtensions().map((manifest) => this._toRow(manifest as ExtensionManifest)))}
            >
                <div class="summary">
                    <button class=${`summary-card ${this._featuredOnly ? "active" : ""}`} @click=${() => (this._featuredOnly = !this._featuredOnly)}>
                        <strong>${this._items.filter((item) => FEATURE_TYPES.includes(item.type)).length}</strong>
                        <span>Core UI Types</span>
                    </button>
                    ${FEATURE_TYPES.map(
                        (type) => html`
                            <button class=${`summary-card ${this._type === type ? "active" : ""}`} @click=${() => (this._type = this._type === type ? "" : type)}>
                                <strong>${typeCounts.get(type) ?? 0}</strong>
                                <span>${type}</span>
                            </button>
                        `
                    )}
                </div>

                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                placeholder="Alias, type, package, condition, meta"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Type</label>
                            <select @change=${(e: Event) => (this._type = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${types.map((type) => html`<option value=${type}>${type}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Source</label>
                            <select @change=${(e: Event) => (this._package = (e.target as HTMLSelectElement).value)}>
                                <option value="">Any</option>
                                ${packages.map((pkg) => html`<option value=${pkg}>${pkg}</option>`)}
                            </select>
                        </div>
                    </div>
                    <p class="help">
                        Source package is inferred from manifest package fields, aliases and script paths when available. Expand a row to inspect the raw manifest JSON.
                    </p>
                </uui-box>

                <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong></p>
                <div class="manifest-table">
                    <div class="table-head">
                        <button @click=${() => this._sortBy("type")}>${this._sortLabel("type", "Type")}</button>
                        <button @click=${() => this._sortBy("alias")}>${this._sortLabel("alias", "Alias")}</button>
                        <button @click=${() => this._sortBy("name")}>${this._sortLabel("name", "Name")}</button>
                        <button @click=${() => this._sortBy("weight")}>${this._sortLabel("weight", "Weight")}</button>
                        <button @click=${() => this._sortBy("package")}>${this._sortLabel("package", "Source")}</button>
                        <span>Conditions</span>
                    </div>
                    ${results.map((item) => this._renderRow(item))}
                </div>
            </godmode-page>
        `;
    }

    private _renderRow(item: ExtensionRow) {
        return html`
            <button class="manifest-row" @click=${() => this._toggleExpanded(item.alias)}>
                <span><span class="pill">${item.type}</span>${item.kind ? html`<span class="kind">${item.kind}</span>` : ""}</span>
                <code>${item.alias}</code>
                <span>${item.name}</span>
                <span>${item.weight ?? ""}</span>
                <small>${item.package}</small>
                <span>
                    ${item.conditionCount ? html`<span class="condition-count">${item.conditionCount}</span>` : ""}
                    <small>${item.conditions}</small>
                </span>
            </button>
            ${this._expanded.has(item.alias)
                ? html`
                      <div class="details">
                          <div class="detail-actions">
                              <godmode-ai-explain-host .subject=${this._explainSubject(item)}></godmode-ai-explain-host>
                          </div>
                          <dl>
                              <dt>Meta</dt>
                              <dd>${item.meta || "None"}</dd>
                              <dt>Raw manifest</dt>
                              <dd><pre>${JSON.stringify(item.manifest, this._jsonReplacer, 2)}</pre></dd>
                          </dl>
                      </div>
                  `
                : ""}
        `;
    }

    private _explainSubject(item: ExtensionRow) {
        const manifest = item.manifest;
        const meta = this._record(manifest.meta);
        const relationships = this._extensionRelationships(item);
        return {
            subjectType: "Umbraco backoffice extension manifest",
            title: item.alias,
            data: {
                alias: item.alias,
                type: item.type,
                kind: item.kind,
                name: item.name,
                label: meta.label,
                icon: meta.icon,
                pathname: meta.pathname,
                entityType: meta.entityType,
                weight: item.weight,
                sourcePackage: item.package,
                conditions: item.conditions,
                conditionCount: item.conditionCount,
                conditionSummary: this._conditionSummaries(manifest.conditions),
                metaSummary: item.meta,
                likelyPurpose: this._extensionPurpose(item)
            },
            context: {
                relatedExtensions: relationships,
                rawManifest: JSON.parse(JSON.stringify(manifest, this._jsonReplacer)),
                docsReference: this._docsReference(item),
                umbracoExtensionConcepts: this._extensionConcepts(item),
                likelyCoreUiFeature: FEATURE_TYPES.includes(item.type),
                inferredSourcePackage: item.package
            }
        };
    }

    private _extensionRelationships(item: ExtensionRow) {
        const manifest = item.manifest;
        const meta = this._record(manifest.meta);

        if (item.type === "menuItem") {
            const entityType = typeof meta.entityType === "string" ? meta.entityType : "";
            const workspaces = this._items
                .filter((candidate) => candidate.type === "workspace" && this._record(candidate.manifest.meta).entityType === entityType)
                .map((workspace) => this._summarizeRelatedExtension(workspace));
            const workspaceViews = workspaces.flatMap((workspace) => this._workspaceViewsForAlias(workspace.alias));

            return {
                opensEntityType: entityType,
                appearsInMenus: Array.isArray(meta.menus) ? meta.menus : [],
                isGodModeMenuItem: Array.isArray(meta.menus) && meta.menus.includes(GODMODE_MENU_ALIAS),
                relatedWorkspaces: workspaces,
                relatedWorkspaceViews: workspaceViews
            };
        }

        if (item.type === "workspace") {
            const entityType = typeof meta.entityType === "string" ? meta.entityType : "";
            const menuItems = this._items
                .filter((candidate) => candidate.type === "menuItem" && this._record(candidate.manifest.meta).entityType === entityType)
                .map((menuItem) => this._summarizeRelatedExtension(menuItem));

            return {
                hostsEntityType: entityType,
                openedByMenuItems: menuItems,
                workspaceViews: this._workspaceViewsForAlias(item.alias)
            };
        }

        if (item.type === "workspaceView") {
            const workspaceAlias = this._workspaceAliasFromConditions(manifest.conditions);
            const workspace = workspaceAlias ? this._items.find((candidate) => candidate.alias === workspaceAlias) : undefined;

            return {
                belongsToWorkspaceAlias: workspaceAlias,
                belongsToWorkspace: workspace ? this._summarizeRelatedExtension(workspace) : null,
                routePathname: meta.pathname,
                viewLabel: meta.label
            };
        }

        if (item.type === "sectionSidebarApp") {
            return {
                mountsMenuAlias: meta.menu,
                sectionConditions: manifest.conditions,
                exposesGodModeMenu: meta.menu === GODMODE_MENU_ALIAS
            };
        }

        return {};
    }

    private _workspaceViewsForAlias(workspaceAlias: string) {
        return this._items
            .filter((candidate) => candidate.type === "workspaceView" && this._workspaceAliasFromConditions(candidate.manifest.conditions) === workspaceAlias)
            .map((view) => this._summarizeRelatedExtension(view));
    }

    private _workspaceAliasFromConditions(value: unknown): string {
        if (!Array.isArray(value)) return "";
        const condition = value.map((item) => this._record(item)).find((item) => item.alias === UMB_WORKSPACE_CONDITION_ALIAS);
        return typeof condition?.match === "string" ? condition.match : "";
    }

    private _summarizeRelatedExtension(item: ExtensionRow) {
        const meta = this._record(item.manifest.meta);
        return {
            alias: item.alias,
            type: item.type,
            name: item.name,
            label: meta.label,
            pathname: meta.pathname,
            entityType: meta.entityType,
            icon: meta.icon,
            weight: item.weight,
            sourcePackage: item.package
        };
    }

    private _extensionPurpose(item: ExtensionRow) {
        const manifest = item.manifest;
        const meta = this._record(manifest.meta);
        const label = typeof meta.label === "string" ? meta.label : item.name || item.alias;

        switch (item.type) {
            case "menu":
                return `Defines a named menu container that other menu items can appear inside.`;
            case "sectionSidebarApp":
                return `Mounts a sidebar app, usually to expose a menu in one or more Umbraco sections.`;
            case "menuItem":
                return `Adds the "${label}" navigation item. If it has an entityType, selecting it opens the matching workspace.`;
            case "workspace":
                return `Provides the editing or inspection workspace for entity type "${meta.entityType ?? "unknown"}".`;
            case "workspaceView":
                return `Adds the "${label}" view/tab to a workspace, usually at pathname "${meta.pathname ?? "overview"}".`;
            case "modal":
                return `Registers a modal dialog that can be opened by other backoffice components.`;
            case "backofficeEntryPoint":
                return `Loads a package JavaScript entry point so its extensions and custom elements can register.`;
            default:
                return FEATURE_TYPES.includes(item.type)
                    ? `Registers a visible backoffice UI extension of type "${item.type}".`
                    : `Registers an Umbraco extension of type "${item.type}".`;
        }
    }

    private _docsReference(item: ExtensionRow) {
        return {
            sourceUrl: UMBRACO_EXTENSION_TYPES_DOCS_URL,
            conditionSourceUrl: UMBRACO_EXTENSION_CONDITIONS_DOCS_URL,
            currentTypeDescription: EXTENSION_TYPE_REFERENCE[item.type] ?? "",
            conditionDescriptions: this._conditionSummaries(item.manifest.conditions),
            relatedTypeDescriptions: this._relatedTypeDescriptions(item)
        };
    }

    private _relatedTypeDescriptions(item: ExtensionRow) {
        const relatedTypes = new Set<string>([item.type]);

        if (item.type === "menuItem") {
            relatedTypes.add("menu");
            relatedTypes.add("workspace");
            relatedTypes.add("workspaceView");
        }

        if (item.type === "workspace") {
            relatedTypes.add("menuItem");
            relatedTypes.add("workspaceView");
        }

        if (item.type === "workspaceView") {
            relatedTypes.add("workspace");
            relatedTypes.add("condition");
        }

        if (item.type === "sectionSidebarApp") {
            relatedTypes.add("section");
            relatedTypes.add("menu");
            relatedTypes.add("menuItem");
            relatedTypes.add("condition");
        }

        if (item.type === "propertyEditorUi") {
            relatedTypes.add("propertyEditorSchema");
            relatedTypes.add("propertyValuePreset");
        }

        if (item.type === "backofficeEntryPoint") {
            relatedTypes.add("bundle");
            relatedTypes.add("appEntryPoint");
        }

        return Array.from(relatedTypes)
            .filter((type) => EXTENSION_TYPE_REFERENCE[type])
            .map((type) => ({
                type,
                description: EXTENSION_TYPE_REFERENCE[type]
            }));
    }

    private _extensionConcepts(item: ExtensionRow) {
        return {
            menuItem: "A navigation item. When meta.entityType matches a workspace meta.entityType, clicking it usually opens that workspace.",
            workspace: "A routed backoffice surface for a specific entity type.",
            workspaceView: "A view or tab inside a workspace. Its WorkspaceAlias condition determines which workspace hosts it.",
            sectionSidebarApp: "A sidebar contribution mounted into an Umbraco section, often used to show a menu.",
            currentExtensionType: item.type
        };
    }

    private _jsonReplacer(_key: string, value: unknown) {
        return typeof value === "function" ? "[Function]" : value;
    }

    static override styles = css`
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
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
            font-size: 1.3rem;
        }
        .filters {
            display: grid;
            grid-template-columns: 2fr 1fr 1fr;
            gap: var(--uui-size-space-4);
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input,
        select {
            width: 100%;
        }
        select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .help,
        .results,
        small {
            color: var(--uui-color-text-alt);
        }
        .help {
            margin: var(--uui-size-space-4) 0 0;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
        }
        .manifest-table {
            display: grid;
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            overflow: hidden;
        }
        .table-head,
        .manifest-row {
            display: grid;
            grid-template-columns: minmax(190px, 1.1fr) minmax(260px, 1.6fr) minmax(180px, 1fr) minmax(80px, 0.45fr) minmax(140px, 0.8fr) minmax(220px, 1.4fr);
            align-items: start;
            column-gap: var(--uui-size-space-4);
            padding: var(--uui-size-space-3);
        }
        .table-head {
            font-weight: 700;
            background: var(--uui-color-surface-alt);
            border-bottom: 1px solid var(--uui-color-border);
        }
        .table-head button,
        .manifest-row {
            border: 0;
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
            font: inherit;
            text-align: left;
        }
        .table-head button {
            cursor: pointer;
            font-weight: 700;
            padding: 0;
            background: transparent;
        }
        .manifest-row {
            cursor: pointer;
            border-bottom: 1px solid var(--uui-color-border);
            width: 100%;
            box-shadow: inset 0 1px 0 color-mix(in srgb, var(--uui-color-surface) 72%, var(--uui-color-border));
            transition: background-color 120ms ease;
        }
        .manifest-row:nth-of-type(odd) {
            background: color-mix(in srgb, var(--uui-color-surface-alt) 38%, var(--uui-color-surface));
        }
        .manifest-row:hover {
            background: color-mix(in srgb, var(--uui-color-interactive) 7%, var(--uui-color-surface-alt));
        }
        .pill,
        .kind,
        .condition-count {
            display: inline-flex;
            border-radius: 3px;
            padding: 1px var(--uui-size-space-1);
            font-size: 0.75rem;
            font-weight: 600;
        }
        .pill {
            background: var(--uui-color-text);
            color: var(--uui-color-surface);
        }
        .kind,
        .condition-count {
            margin-left: var(--uui-size-space-1);
            background: var(--uui-color-border);
            color: var(--uui-color-text);
        }
        .details {
            grid-column: 1 / -1;
            background: var(--uui-color-surface);
            border-bottom: 1px solid var(--uui-color-border);
            padding: var(--uui-size-space-4);
        }
        .detail-actions {
            display: flex;
            justify-content: flex-end;
            margin-bottom: var(--uui-size-space-3);
        }
        .detail-actions godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
        dl {
            display: grid;
            grid-template-columns: 120px minmax(0, 1fr);
            gap: var(--uui-size-space-3);
            margin: 0;
            padding: var(--uui-size-space-4);
            background: var(--uui-color-surface-alt);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }
        dt {
            font-weight: 700;
            color: var(--uui-color-text-alt);
        }
        dd {
            margin: 0;
            min-width: 0;
            overflow-wrap: anywhere;
        }
        pre {
            max-height: 420px;
            overflow: auto;
            margin: 0;
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
        }
        @media (max-width: 900px) {
            .filters {
                grid-template-columns: 1fr;
            }
            .table-head {
                display: none;
            }
            .manifest-row {
                grid-template-columns: 1fr;
                row-gap: var(--uui-size-space-2);
            }
        }
    `;
}

export default GodModeExtensionExplorerElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-extension-explorer": GodModeExtensionExplorerElement;
    }
}
