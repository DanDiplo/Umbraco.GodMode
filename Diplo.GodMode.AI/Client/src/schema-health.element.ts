import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { analyzeSchemaHealth, type GodModeAffectedEntity, type GodModeAnalysisFinding, type GodModeAnalysisResult } from "./api";
import { editLinkForAffectedEntity, openEditorModal } from "./edit-links";

@customElement("godmode-ai-schema-health")
export class GodModeAiSchemaHealthElement extends UmbElementMixin(LitElement) {
  @state() private _loading = false;
  @state() private _result?: GodModeAnalysisResult;
  @state() private _error = "";

  override render() {
    return html`
      <umb-body-layout header-fit-height>
        <header slot="header">
          <div>
            <h1>AI Schema Analysis</h1>
            <p>Review document types, data types, templates, references and existing God Mode health signals.</p>
          </div>
          <uui-button look="primary" color="positive" ?disabled=${this._loading} @click=${() => void this._run()}>
            ${this._loading ? "Analysing..." : "Run analysis"}
          </uui-button>
        </header>

        <main>
          ${this._loading ? html`<uui-loader></uui-loader>` : ""}
          ${this._error ? html`<uui-box headline="Analysis unavailable"><p class="error">${this._error}</p></uui-box>` : ""}
          ${this._result ? this._renderResult(this._result) : this._renderEmpty()}
        </main>
      </umb-body-layout>
    `;
  }

  private async _run() {
    this._loading = true;
    this._error = "";
    try {
      this._result = await analyzeSchemaHealth();
    } catch (error) {
      this._error = error instanceof Error ? error.message : String(error);
    } finally {
      this._loading = false;
    }
  }

  private _renderEmpty() {
    if (this._loading || this._result || this._error) return "";
    return html`
      <uui-box>
        <p>No analysis has been run yet.</p>
      </uui-box>
    `;
  }

  private _renderResult(result: GodModeAnalysisResult) {
    return html`
      <section class="summary">
        <uui-box headline=${result.providerName || "AI Schema Health"}>
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
      </section>

      <section>
        <h2>Findings</h2>
        ${result.findings?.length
          ? html`<div class="findings">${result.findings.map((finding) => this._renderFinding(finding))}</div>`
          : html`<uui-box><p>No AI findings were returned.</p></uui-box>`}
      </section>
    `;
  }

  private _renderFinding(finding: GodModeAnalysisFinding) {
    return html`
      <article>
        <div class="finding-head">
          <span class=${`badge ${finding.severity.toLowerCase()}`}>${finding.severity}</span>
          <strong>${finding.title}</strong>
        </div>
        <p>${finding.detail}</p>
        <dl>
          <div>
            <dt>Category</dt>
            <dd>${finding.category}</dd>
          </div>
          <div>
            <dt>Primary Entity</dt>
            <dd>${finding.entityName || "General"}${finding.entityAlias ? html` <code>${finding.entityAlias}</code>` : ""}</dd>
          </div>
        </dl>
        ${this._renderAffectedEntities(finding)}
        <p class="recommendation">${finding.recommendation}</p>
      </article>
    `;
  }

  private _renderAffectedEntities(finding: GodModeAnalysisFinding) {
    const entities = finding.affectedEntities?.length
      ? finding.affectedEntities
      : finding.entityName
        ? [
            {
              entityType: finding.entityType,
              name: finding.entityName,
              alias: finding.entityAlias,
              key: "",
              context: ""
            }
          ]
        : [];

    if (!entities.length) return "";

    return html`
      <div class="affected">
        <h3>Affected</h3>
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
          ? html`
              <a href=${editLink.href} @click=${(e: Event) => openEditorModal(this, editLink, e)}>
                <strong>${name}</strong>
              </a>
            `
          : html`<strong>${name}</strong>`}
        ${entity.alias ? html`<code>${entity.alias}</code>` : ""}
        ${entity.context ? html`<small>${entity.context}</small>` : ""}
      </div>
    `;
  }

  private _displaySummary(summary: string) {
    const trimmed = summary?.trim() ?? "";
    if (!trimmed) return "No summary was returned.";
    if (trimmed.startsWith("{") || trimmed.startsWith("[")) {
      return "The AI provider returned a response that could not be rendered as a structured report.";
    }
    return trimmed;
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
    h1 {
      margin: 0;
      font-size: 1.6rem;
      font-weight: 700;
    }
    h2 {
      margin: 0 0 var(--uui-size-space-3);
      font-size: 1.05rem;
    }
    p {
      margin: var(--uui-size-space-2) 0 0;
      color: var(--uui-color-text-alt);
      line-height: 1.5;
    }
    .summary h2 {
      margin-top: var(--uui-size-space-4);
    }
    .findings {
      display: grid;
      gap: var(--uui-size-space-3);
    }
    article {
      display: grid;
      gap: var(--uui-size-space-3);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
      background: var(--uui-color-surface);
      padding: var(--uui-size-space-4);
    }
    .finding-head {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }
    dl {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: var(--uui-size-space-3);
      margin: 0;
    }
    dt {
      color: var(--uui-color-text-alt);
      font-size: 0.82rem;
      font-weight: 700;
      text-transform: uppercase;
    }
    dd {
      margin: var(--uui-size-space-1) 0 0;
    }
    h3 {
      margin: 0 0 var(--uui-size-space-2);
      font-size: 0.95rem;
    }
    code {
      display: inline-block;
      margin-left: var(--uui-size-space-1);
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
    .affected-row strong {
      overflow-wrap: anywhere;
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
      font-weight: 600;
    }
    .error {
      color: var(--uui-color-danger);
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
    @media (max-width: 760px) {
      header,
      dl {
        grid-template-columns: 1fr;
      }
      header {
        display: grid;
      }
    }
  `;
}

export default GodModeAiSchemaHealthElement;

declare global {
  interface HTMLElementTagNameMap {
    "godmode-ai-schema-health": GodModeAiSchemaHealthElement;
  }
}
