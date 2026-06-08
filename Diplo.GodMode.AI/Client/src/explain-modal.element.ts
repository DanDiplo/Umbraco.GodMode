import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { explainSubject, type GodModeAiExplainResponse } from "./api";
import type { GodModeAiExplainModalData } from "./explain-modal";

@customElement("godmode-ai-explain-modal")
export class GodModeAiExplainModalElement extends LitElement {
  @property({ attribute: false }) modalContext?: any;
  @property({ type: Object, attribute: false }) data?: GodModeAiExplainModalData;

  @state() private _loading = true;
  @state() private _error = "";
  @state() private _result?: GodModeAiExplainResponse;

  override connectedCallback(): void {
    super.connectedCallback();
    void this._load();
  }

  override render() {
    const subject = this.data?.subject;

    return html`
      <uui-dialog-layout headline=${subject?.title || "AI Explanation"}>
        <uui-button class="close" compact look="secondary" label="Close" @click=${this._close}>
          <uui-icon name="icon-wrong"></uui-icon>
        </uui-button>
        ${this._loading ? html`<uui-loader></uui-loader>` : ""}
        ${this._error ? html`<p class="error">${this._error}</p>` : ""}
        ${this._result ? this._renderResult(this._result, subject?.subjectType) : ""}
      </uui-dialog-layout>
    `;
  }

  private async _load() {
    const subject = this.data?.subject;
    if (!subject) {
      this._loading = false;
      this._error = "No item was provided to explain.";
      return;
    }

    try {
      this._result = await explainSubject(subject);
    } catch (error) {
      this._error = error instanceof Error ? error.message : String(error);
    } finally {
      this._loading = false;
    }
  }

  private _renderResult(result: GodModeAiExplainResponse, subjectType?: string) {
    const isLogEvent = subjectType === "Umbraco Log Event";
    const isLogInsight = subjectType === "Umbraco Log Insight";
    const subjectLevel = String(this.data?.subject?.data?.level ?? "");
    const hasException = Boolean(this.data?.subject?.data?.exception);
    const hasStackFrames = Array.isArray(this.data?.subject?.data?.stackFrames) && this.data?.subject?.data?.stackFrames.length > 0;
    const isErrorLikeLogEvent = (isLogEvent || isLogInsight) && (hasException || hasStackFrames || subjectLevel === "Error" || subjectLevel === "Fatal");

    return html`
      <div class="content">
        ${result.summary ? html`<p class="summary">${result.summary}</p>` : ""}
        ${isLogEvent || isLogInsight
          ? html`
              ${this._section(isLogInsight ? "Pattern Diagnosis" : isErrorLikeLogEvent ? "Primary Diagnosis" : "What Happened", result.primaryDiagnosis)}
              ${this._section(isErrorLikeLogEvent || isLogInsight ? "Where To Look" : "Relevant Context", result.whereToLook, "code")}
              ${this._section(isLogInsight ? "Likely Shared Cause" : isErrorLikeLogEvent ? "Likely Cause" : "Likely Trigger", result.likelyCause)}
              ${result.howToFix?.length
                ? html`
                    <section>
                      <h2>${isLogInsight ? "Triage / Fix Plan" : isErrorLikeLogEvent ? "How To Fix" : "Recommended Action"}</h2>
                      <ul>
                        ${result.howToFix.map((step) => html`<li>${step}</li>`)}
                      </ul>
                    </section>
                  `
                : ""}
            `
          : html`
              ${this._section("What It Is", result.whatItIs)}
              ${this._section("Why It Matters", result.whyItMatters)}
            `}
        ${result.observedDetails?.length
          ? html`
              <section>
                <h2>${isLogEvent || isLogInsight ? "Supporting Evidence" : "Observed Details"}</h2>
                <ul>
                  ${result.observedDetails.map((detail) => html`<li>${detail}</li>`)}
                </ul>
              </section>
            `
          : ""}
        ${this._section("Risk", result.risk)}
        ${result.suggestedNextSteps?.length
          ? html`
              <section>
                <h2>Next Steps</h2>
                <ul>
                  ${result.suggestedNextSteps.map((step) => html`<li>${step}</li>`)}
                </ul>
              </section>
            `
          : ""}
      </div>
    `;
  }

  private _section(label: string, value?: string, kind: "text" | "code" = "text") {
    return value
      ? html`
          <section>
            <h2>${label}</h2>
            ${kind === "code" ? html`<p><code>${value}</code></p>` : html`<p>${value}</p>`}
          </section>
        `
      : "";
  }

  private _close() {
    this.modalContext?.reject();
  }

  static override styles = css`
    uui-dialog-layout {
      position: relative;
      width: min(820px, 92vw);
      max-height: 82vh;
    }
    .close {
      position: absolute;
      top: var(--uui-size-space-4);
      right: var(--uui-size-space-4);
      z-index: 1;
    }
    .content {
      display: grid;
      gap: var(--uui-size-space-4);
      max-width: 720px;
    }
    .summary {
      margin: 0;
      color: var(--uui-color-text);
      font-weight: 700;
      line-height: 1.5;
    }
    section {
      display: grid;
      gap: var(--uui-size-space-2);
    }
    h2 {
      margin: 0;
      font-size: 0.95rem;
    }
    p,
    ul {
      margin: 0;
      color: var(--uui-color-text-alt);
      line-height: 1.5;
    }
    code {
      overflow-wrap: anywhere;
      white-space: pre-wrap;
    }
    .error {
      color: var(--uui-color-danger);
    }
  `;
}

export default GodModeAiExplainModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-explain-modal": GodModeAiExplainModalElement;
  }
}
