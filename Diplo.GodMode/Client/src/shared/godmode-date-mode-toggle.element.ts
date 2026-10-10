import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { DATE_MODE_CHANGE_EVENT, getDateDisplayMode, setDateDisplayMode, type DateDisplayMode } from "./date-time";

const OPTIONS: Array<{ mode: DateDisplayMode; label: string; title: string }> = [
    { mode: "local", label: "Local", title: "Show dates in your browser's time zone" },
    { mode: "server", label: "Server", title: "Show dates in the web server's time zone" },
    { mode: "utc", label: "UTC", title: "Show dates in UTC, as Umbraco 17 stores them" }
];

/** Switches how every `<godmode-date>` in GodMode displays dates. The choice is remembered per browser. */
@customElement("godmode-date-mode-toggle")
export class GodModeDateModeToggleElement extends LitElement {
    @state() private _mode: DateDisplayMode = getDateDisplayMode();

    private _onModeChange = () => (this._mode = getDateDisplayMode());

    override connectedCallback(): void {
        super.connectedCallback();
        window.addEventListener(DATE_MODE_CHANGE_EVENT, this._onModeChange);
    }

    override disconnectedCallback(): void {
        window.removeEventListener(DATE_MODE_CHANGE_EVENT, this._onModeChange);
        super.disconnectedCallback();
    }

    override render() {
        return html`
            <span class="label">Dates in</span>
            <uui-button-group>
                ${OPTIONS.map(
                    (option) => html`
                        <uui-button
                            compact
                            look=${this._mode === option.mode ? "primary" : "secondary"}
                            label=${option.title}
                            title=${option.title}
                            @click=${() => setDateDisplayMode(option.mode)}
                            >${option.label}</uui-button
                        >
                    `
                )}
            </uui-button-group>
        `;
    }

    static override styles = css`
        :host {
            display: inline-flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }
        .label {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-date-mode-toggle": GodModeDateModeToggleElement;
    }
}
