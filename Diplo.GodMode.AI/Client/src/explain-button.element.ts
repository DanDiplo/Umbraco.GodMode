import { LitElement, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import type { GodModeAiExplainSubject } from "./api";
import { openExplainModal } from "./explain-modal";

@customElement("godmode-ai-explain-button")
export class GodModeAiExplainButtonElement extends UmbElementMixin(LitElement) {
  @property({ type: Object, attribute: false }) subject?: GodModeAiExplainSubject;

  override render() {
    if (!this.subject) return html``;

    return html`
      <uui-button compact look="secondary" label="Explain" title="Explain this item with AI" @click=${(e: Event) => openExplainModal(this, this.subject!, e)}>
        Explain
      </uui-button>
    `;
  }
}

export default GodModeAiExplainButtonElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-explain-button": GodModeAiExplainButtonElement;
  }
}
