import { UMB_EDIT_DATA_TYPE_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/data-type";
import { UMB_DOCUMENT_ENTITY_TYPE, UMB_EDIT_DOCUMENT_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/document";
import { UMB_DOCUMENT_TYPE_ENTITY_TYPE, UMB_EDIT_DOCUMENT_TYPE_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/document-type";
import { UMB_EDIT_MEDIA_WORKSPACE_PATH_PATTERN, UMB_MEDIA_ENTITY_TYPE } from "@umbraco-cms/backoffice/media";
import { UmbModalRouteRegistrationController } from "@umbraco-cms/backoffice/router";
import { UMB_TEMPLATE_ENTITY_TYPE } from "@umbraco-cms/backoffice/template";
import { UMB_WORKSPACE_EDIT_PATH_PATTERN, UMB_WORKSPACE_MODAL } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { GodModeAffectedEntity } from "./api";

const EDIT_URLS = {
  template: "section/settings/workspace/template/edit/",
  dataType: "section/settings/workspace/data-type/edit/",
  documentType: "section/settings/workspace/document-type/edit/",
  media: "section/media/workspace/media/edit/",
  content: "section/content/workspace/document/edit/"
} as const;

export type EditableEntityType = keyof typeof EDIT_URLS;

export interface EntityEditLink {
  entityType: EditableEntityType;
  unique: string;
  href: string;
}

export function editLinkForAffectedEntity(entity: GodModeAffectedEntity): EntityEditLink | undefined {
  const entityType = toEditableEntityType(entity.entityType);
  const unique = normaliseUnique(entity.key);

  if (!entityType || !unique) return undefined;

  return {
    entityType,
    unique,
    href: editUrl(entityType, unique)
  };
}

export function openEditorModal(host: UmbControllerHost, link: EntityEditLink, e?: Event): void {
  e?.preventDefault();
  e?.stopPropagation();

  const workspaceType = workspaceEntityType(link.entityType);

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
      const workspacePath = editWorkspacePath(link.entityType, link.unique);

      history.pushState(null, "", `${modalPath}/${workspacePath}`);
      window.dispatchEvent(new PopStateEvent("popstate"));
    });
}

function editUrl(entityType: EditableEntityType, unique: string): string {
  const url = `${EDIT_URLS[entityType]}${unique}`;
  return entityType === "media" ? `${url}/invariant` : url;
}

function toEditableEntityType(entityType: string): EditableEntityType | undefined {
  switch (entityType.trim().toLowerCase()) {
    case "content":
    case "content item":
    case "document":
    case "document item":
      return "content";
    case "document type":
    case "document-type":
    case "doctype":
    case "element type":
    case "element-type":
    case "content type":
    case "content or media type":
      return "documentType";
    case "data type":
    case "data-type":
    case "datatype":
      return "dataType";
    case "template":
      return "template";
    case "media":
    case "media item":
      return "media";
    default:
      return undefined;
  }
}

function normaliseUnique(unique: string | null | undefined): string {
  const value = unique?.trim() ?? "";
  return value && value.toLowerCase() !== "null" ? value : "";
}

function workspaceEntityType(entityType: EditableEntityType): string {
  switch (entityType) {
    case "content":
      return UMB_DOCUMENT_ENTITY_TYPE;
    case "documentType":
      return UMB_DOCUMENT_TYPE_ENTITY_TYPE;
    case "media":
      return UMB_MEDIA_ENTITY_TYPE;
    case "template":
      return UMB_TEMPLATE_ENTITY_TYPE;
    case "dataType":
      return "data-type";
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
    case "template":
      return UMB_WORKSPACE_EDIT_PATH_PATTERN.generateLocal({ unique });
  }
}
