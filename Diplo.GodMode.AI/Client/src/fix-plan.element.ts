import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { createFixPlan, type GodModeAffectedEntity, type GodModeAnalysisFinding, type GodModeAnalysisResult } from "./api";
import { editLinkForAffectedEntity, openEditorModal } from "./edit-links";

@customElement("godmode-ai-fix-plan")
export class GodModeAiFixPlanElement extends UmbElementMixin(LitElement) {
  @state() private _loading = false;
  @state() private _result?: GodModeAnalysisResult;
  @state() private _error = "";

  override render() {
    return html`
      <umb-body-layout header-fit-height>
        <header slot="header">
          <div>
            <h1>AI Fix Plan</h1>
            <p>Prioritise God Mode schema, cleanup and drift signals into an actionable remediation queue.</p>
          </div>
          <uui-button look="primary" color="positive" ?disabled=${this._loading} @click=${() => void this._run()}>
            ${this._loading ? "Planning..." : "Create plan"}
          </uui-button>
        </header>

        <main>
          ${this._loading ? html`<uui-loader></uui-loader>` : ""}
          ${this._error ? html`<uui-box headline="Fix plan unavailable"><p class="error">${this._error}</p></uui-box>` : ""}
          ${this._result ? this._renderResult(this._result) : this._renderEmpty()}
        </main>
      </umb-body-layout>
    `;
  }

  private async _run() {
    this._loading = true;
    this._error = "";
    try {
      this._result = await createFixPlan();
    } catch (error) {
      this._error = error instanceof Error ? error.message : String(error);
    } finally {
      this._loading = false;
    }
  }

  private _renderEmpty() {
    if (this._loading || this._result || this._error) return "";
    return html`<uui-box><p>No fix plan has been created yet.</p></uui-box>`;
  }

  private _renderResult(result: GodModeAnalysisResult) {
    return html`
      <uui-box headline=${result.providerName || "AI Fix Plan"}>
        <p>${this._displaySummary(result.summary)}</p>
        ${result.suggestedNextSteps?.length
          ? html`
              <h2>Next Steps</h2>
              <ul>
                ${result.suggestedNextSteps.map((step) => html`<li>${step}</li>`)}
              </ul>
            `
          : ""}
      </uui-box>

      <section>
        <h2>Plan</h2>
        ${result.findings?.length
          ? html`<ol class="plan">${result.findings.map((finding, index) => this._renderPlanItem(finding, index))}</ol>`
          : html`<uui-box><p>No fix-plan items were returned.</p></uui-box>`}
      </section>
    `;
  }

  private _renderPlanItem(finding: GodModeAnalysisFinding, index: number) {
    return html`
      <li>
        <article>
          <div class="plan-head">
            <span class="rank">${index + 1}</span>
            <div>
              <h3>${finding.title}</h3>
              <p>${finding.category || "Review First"} · Priority ${finding.score}</p>
            </div>
            <span class=${`badge ${finding.severity.toLowerCase()}`}>${finding.severity}</span>
          </div>
          <p>${finding.detail}</p>
          ${this._renderAffectedEntities(finding)}
          <p class="recommendation">${finding.recommendation}</p>
        </article>
      </li>
    `;
  }

  private _renderAffectedEntities(finding: GodModeAnalysisFinding) {
    const entities = finding.affectedEntities?.length
      ? finding.affectedEntities
      : finding.entityName
        ? [{ entityType: finding.entityType, name: finding.entityName, alias: finding.entityAlias, key: "", context: "" }]
        : [];

    if (!entities.length) return "";

    return html`
      <div class="affected">
        <h4>Affected</h4>
        <div class="affected-list">
          ${entities.map((entity) => this._renderAffectedEntity(entity))}
        </div>
      </div>
    `;
  }

