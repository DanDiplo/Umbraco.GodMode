export interface SortState {
    column: string;
    reverse: boolean;
}

export function applySort<T extends Record<string, unknown>>(items: readonly T[], sort: SortState): T[] {
    const { column, reverse } = sort;
    if (!column) return [...items];
    const sorted = [...items].sort((a, b) => {
        const av = a[column];
        const bv = b[column];
        if (av === bv) return 0;
        if (av === null || av === undefined) return 1;
        if (bv === null || bv === undefined) return -1;
        if (typeof av === "number" && typeof bv === "number") return av - bv;
        return String(av).localeCompare(String(bv), undefined, { numeric: true, sensitivity: "base" });
    });
    return reverse ? sorted.reverse() : sorted;
}

export function toggleSort(current: SortState, column: string): SortState {
    if (current.column === column) {
        return { column, reverse: !current.reverse };
    }
    return { column, reverse: false };
}
