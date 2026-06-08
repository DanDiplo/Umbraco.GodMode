import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { GodModeAiExplainSubject } from "./api";
import { openWithModalFeedback } from "./modal-feedback";

export const GODMODE_AI_EXPLAIN_MODAL_ALIAS = "Diplo.Modal.GodModeAI.Explain";

export interface GodModeAiExplainModalData {
  subject: GodModeAiExplainSubject;
}

export function openExplainModal(host: UmbControllerHost, subject: GodModeAiExplainSubject, e?: Event): void {
  void openWithModalFeedback(e, () => {
    void umbOpenModal(host, GODMODE_AI_EXPLAIN_MODAL_ALIAS, { data: { subject } }).catch(() => undefined);
  });
}
