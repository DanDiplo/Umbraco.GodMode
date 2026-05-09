import type { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";
import { manifests } from "./manifests";

export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
  extensionRegistry.registerMany(manifests);
};
