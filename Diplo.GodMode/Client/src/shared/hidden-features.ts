import { godmodeGet } from "../api/client";

/**
 * Extra names a feature can be hidden by, on top of its browser id and label.
 * Covers names shown only in the tree (e.g. "Overview") and v13-era ids so
 * existing `FeaturesToHide` config keeps working after an upgrade.
 */
const FEATURE_ALIASES: Readonly<Record<string, ReadonlyArray<string>>> = {
    informationBrowser: ["Overview", "Information Browser"],
    typesIntro: ["reflectionTree"]
};

/**
 * Case-insensitive lookup of the `GodMode:FeaturesToHide` setting. A feature
 * is hidden when its id, label or any alias is listed.
 */
export class GodModeHiddenFeatures {
    static readonly none = new GodModeHiddenFeatures([]);

    readonly #hidden: ReadonlySet<string>;

    constructor(featuresToHide: ReadonlyArray<string>) {
        this.#hidden = new Set(featuresToHide.map(normalise).filter(Boolean));
    }

    get isEmpty(): boolean {
        return this.#hidden.size === 0;
    }

    /** True when the feature with this browser id (or any of the given names) is configured as hidden. */
    isHidden(id: string, ...names: ReadonlyArray<string | undefined>): boolean {
        if (this.isEmpty) return false;
        return [id, ...names, ...(FEATURE_ALIASES[id] ?? [])].some((name) => !!name && this.#hidden.has(normalise(name)));
    }
}

interface GodModeFeatureConfigResponse {
    featuresToHide?: string[];
}

let hiddenFeaturesRequest: Promise<GodModeHiddenFeatures> | undefined;

/**
 * Loads `FeaturesToHide` from the GodMode config endpoint once per backoffice
 * session. A failed request resolves to "nothing hidden" and is retried on the
 * next call rather than cached.
 */
export function getGodModeHiddenFeatures(): Promise<GodModeHiddenFeatures> {
    hiddenFeaturesRequest ??= godmodeGet<GodModeFeatureConfigResponse>("config")
        .then((cfg) => new GodModeHiddenFeatures(cfg.featuresToHide ?? []))
        .catch(() => {
            hiddenFeaturesRequest = undefined;
            return GodModeHiddenFeatures.none;
        });
    return hiddenFeaturesRequest;
}

function normalise(value: string): string {
    return value.trim().toLowerCase();
}
