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
    loadKey?: string;
}

export type GodModeEvidenceLoader = () => Promise<GodModeEvidenceDrawerData>;
export type GodModeLazyEvidenceDrawerData = GodModeEvidenceDrawerData & { load?: GodModeEvidenceLoader };

const loaders = new Map<string, GodModeEvidenceLoader>();

export function takeEvidenceLoader(key: string): GodModeEvidenceLoader | undefined {
    const loader = loaders.get(key);
    loaders.delete(key);
    return loader;
}

export function openEvidenceDrawer(host: UmbControllerHost, data: GodModeEvidenceDrawerData, e?: Event): void {
    void openWithModalFeedback(e, () => {
        void umbOpenModal(host, GODMODE_EVIDENCE_DRAWER_ALIAS, { data }).catch(() => undefined);
    });
}

export function openLazyEvidenceDrawer(host: UmbControllerHost, data: GodModeLazyEvidenceDrawerData, e?: Event): void {
    const load = data.load;
    if (!load) {
        openEvidenceDrawer(host, data, e);
        return;
    }

    const loadKey = crypto.randomUUID();
    loaders.set(loadKey, load);

    const { load: _load, ...modalData } = data;
    openEvidenceDrawer(host, { ...modalData, loadKey }, e);
}
