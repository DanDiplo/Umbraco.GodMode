import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import {
    DATE_MODE_CHANGE_EVENT,
    describeDate,
    formatDateTime,
    formatRelative,
    getDateDisplayMode,
    loadServerTime,
    parseApiDate,
    type DateDisplayMode
} from "./date-time";

/**
 * Renders an API date in the user's chosen display mode (local, server or UTC), with a tooltip
 * showing all three. Re-renders when the display mode changes anywhere in GodMode.
 */
@customElement("godmode-date")
export class GodModeDateElement extends LitElement {
    @property() value: string | null | undefined = null;

    /** Also show a relative hint such as "in 5 minutes". */
    @property({ type: Boolean }) relative = false;

    /** Text shown when there is no date. */
    @property() empty = "";

    @state() private _mode: DateDisplayMode = getDateDisplayMode();

    private _onModeChange = () => {
        this._mode = getDateDisplayMode();
        if (this._mode === "server") void loadServerTime().then(() => this.requestUpdate());
    };

    override connectedCallback(): void {
        super.connectedCallback();
        window.addEventListener(DATE_MODE_CHANGE_EVENT, this._onModeChange);
        // The server zone is needed for server mode and for the tooltip.
        void loadServerTime().then(() => this.requestUpdate());
    }

    override disconnectedCallback(): void {
        window.removeEventListener(DATE_MODE_CHANGE_EVENT, this._onModeChange);
        super.disconnectedCallback();
    }

    override render() {
        const date = parseApiDate(this.value);
        if (!date) return html`${this.value || this.empty}`;

        return html`<time datetime=${date.toISOString()} title=${describeDate(this.value)}
            >${formatDateTime(this.value, this._mode)}${this.relative ? html` <small>(${formatRelative(this.value)})</small>` : ""}</time
        >`;
    }

    static override styles = css`
        time {
            cursor: help;
        }
        small {
            color: var(--uui-color-text-alt);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-date": GodModeDateElement;
    }
}
