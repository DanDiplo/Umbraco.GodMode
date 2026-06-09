import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";
import { openWithModalFeedback } from "./modal-feedback";

export const GODMODE_EVIDENCE_DRAWER_ALIAS = "Diplo.Modal.GodMode.Evidence";

export interface GodModeEvidenceSection {
    heading: string;
    description?: string;
    visual?: "database-relationship-graph";
    items: unknown;
}

export interface GodModeEvidenceDrawerData {
    title: string;
    subtitle?: string;
    summary?: Array<{ label: string; value: unknown }>;
    sections: GodModeEvidenceSection[];
    load?: () => Promise<GodModeEvidenceDrawerData>;
}

export function openEvidenceDrawer(host: UmbControllerHost, data: GodModeEvidenceDrawerData, e?: Event): void {
    void openWithModalFeedback(e, () => {
        void umbOpenModal(host, GODMODE_EVIDENCE_DRAWER_ALIAS, { data }).catch(() => undefined);
    });
}

export function openLazyEvidenceDrawer(host: UmbControllerHost, data: GodModeEvidenceDrawerData, e?: Event): void {
    openEvidenceDrawer(host, data, e);
}
