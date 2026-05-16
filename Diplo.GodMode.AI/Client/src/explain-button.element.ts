import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import type { GodModeAiExplainSubject } from "./api";
import { openExplainModal } from "./explain-modal";

type GodModeAiExplainSubjectProvider = () => GodModeAiExplainSubject | Promise<GodModeAiExplainSubject>;

@customElement("godmode-ai-explain-button")
export class GodModeAiExplainButtonElement extends UmbElementMixin(LitElement) {
  @property({ type: Object, attribute: false }) subject?: GodModeAiExplainSubject;
  @property({ attribute: false }) subjectProvider?: GodModeAiExplainSubjectProvider;
  @state() private _loading = false;

  private async _open(e: Event) {
    e.stopPropagation();

    if (this._loading) return;

    this._loading = true;
    try {
      const subject = this.subjectProvider ? await this.subjectProvider() : this.subject;
      if (subject) {
        openExplainModal(this, subject, e);
      }
    } finally {
      this._loading = false;
    }
  }

  override render() {
    if (!this.subject && !this.subjectProvider) return html``;

    return html`
      <uui-button class="explain-button" compact look="secondary" label="Explain" title="Explain this item with AI" ?disabled=${this._loading} @click=${this._open}>
        <uui-icon name="icon-help-alt"></uui-icon>
        ${this._loading ? "Loading..." : "Explain"}
      </uui-button>
    `;
  }

  static override styles = [
    css`
      :host {
        display: contents;
      }

      .explain-button {
        --uui-button-contrast: var(--uui-palette-space-cadet, #00003c);
      }
    `
  ];
}

export default GodModeAiExplainButtonElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-explain-button": GodModeAiExplainButtonElement;
  }
}
