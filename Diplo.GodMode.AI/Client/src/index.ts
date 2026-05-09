import type { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";
import { manifests } from "./manifests";
import "./explain-button.element";

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
  extensionRegistry.registerMany(manifests);
};
