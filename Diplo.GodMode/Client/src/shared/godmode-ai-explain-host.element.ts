import { LitElement, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";

export interface GodModeAiExplainSubject {
    subjectType: string;
    title: string;
    data: Record<string, unknown>;
    context?: Record<string, unknown>;
}

const AI_EXPLAIN_BUTTON_TAG = "godmode-ai-explain-button";

@customElement("godmode-ai-explain-host")
export class GodModeAiExplainHostElement extends LitElement {
    @property({ type: Object, attribute: false }) subject?: GodModeAiExplainSubject;

    @state() private _isAiExplainAvailable = customElements.get(AI_EXPLAIN_BUTTON_TAG) !== undefined;

    override connectedCallback(): void {
        super.connectedCallback();

        if (this._isAiExplainAvailable) return;

        void customElements.whenDefined(AI_EXPLAIN_BUTTON_TAG).then(() => {
            this._isAiExplainAvailable = true;
        });
    }

    override render() {
        if (!this.subject || !this._isAiExplainAvailable) return html``;

        return html`<godmode-ai-explain-button .subject=${this.subject}></godmode-ai-explain-button>`;
    }
}

export default GodModeAiExplainHostElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-ai-explain-host": GodModeAiExplainHostElement;
    }
}
