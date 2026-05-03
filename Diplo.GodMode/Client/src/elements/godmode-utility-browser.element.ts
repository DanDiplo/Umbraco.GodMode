import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet, godmodePost } from "../api/client";
import "../shared";
import type { Lang, ServerResponse } from "../shared/types";

function responseKind(r: ServerResponse): { color: string; label: string } {
    // C# enum order is Success=0, Error=1, Warning=2 (see ServerResponseType.cs)
    if (r.response === "Success" || r.responseType === 0) return { color: "positive", label: "Success" };
    if (r.response === "Warning" || r.responseType === 2) return { color: "warning", label: "Warning" };
    return { color: "danger", label: "Error" };
}

@customElement("godmode-utility-browser")
export class GodModeUtilityBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _languages: Lang[] = [];
    @state() private _selectedCulture: string | null = null;
    @state() private _busy = false;
    @state() private _warmupCurrent = 0;
    @state() private _warmupTotal = 0;
    @state() private _warmupCurrentUrl = "";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadLanguages();
    }

    private async _loadLanguages() {
        try {
            this._languages = await godmodeGet<Lang[]>("languages");
        } catch {
            // non-fatal
        }
    }

    private async _notifyResponse(promise: Promise<ServerResponse>) {
        this._busy = true;
        try {
            const r = await promise;
            const kind = responseKind(r);
            this.dispatchEvent(
                new CustomEvent("notify", {
                    detail: { color: kind.color, message: r.message },
                    bubbles: true,
                    composed: true
                })
            );
            console.log(`[${kind.label}] ${r.message}`);
        } catch (e) {
            console.error(e);
        } finally {
            this._busy = false;
        }
    }

    private _clearCache(cache: string) {
        return this._notifyResponse(godmodePost<ServerResponse>("cache/clear", { cache }));
    }

    private _purgeMediaCache() {
        if (!confirm("This will delete cached image crops on disk. Continue?")) return;
        return this._notifyResponse(godmodePost<ServerResponse>("cache/purge-media"));
    }

    private _restartAppPool() {
        if (!confirm("This will take the site offline (and won't restart it). Are you sure?")) return;
        return this._notifyResponse(godmodePost<ServerResponse>("app/restart"));
    }

    private async _warmupTemplates() {
        try {
            this._busy = true;
            const urls = await godmodeGet<string[]>("templates/urls-to-ping");
            await this._pingAll(urls);
        } finally {
            this._busy = false;
        }
    }

    private async _warmupAll() {
        try {
            this._busy = true;
            const urls = await godmodeGet<string[]>("urls-to-ping", {
                culture: this._selectedCulture ?? ""
            });
            await this._pingAll(urls);
        } finally {
            this._busy = false;
        }
    }

    private async _pingAll(urls: string[]) {
        if (!urls.length) {
            console.warn("URL list was empty");
            return;
        }
        this._warmupTotal = urls.length;
        this._warmupCurrent = 0;
        this._warmupCurrentUrl = "[waiting…]";
        for (const url of urls) {
            this._warmupCurrentUrl = url;
            try {
                await fetch(url);
            } catch {
                /* noop — count progress regardless */
            }
            this._warmupCurrent++;
        }
        this._warmupCurrentUrl = "Done.";
    }

    override render() {
        return html`
            <godmode-page heading="Utilities" description="Clear caches, restart the application, warm up templates.">
                <uui-box headline="Caches">
                    <div class="row">
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._clearCache("All")}>Clear all caches</uui-button>
                        <uui-button look="secondary" ?disabled=${this._busy} @click=${() => void this._clearCache("ContentCache")}>Clear content cache</uui-button>
                        <uui-button look="secondary" ?disabled=${this._busy} @click=${() => void this._clearCache("MediaCache")}>Clear media cache</uui-button>
                        <uui-button look="secondary" color="danger" ?disabled=${this._busy} @click=${() => void this._purgeMediaCache()}>Purge media folder</uui-button>
                    </div>
                </uui-box>

                <uui-box headline="Application" style="margin-top: var(--uui-size-space-3)">
                    <p>Stops the app — your hosting platform should restart it automatically. Don't use on production unless you know what you're doing.</p>
                    <uui-button look="primary" color="danger" ?disabled=${this._busy} @click=${() => void this._restartAppPool()}>Stop application</uui-button>
                </uui-box>

                <uui-box headline="Warm-up" style="margin-top: var(--uui-size-space-3)">
                    <p>Compile every Razor template by visiting one URL per template, or visit every URL on the site for a chosen culture.</p>
                    <div class="row">
                        <select @change=${(e: Event) => (this._selectedCulture = (e.target as HTMLSelectElement).value || null)}>
                            <option value="">Default culture</option>
                            ${this._languages.map((l) => html`<option value=${l.culture}>${l.name}</option>`)}
                        </select>
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._warmupTemplates()}>Warm up templates</uui-button>
                        <uui-button look="primary" ?disabled=${this._busy} @click=${() => void this._warmupAll()}>Ping every URL</uui-button>
                    </div>
                    ${this._warmupTotal
                        ? html`<p class="progress">${this._warmupCurrent} / ${this._warmupTotal} — <code>${this._warmupCurrentUrl}</code></p>`
                        : ""}
                </uui-box>
            </godmode-page>
        `;
    }

    static override styles = css`
        .row {
            display: flex;
            gap: var(--uui-size-space-3);
            flex-wrap: wrap;
            align-items: center;
        }
        select {
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .progress {
            margin-top: var(--uui-size-space-3);
            color: var(--uui-color-text-alt);
        }
    `;
}

export default GodModeUtilityBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-utility-browser": GodModeUtilityBrowserElement;
    }
}
