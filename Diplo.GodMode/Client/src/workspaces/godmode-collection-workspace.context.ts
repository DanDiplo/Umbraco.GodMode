import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbViewContext } from "@umbraco-cms/backoffice/view";
import {
    UMB_WORKSPACE_CONTEXT,
    UmbWorkspaceRouteManager,
    type UmbRoutableWorkspaceContext
} from "@umbraco-cms/backoffice/workspace";

export interface GodModeCollectionWorkspaceArgs {
    workspaceAlias: string;
    entityType: string;
    /** Header title for the given route unique (null = the workspace root). */
    headlineFor: (unique: string | null) => string;
}

/**
 * Minimal routable workspace context for the GodMode collection workspaces.
 * Mirrors UmbDefaultWorkspaceContext (provides UMB_WORKSPACE_CONTEXT plus the
 * entity/view contexts) but is routable so the `edit/:unique` segment can push
 * the folder id onto the entity context — which scopes the collection view, and
 * which a default-kind workspace cannot do.
 */
export abstract class GodModeCollectionWorkspaceContextBase
    extends UmbContextBase
    implements UmbRoutableWorkspaceContext
{
    public readonly workspaceAlias: string;
    public readonly routes = new UmbWorkspaceRouteManager(this);
    public readonly view = new UmbViewContext(this, null);

    #entityType: string;
    #entityContext = new UmbEntityContext(this);

    protected constructor(host: UmbControllerHost, args: GodModeCollectionWorkspaceArgs) {
        super(host, UMB_WORKSPACE_CONTEXT.toString());

        this.workspaceAlias = args.workspaceAlias;
        this.#entityType = args.entityType;
        this.#entityContext.setEntityType(args.entityType);

        const applyRoute = (component: unknown, rawUnique?: string) => {
            const unique = !rawUnique || rawUnique === "null" ? null : rawUnique;
            const headline = args.headlineFor(unique);
            this.#entityContext.setUnique(unique);
            this.view.setTitle(headline);
            (component as { headline: string }).headline = headline;
        };

        this.routes.setRoutes([
            {
                path: "edit/:unique",
                component: () => import("./godmode-collection-workspace.element"),
                setup: (component, info) => applyRoute(component, info.match.params.unique)
            },
            {
                path: "",
                component: () => import("./godmode-collection-workspace.element"),
                setup: (component) => applyRoute(component)
            }
        ]);
    }

    getEntityType() {
        return this.#entityType;
    }

    getUnique() {
        return this.#entityContext.getUnique();
    }
}
