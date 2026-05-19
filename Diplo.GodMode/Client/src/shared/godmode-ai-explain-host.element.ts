import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { isGodModeAiExplainAvailable, observeGodModeAiExplainAvailability } from "./ai-availability";

export interface GodModeAiExplainSubject {
    subjectType: string;
    title: string;
    data: Record<string, unknown>;
    context?: Record<string, unknown>;
}

export type GodModeAiExplainSubjectProvider = () => GodModeAiExplainSubject | Promise<GodModeAiExplainSubject>;

@customElement("godmode-ai-explain-host")
export class GodModeAiExplainHostElement extends LitElement {
    @property({ type: Object, attribute: false }) subject?: GodModeAiExplainSubject;
    @property({ attribute: false }) subjectProvider?: GodModeAiExplainSubjectProvider;

    @state() private _isAiExplainAvailable = isGodModeAiExplainAvailable();
    private _disposeAvailabilityObserver?: () => void;

    override connectedCallback(): void {
        super.connectedCallback();

        if (this._isAiExplainAvailable) return;

        this._disposeAvailabilityObserver = observeGodModeAiExplainAvailability(() => {
            this._isAiExplainAvailable = true;
        });
    }

    override disconnectedCallback(): void {
        this._disposeAvailabilityObserver?.();
        this._disposeAvailabilityObserver = undefined;
        super.disconnectedCallback();
    }

    override render() {
        if ((!this.subject && !this.subjectProvider) || !this._isAiExplainAvailable) return html``;

        return html`<godmode-ai-explain-button .subject=${this.subject} .subjectProvider=${this.subjectProvider}></godmode-ai-explain-button>`;
    }

    static override styles = [
        css`
            :host {
                display: contents;
            }
        `
    ];
}

export default GodModeAiExplainHostElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-ai-explain-host": GodModeAiExplainHostElement;
    }
}
