import { GODMODE_AI_EXPLAIN_MODAL_ALIAS } from "./explain-modal";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "modal",
    alias: GODMODE_AI_EXPLAIN_MODAL_ALIAS,
    name: "GodMode AI Explain Modal",
    element: () => import("./explain-modal.element")
  }
];
