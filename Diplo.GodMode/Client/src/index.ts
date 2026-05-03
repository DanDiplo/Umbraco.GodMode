import type { UmbEntryPointOnInit } from "@umbraco-cms/backoffice/extension-api";
import { allManifests } from "./manifests/catalog";

// Entry point — invoked by the backoffice when the package is loaded.
// Manifests are registered programmatically (rather than via a fat JSON array
// in umbraco-package.json) so we get full TypeScript validation.
export const onInit: UmbEntryPointOnInit = (_host, extensionRegistry) => {
    extensionRegistry.registerMany(allManifests);
};
