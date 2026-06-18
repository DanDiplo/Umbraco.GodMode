import type { UmbTreeItemModel, UmbTreeRootModel } from "@umbraco-cms/backoffice/tree";

export interface GodModeTreeItemModel extends UmbTreeItemModel {
    /** Leaves only: the existing standalone browser workspace entityType. */
    workspaceEntityType?: string;
}

export type GodModeTreeRootModel = UmbTreeRootModel;
