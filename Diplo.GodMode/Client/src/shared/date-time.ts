import { godmodeGet } from "../api/client";
import type { ServerTimeInfo } from "./types";

/** Which clock dates are shown in: the viewer's browser, the web server, or UTC. */
export type DateDisplayMode = "local" | "server" | "utc";

export const DATE_MODE_CHANGE_EVENT = "godmode-date-mode-change";

const STORAGE_KEY = "godmode.dateDisplayMode";
const MODES: DateDisplayMode[] = ["local", "server", "utc"];

let mode: DateDisplayMode = readStoredMode();
let serverTime: Promise<ServerTimeInfo | null> | null = null;
let serverTimeZone: string | null = null;

function readStoredMode(): DateDisplayMode {
    try {
        const stored = localStorage.getItem(STORAGE_KEY) as DateDisplayMode | null;
        return stored && MODES.includes(stored) ? stored : "local";
    } catch {
        return "local";
    }
}

export function getDateDisplayMode(): DateDisplayMode {
    return mode;
}

export function setDateDisplayMode(next: DateDisplayMode): void {
    if (next === mode) return;
    mode = next;
    try {
        localStorage.setItem(STORAGE_KEY, next);
    } catch {
        // Storage can be unavailable (private windows); the mode still applies for this page.
    }
    window.dispatchEvent(new CustomEvent(DATE_MODE_CHANGE_EVENT, { detail: next }));
}

/** The viewer's own IANA time zone, e.g. "Europe/London". */
export function browserTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
}

/**
 * Loads (once) the server clock and time zone. Pass `refresh` to re-fetch, e.g. for a clock drift check.
 */
export function loadServerTime(refresh = false): Promise<ServerTimeInfo | null> {
    if (!serverTime || refresh) {
        serverTime = godmodeGet<ServerTimeInfo>("utilities/server-time")
            .then((info) => {
                serverTimeZone = info.ianaId || (info.isUtc ? "UTC" : null);
                return info;
            })
            .catch(() => null);
    }
    return serverTime;
}

/** The server's IANA time zone once {@link loadServerTime} has resolved, else null. */
export function serverTimeZoneId(): string | null {
    return serverTimeZone;
}

/**
 * Parses a date from the API. DateTime values are sent as UTC with a trailing "Z"; anything without an
 * offset is assumed to be UTC too, because Umbraco 17 stores system dates in UTC.
 */
export function parseApiDate(value: string | null | undefined): Date | null {
    if (!value) return null;
    const hasZone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value);
    const normalized = /^\d{4}-\d{2}-\d{2}$/.test(value) ? `${value}T00:00:00Z`
        : hasZone ? value : `${value.replace(" ", "T")}Z`;
    const date = new Date(normalized);
    return Number.isNaN(date.getTime()) ? null : date;
}

/** Resolves the IANA zone for a display mode; server mode falls back to UTC until the server zone is known. */
export function timeZoneFor(displayMode: DateDisplayMode): string {
    switch (displayMode) {
        case "utc":
            return "UTC";
        case "server":
            return serverTimeZone ?? "UTC";
        default:
            return browserTimeZone();
    }
}

/** Formats an API date in the given (or current) display mode, always including the zone name. */
export function formatDateTime(value: string | null | undefined, displayMode: DateDisplayMode = mode, fallback = ""): string {
    const date = parseApiDate(value);
    if (!date) return value || fallback;
    return formatInZone(date, timeZoneFor(displayMode));
}

export function formatInZone(date: Date, timeZone: string, options: Intl.DateTimeFormatOptions = {}): string {
    try {
        return new Intl.DateTimeFormat(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            timeZone,
            timeZoneName: "short",
            ...options
        }).format(date);
    } catch {
        return date.toISOString();
    }
}

/** Multi-line tooltip showing a date in UTC, server time and the viewer's local time. */
export function describeDate(value: string | null | undefined): string {
    const date = parseApiDate(value);
    if (!date) return "";
    const lines = [`UTC: ${formatInZone(date, "UTC")}`];
    if (serverTimeZone) lines.push(`Server: ${formatInZone(date, serverTimeZone)}`);
    lines.push(`Local: ${formatInZone(date, browserTimeZone())}`);
    return lines.join("\n");
}

/** Short relative description such as "in 5 min" or "3 hours ago". */
export function formatRelative(value: string | null | undefined, now: number = Date.now()): string {
    const date = parseApiDate(value);
    if (!date) return "";
    const seconds = Math.round((date.getTime() - now) / 1000);
    const abs = Math.abs(seconds);
    const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
        ["day", 86400],
        ["hour", 3600],
        ["minute", 60],
        ["second", 1]
    ];
    const [unit, size] = units.find(([, s]) => abs >= s) ?? ["second", 1];
    return new Intl.RelativeTimeFormat(undefined, { numeric: "auto" }).format(Math.round(seconds / size), unit);
}

/** Current UTC offset of an IANA zone in minutes, e.g. 60 for Europe/London in summer. */
export function zoneOffsetMinutes(timeZone: string, at: Date = new Date()): number {
    try {
        const parts = new Intl.DateTimeFormat("en-US", {
            timeZone,
            hourCycle: "h23",
            year: "numeric",
            month: "numeric",
            day: "numeric",
            hour: "numeric",
            minute: "numeric",
            second: "numeric"
        }).formatToParts(at);
        const get = (type: string) => Number(parts.find((p) => p.type === type)?.value ?? 0);
        const asUtc = Date.UTC(get("year"), get("month") - 1, get("day"), get("hour"), get("minute"), get("second"));
        return Math.round((asUtc - Math.floor(at.getTime() / 1000) * 1000) / 60000);
    } catch {
        return -at.getTimezoneOffset();
    }
}

export function formatOffset(minutes: number): string {
    const sign = minutes < 0 ? "-" : "+";
    const abs = Math.abs(minutes);
    return `UTC${sign}${String(Math.floor(abs / 60)).padStart(2, "0")}:${String(abs % 60).padStart(2, "0")}`;
}
