import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import { UMB_ENTITY_CONTEXT } from "@umbraco-cms/backoffice/entity";
import type { UmbCollectionFilterModel } from "@umbraco-cms/backoffice/collection";
import { GodModeTreeRepository } from "../tree/godmode-tree.repository";

/**
 * Collection repository that lists the children of the current tree node. The
 * parent comes from the workspace's entity context: the root workspace has no
 * unique (so we return the top-level folders), a folder workspace sets its
 * unique (so we return that folder's children).
 */
export class GodModeTreeItemChildrenCollectionRepository extends UmbRepositoryBase {
    #treeRepository = new GodModeTreeRepository(this);

    constructor(host: UmbControllerHost) {
        super(host);
    }

    async requestCollection(filter: UmbCollectionFilterModel) {
        const entityContext = await this.getContext(UMB_ENTITY_CONTEXT);
        if (!entityContext) throw new Error("Entity context not found");

        const entityType = entityContext.getEntityType();
        const unique = entityContext.getUnique();
        const skip = filter.skip ?? 0;
        const take = filter.take ?? 100;

        // Treat an absent/null unique as the tree root (lists the folders);
        // a real unique lists that folder's children.
        const isRoot = !entityType || unique === undefined || unique === null || unique === "null";
        if (isRoot) {
            return this.#treeRepository.requestTreeRootItems({ skip, take });
        }

        return this.#treeRepository.requestTreeItemsOf({ parent: { entityType, unique }, skip, take });
    }
}

export { GodModeTreeItemChildrenCollectionRepository as api };
