import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbOpenModal } from "@umbraco-cms/backoffice/modal";

export const GODMODE_EVIDENCE_DRAWER_ALIAS = "Diplo.Modal.GodMode.Evidence";

export interface GodModeEvidenceSection {
    heading: string;
    description?: string;
    items: unknown;
}

export interface GodModeEvidenceDrawerData {
    title: string;
    subtitle?: string;
    summary?: Array<{ label: string; value: unknown }>;
    sections: GodModeEvidenceSection[];
}

export function openEvidenceDrawer(host: UmbControllerHost, data: GodModeEvidenceDrawerData, e?: Event): void {
    e?.preventDefault();
    e?.stopPropagation();

    void umbOpenModal(host, GODMODE_EVIDENCE_DRAWER_ALIAS, { data }).catch(() => undefined);
}
