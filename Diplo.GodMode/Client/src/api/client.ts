import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { GODMODE_API_BASE } from "../constants";

/**
 * The backoffice HTTP client only attaches the bearer token when a request
 * declares the bearer security scheme — without this, every call comes back
 * 401 and trips the auth interceptor's re-auth loop.
 */
const BEARER_SECURITY = [{ scheme: "bearer", type: "http" }] as const;

function buildQuery(query?: Record<string, unknown>): string {
    if (!query) return "";
    const parts = Object.entries(query)
        .filter(([, v]) => v !== undefined && v !== null && v !== "")
        .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`);
    return parts.length ? "?" + parts.join("&") : "";
}

/**
 * Thin GET helper around the backoffice HTTP client. Once we ship a generated
 * OpenAPI client (Phase 2.5) this file becomes a barrel re-export.
 */
export async function godmodeGet<T>(path: string, query?: Record<string, unknown>): Promise<T> {
    const { data, error } = await umbHttpClient.get<T>({
        url: `${GODMODE_API_BASE}/${path}${buildQuery(query)}`,
        security: BEARER_SECURITY
    });

    if (error) {
        throw error;
    }

    return data as T;
}

export async function godmodePost<T>(path: string, query?: Record<string, unknown>, body?: unknown): Promise<T> {
    const { data, error } = await umbHttpClient.post<T>({
        url: `${GODMODE_API_BASE}/${path}${buildQuery(query)}`,
        body,
        security: BEARER_SECURITY
    });

    if (error) {
        throw error;
    }

    return data as T;
}

export async function godmodeDelete<T>(path: string): Promise<T> {
    const { data, error } = await umbHttpClient.delete<T>({
        url: `${GODMODE_API_BASE}/${path}`,
        security: BEARER_SECURITY
    });

    if (error) {
        throw error;
    }

    return data as T;
}
