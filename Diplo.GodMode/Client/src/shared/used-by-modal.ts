import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { openWithModalFeedback } from "./modal-feedback";

export const GODMODE_USED_BY_MODAL_ALIAS = "Diplo.Modal.GodMode.UsedBy";

export interface GodModeUsedByModalData {
    targetType: string;
    targetKey: string;
    targetName?: string;
    targetAlias?: string;
}

export function openUsedByModal(host: UmbControllerHost, data: GodModeUsedByModalData, e?: Event): void {
    void openWithModalFeedback(e, () => {
        void umbOpenModal(host, GODMODE_USED_BY_MODAL_ALIAS, { data }).catch(() => undefined);
    });
}
