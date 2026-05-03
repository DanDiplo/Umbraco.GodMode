/** Replaces the legacy `godModeFileSize` filter. */
export function formatBytes(bytes: number | null | undefined, precision = 1): string {
    if (bytes == null) return "N/A";
    if (!Number.isFinite(bytes)) return "-";
    if (bytes === 0) return "0 bytes";
    const units = ["bytes", "KB", "MB", "GB", "TB"];
    const i = Math.min(units.length - 1, Math.floor(Math.log(bytes) / Math.log(1024)));
    return `${(bytes / Math.pow(1024, i)).toFixed(precision)} ${units[i]}`;
}

/** Replaces the legacy `godModeUnique` filter. */
export function uniqueBy<T>(items: readonly T[], key: keyof T): T[] {
    const seen = new Set<unknown>();
    const out: T[] = [];
    for (const item of items) {
        const v = item[key];
        if (!seen.has(v)) {
            seen.add(v);
            out.push(item);
        }
    }
    return out;
}

/** Truncate a string to N chars, appending an ellipsis if it was cut. */
export function truncate(s: string | null | undefined, n: number): string {
    if (!s) return "";
    return s.length <= n ? s : s.slice(0, n) + "…";
}

/** ISO date → short readable string. Returns the input as-is if not parseable. */
export function formatDate(s: string | null | undefined): string {
    if (!s) return "";
    const d = new Date(s);
    if (Number.isNaN(d.getTime())) return s;
    return d.toLocaleString();
}
