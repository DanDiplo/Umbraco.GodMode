import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet, godmodePost } from "../api/client";
import "../shared";
import type { DiagnosticGroup } from "../shared/types";

const REVEAL_PASSWORD_SESSION_KEY = "diplo.godmode.diagnostics.revealPassword";

@customElement("godmode-diagnostic-browser")
export class GodModeDiagnosticBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _groups: DiagnosticGroup[] = [];
    @state() private _selectedGroupId: number | null = null;
    @state() private _search = "";
    @state() private _loading = true;
    @state() private _revealEnabled = false;
    @state() private _revealed = false;
    @state() private _revealPassword = sessionStorage.getItem(REVEAL_PASSWORD_SESSION_KEY) ?? "";
    @state() private _revealError = "";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        this._revealError = "";
        try {
            this._revealEnabled = await godmodeGet<boolean>("diagnostics/reveal-enabled");
            this._groups = this._revealEnabled && this._revealPassword
                ? await this._loadRevealed()
                : await godmodeGet<DiagnosticGroup[]>("diagnostics");
            if (this._groups.length && this._selectedGroupId === null) {
                this._selectedGroupId = this._groups[0].id;
            }
        } catch {
            sessionStorage.removeItem(REVEAL_PASSWORD_SESSION_KEY);
            this._revealPassword = "";
            this._revealed = false;
            this._groups = await godmodeGet<DiagnosticGroup[]>("diagnostics");
        } finally {
            this._loading = false;
        }
    }

    private async _loadRevealed() {
        const groups = await godmodePost<DiagnosticGroup[]>("diagnostics/reveal", undefined, { password: this._revealPassword });
        sessionStorage.setItem(REVEAL_PASSWORD_SESSION_KEY, this._revealPassword);
        this._revealed = true;
        return groups;
    }

    private async _reveal() {
        if (!this._revealPassword) return;

        this._loading = true;
        this._revealError = "";
        try {
            this._groups = await this._loadRevealed();
        } catch {
            sessionStorage.removeItem(REVEAL_PASSWORD_SESSION_KEY);
            this._revealed = false;
            this._revealError = "Password not accepted";
        } finally {
            this._loading = false;
        }
    }

    private async _hideRedactedValues() {
        sessionStorage.removeItem(REVEAL_PASSWORD_SESSION_KEY);
        this._revealPassword = "";
        this._revealed = false;
        this._revealError = "";
        await this._load();
    }

    private _selectedGroup(): DiagnosticGroup | undefined {
        return this._groups.find((g) => g.id === this._selectedGroupId);
    }

    override render() {
        const group = this._selectedGroup();
        const q = this._search.trim().toLowerCase();
        return html`
            <godmode-page heading="Diagnostics" description="Umbraco settings, server config, runtime info." show-reload @reload=${() => void this._load()}>
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Group</label>
                            <select @change=${(e: Event) => (this._selectedGroupId = Number((e.target as HTMLSelectElement).value))}>
                                ${this._groups.map(
                                    (g) => html`<option value=${g.id} ?selected=${g.id === this._selectedGroupId}>${g.title}</option>`
                                )}
                            </select>
                        </div>
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by key or value"
                                .value=${this._search}
                                @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label class="label-with-help">
                                Reveal
                                <uui-icon
                                    name="icon-help-alt"
                                    title="Enter the password from the configured local environment variable to reload diagnostics with redacted values revealed for this browser session."
                                ></uui-icon>
                            </label>
                            ${this._revealEnabled
                                ? html`
                                      <div class="reveal">
                                          <uui-input
                                              type="password"
                                              autocomplete="current-password"
                                              placeholder="Password"
                                              .value=${this._revealPassword}
                                              ?disabled=${this._revealed}
                                              @input=${(e: Event) => (this._revealPassword = (e.target as HTMLInputElement).value)}
                                              @keydown=${(e: KeyboardEvent) => {
                                                  if (e.key === "Enter") void this._reveal();
                                              }}
                                          ></uui-input>
                                          ${this._revealed
                                              ? html`<uui-button look="secondary" @click=${() => void this._hideRedactedValues()}>Hide</uui-button>`
                                              : html`<uui-button look="primary" ?disabled=${!this._revealPassword} @click=${() => void this._reveal()}>
                                                    Reveal
                                              </uui-button>`}
                                      </div>
                                      <div class="reveal-hint">
                                          Enter the local reveal password to show redacted values for this browser session.
                                      </div>
                                      ${this._revealError ? html`<div class="reveal-error">${this._revealError}</div>` : ""}
                                  `
                                : html`<div class="reveal-disabled">Set the reveal password environment variable to enable this.</div>`}
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : !group
                      ? ""
                      : group.sections.map((sec) => {
                            const matched = sec.diagnostics.filter((d) => {
                                if (!q) return true;
                                if (d.key.toLowerCase().includes(q)) return true;
                                const vs = d.value == null ? "" : String(d.value).toLowerCase();
                                return vs.includes(q);
                            });
                            if (!matched.length && q) return "";
                            return html`
                                <uui-box headline=${sec.heading} style="margin-top: var(--uui-size-space-3)">
                                    <uui-table>
                                        <uui-table-head>
                                            <uui-table-head-cell style="width:35%">Key</uui-table-head-cell>
                                            <uui-table-head-cell>Value</uui-table-head-cell>
                                            <uui-table-head-cell>Actions</uui-table-head-cell>
                                        </uui-table-head>
                                        ${matched.map(
                                            (d) => html`
                                                <uui-table-row>
                                                    <uui-table-cell><strong>${d.key}</strong></uui-table-cell>
                                                    <uui-table-cell class="value-cell"><code>${d.value == null ? "—" : String(d.value)}</code></uui-table-cell>
                                                    <uui-table-cell class="action-cell">
                                                        <godmode-ai-explain-host
                                                            .subject=${this._explainSubject(group.title, sec.heading, d)}
                                                        ></godmode-ai-explain-host>
                                                    </uui-table-cell>
                                                </uui-table-row>
                                            `
                                        )}
                                    </uui-table>
                                </uui-box>
                            `;
                        })}
            </godmode-page>
        `;
    }

    private _explainSubject(groupTitle: string, sectionHeading: string, diagnostic: { key: string; value: string | null }) {
        return {
            subjectType: "God Mode diagnostic",
            title: diagnostic.key,
            data: {
                group: groupTitle,
                section: sectionHeading,
                key: diagnostic.key,
                value: diagnostic.value,
                valueIsRedacted: diagnostic.value === "[Redacted]",
                source: "God Mode diagnostics"
            }
        };
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: 1fr 2fr 1.5fr;
            gap: var(--uui-size-space-4);
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .label-with-help {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-1);
        }
        .label-with-help uui-icon {
            color: var(--uui-color-text-alt);
            cursor: help;
        }
        .filters uui-input {
            width: 100%;
        }
        .reveal {
            display: grid;
            grid-template-columns: minmax(0, 1fr) auto;
            gap: var(--uui-size-space-2);
            align-items: center;
        }
        .reveal-error {
            color: var(--uui-color-danger);
            font-size: 0.875rem;
            margin-top: var(--uui-size-space-1);
        }
        .reveal-hint {
            color: var(--uui-color-text-alt);
            font-size: 0.8125rem;
            line-height: 1.35;
            margin-top: var(--uui-size-space-1);
        }
        .reveal-disabled {
            min-height: 32px;
            display: flex;
            align-items: center;
            color: var(--uui-color-text-alt);
        }
        .filters select {
            width: 100%;
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .action-cell {
            width: 7rem;
        }
        .value-cell {
            min-width: 0;
            max-width: 0;
        }
        .value-cell code {
            display: block;
            max-width: 100%;
            white-space: pre-wrap;
            overflow-wrap: anywhere;
            word-break: break-word;
        }
        @media (max-width: 900px) {
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeDiagnosticBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-diagnostic-browser": GodModeDiagnosticBrowserElement;
    }
}
