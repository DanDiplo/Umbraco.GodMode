import { UMB_EDIT_DATA_TYPE_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/data-type";
import { UMB_DOCUMENT_ENTITY_TYPE, UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/document";
import { UMB_DOCUMENT_TYPE_ENTITY_TYPE, UMB_EDIT_DOCUMENT_TYPE_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/document-type";
import { UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN, UMB_MEDIA_ENTITY_TYPE } from "@umbraco-cms/backoffice/media";
import { UMB_EDIT_MEMBER_WORKSPACE_PATH_PATTERN, UMB_MEMBER_ENTITY_TYPE } from "@umbraco-cms/backoffice/member";
import { UmbModalRouteRegistrationController } from "@umbraco-cms/backoffice/router";
import { UMB_TEMPLATE_ENTITY_TYPE } from "@umbraco-cms/backoffice/template";
import { UMB_WORKSPACE_EDIT_PATH_PATTERN, UMB_WORKSPACE_MODAL } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { EDIT_URLS } from "../constants";
import { openWithModalFeedback } from "./modal-feedback";

export type EditableEntityType = keyof typeof EDIT_URLS;

export function editUrl(entityType: EditableEntityType, unique: string | null | undefined): string {
    if (!unique) return "";
    const url = `${EDIT_URLS[entityType]}${unique}`;
    return entityType === "media" ? `${url}/invariant` : url;
}

export function openEditorModal(host: UmbControllerHost, entityType: EditableEntityType, unique: string | null | undefined, e?: Event): void {
    if (!unique) return;

    void openWithModalFeedback(e, () => {
        const workspaceType = workspaceEntityType(entityType);

        new UmbModalRouteRegistrationController(host, UMB_WORKSPACE_MODAL)
            .addAdditionalPath(workspaceType)
            .onSetup(() => ({
                data: {
                    entityType: workspaceType,
                    preset: {}
                }
            }))
            .onSubmit(() => undefined)
            .onReject(() => undefined)
            .observeRouteBuilder((routeBuilder) => {
                const modalPath = routeBuilder({});
                const workspacePath = editWorkspacePath(entityType, unique);

                history.pushState(null, "", `${modalPath}/${workspacePath}`);
                window.dispatchEvent(new PopStateEvent("popstate"));
            });
    });
}

function workspaceEntityType(entityType: EditableEntityType): string {
    switch (entityType) {
        case "content":
            return UMB_DOCUMENT_ENTITY_TYPE;
        case "documentType":
            return UMB_DOCUMENT_TYPE_ENTITY_TYPE;
        case "media":
            return UMB_MEDIA_ENTITY_TYPE;
        case "member":
            return UMB_MEMBER_ENTITY_TYPE;
        case "template":
            return UMB_TEMPLATE_ENTITY_TYPE;
        case "dataType":
        case "partial":
            return entityType === "dataType" ? "data-type" : "partial-view";
    }
}

function editWorkspacePath(entityType: EditableEntityType, unique: string): string {
    switch (entityType) {
        case "content":
            return UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN.generateLocal({ unique });
        case "documentType":
            return UMB_EDIT_DOCUMENT_TYPE_WORKSPACE_PATH_PATTERN.generateLocal({ unique });
        case "dataType":
            return UMB_EDIT_DATA_TYPE_WORKSPACE_PATH_PATTERN.generateLocal({ unique });
        case "media":
            return `${UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN.generateLocal({ unique })}/invariant`;
        case "member":
            return UMB_EDIT_MEMBER_WORKSPACE_PATH_PATTERN.generateLocal({ unique });
        case "template":
        case "partial":
            return UMB_WORKSPACE_EDIT_PATH_PATTERN.generateLocal({ unique });
    }
}
