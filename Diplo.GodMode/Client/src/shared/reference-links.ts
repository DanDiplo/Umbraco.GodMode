import { GODMODE_ENTITY_TYPE_PREFIX } from "../constants";

export function referenceGraphUrl(targetType: string, targetKey: string, mode: "used-by" | "uses" = "used-by"): string {
    const params = new URLSearchParams({
        mode,
        targetType,
        targetKey
    });

    return `section/settings/workspace/${GODMODE_ENTITY_TYPE_PREFIX}-referenceGraph?${params.toString()}`;
}
