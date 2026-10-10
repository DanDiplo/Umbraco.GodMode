import { LitElement, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { formatInZone, formatOffset } from "./date-time";

/** Only this small element updates each second; its containing page remains unchanged. */
@customElement("godmode-live-clock")
export class GodModeLiveClockElement extends LitElement {
    @property() timeZone = "UTC";
    @property({ type: Number }) driftMs = 0;
    @property({ type: Number }) offsetMinutes = 0;
    @state() private _now = Date.now();
    private _timer?: number;

    override connectedCallback(): void {
        super.connectedCallback();
        this._now = Date.now();
        this._timer = window.setInterval(() => (this._now = Date.now()), 1000);
    }

    override disconnectedCallback(): void {
        window.clearInterval(this._timer);
        super.disconnectedCallback();
    }

    override render() {
        const date = new Date(this._now + this.driftMs + this.offsetMinutes * 60_000);
        return this.offsetMinutes
            ? html`${formatInZone(date, "UTC", { timeZoneName: undefined })} ${formatOffset(this.offsetMinutes)}`
            : html`${formatInZone(date, this.timeZone)}`;
    }
}