  private _renderAffectedEntity(entity: GodModeAffectedEntity) {
    const editLink = editLinkForAffectedEntity(entity);
    const name = entity.name || "Unnamed";

    return html`
      <div class="affected-row">
        <span>${entity.entityType || "Entity"}</span>
        ${editLink
          ? html`<a href=${editLink.href} @click=${(e: Event) => openEditorModal(this, editLink, e)}><strong>${name}</strong></a>`
          : html`<strong>${name}</strong>`}
        ${entity.alias ? html`<code>${entity.alias}</code>` : ""}
        ${entity.context ? html`<small>${entity.context}</small>` : ""}
      </div>
    `;
  }

  private _displaySummary(summary: string) {
    const trimmed = summary?.trim() ?? "";
    if (!trimmed) return "No summary was returned.";
    return trimmed.startsWith("{") || trimmed.startsWith("[") ? "The AI response could not be rendered as a structured fix plan." : trimmed;
  }

  static override styles = css`
    :host {
      display: contents;
    }
    main {
      display: grid;
      gap: var(--uui-size-space-5);
      max-width: 1280px;
      padding: var(--uui-size-space-5);
    }
    header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-5);
    }
    h1,
    h2,
    h3,
    h4,
    p {
      margin: 0;
    }
    h1 {
      font-size: 1.6rem;
      font-weight: 700;
    }
    h2 {
      margin-bottom: var(--uui-size-space-3);
      font-size: 1.05rem;
    }
    h3 {
      font-size: 1rem;
    }
    p {
      color: var(--uui-color-text-alt);
      line-height: 1.5;
    }
    uui-box p,
    header p {
      margin-top: var(--uui-size-space-2);
    }
    .plan {
      display: grid;
      gap: var(--uui-size-space-3);
      margin: 0;
      padding: 0;
      list-style: none;
    }
    article {
      display: grid;
      gap: var(--uui-size-space-3);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface);
      padding: var(--uui-size-space-4);
    }
    .plan-head {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr) auto;
      align-items: center;
      gap: var(--uui-size-space-3);
    }
    .rank {
      display: inline-grid;
      place-items: center;
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: var(--uui-color-interactive);
      color: var(--uui-color-selected-contrast);
      font-weight: 700;
    }
    .affected {
      display: grid;
      gap: var(--uui-size-space-2);
      border-top: 1px solid var(--uui-color-border);
      padding-top: var(--uui-size-space-3);
    }
    .affected-list {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: var(--uui-size-space-2);
    }
    .affected-row {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr);
      align-items: center;
      gap: var(--uui-size-space-1) var(--uui-size-space-2);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      padding: var(--uui-size-space-2);
      background: var(--uui-color-surface-alt);
    }
    .affected-row span {
      color: var(--uui-color-text-alt);
      font-size: 0.78rem;
      font-weight: 700;
      text-transform: uppercase;
    }
    .affected-row a {
      color: var(--uui-color-interactive);
      text-decoration: none;
      overflow-wrap: anywhere;
    }
    .affected-row a:hover,
    .affected-row a:focus-visible {
      color: var(--uui-color-interactive-emphasis);
      text-decoration: underline;
    }
    .affected-row code,
    .affected-row small {
      grid-column: 2;
      margin: 0;
    }
    .affected-row small {
      color: var(--uui-color-text-alt);
    }
    .recommendation {
      color: var(--uui-color-text);
      font-weight: 700;
    }
    .badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 64px;
      border-radius: 999px;
      padding: 2px var(--uui-size-space-2);
      color: white;
      font-weight: 700;
      font-size: 12px;
    }
    .high {
      background: #c92a2a;
    }
    .medium {
      background: #f08c00;
    }
    .low {
      background: #2f80ed;
    }
    .info {
      background: #6c757d;
    }
    .error {
      color: var(--uui-color-danger);
    }
    @media (max-width: 760px) {
      header,
      .plan-head {
        display: grid;
        grid-template-columns: 1fr;
      }
    }
  `;
}

export default GodModeAiFixPlanElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-fix-plan": GodModeAiFixPlanElement;
  }
}
