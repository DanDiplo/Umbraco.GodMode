import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { openWithModalFeedback } from "./modal-feedback";

export const GODMODE_ELEMENT_TYPE_USAGE_MODAL_ALIAS = "Diplo.Modal.GodMode.ElementTypeUsage";

export interface GodModeElementTypeUsageModalData {
    elementTypeKey: string;
    elementTypeName?: string;
    elementTypeAlias?: string;
}

export function openElementTypeUsageModal(host: UmbControllerHost, data: GodModeElementTypeUsageModalData, e?: Event): void {
    void openWithModalFeedback(e, () => {
        void umbOpenModal(host, GODMODE_ELEMENT_TYPE_USAGE_MODAL_ALIAS, { data }).catch(() => undefined);
    });
}
