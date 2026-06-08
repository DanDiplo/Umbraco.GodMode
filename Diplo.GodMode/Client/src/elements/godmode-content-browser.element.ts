import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ContentItem, ContentMediaDetail, Lang, Page } from "../shared/types";
import { truncate } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openEvidenceDrawer } from "../shared/evidence-drawer";

type CultureState = {
    iso: string;
    available: boolean;
    published: boolean;
    edited: boolean;
    name: string;
};

@customElement("godmode-content-browser")
export class GodModeContentBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _page: Page<ContentItem> | null = null;
    @state() private _aliases: string[] = [];
    @state() private _languages: Lang[] = [];
    @state() private _loading = false;
    @state() private _currentPage = 1;
    @state() private _pageSize = 15;
    @state() private _orderBy = "N.id";
    @state() private _name = "";
    @state() private _alias = "";
    @state() private _languageId: number | null = null;
    @state() private _missingLanguageId: number | null = null;
    @state() private _publishedLanguageId: number | null = null;
    @state() private _edited: "any" | "yes" | "no" = "any";
    @state() private _trashed: "any" | "yes" | "no" = "any";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadFilters();
        void this._fetch();
    }

    private async _loadFilters() {
        try {
            const [aliases, languages] = await Promise.all([
                godmodeGet<string[]>("standard-content-type-aliases"),
                godmodeGet<Lang[]>("languages")
            ]);
            this._aliases = aliases;
            this._languages = languages;
        } catch {
            // non-fatal
        }
    }

    private async _fetch() {
        this._loading = true;
        try {
            const trashed = this._trashed === "any" ? undefined : this._trashed === "yes";
            const edited = this._edited === "any" ? undefined : this._edited === "yes";
            this._page = await godmodeGet<Page<ContentItem>>("content", {
                page: this._currentPage,
                pageSize: this._pageSize,
                name: this._name,
                alias: this._alias,
                languageId: this._languageId,
                missingLanguageId: this._missingLanguageId,
                publishedLanguageId: this._publishedLanguageId,
                edited,
                trashed,
                orderBy: this._orderBy
            });
        } finally {
            this._loading = false;
        }
    }

    private _filterChange(fn: () => void) {
        fn();
        this._currentPage = 1;
        void this._fetch();
    }

    private _onPageChange = (e: CustomEvent<number>) => {
        this._currentPage = e.detail;
        void this._fetch();
    };

    private _languageFilter(e: Event, setter: (value: number | null) => void) {
        const value = (e.target as HTMLSelectElement).value;
        this._filterChange(() => setter(value === "" ? null : Number(value)));
    }

    private _cultureStates(content: ContentItem): CultureState[] {
        if (!content.cultureStates || content.cultureStates === "Invariant") return [];

        return content.cultureStates
            .split("|")
            .map((state) => {
                const [iso, available, published, edited, ...name] = state.split(":");
                return {
                    iso,
                    available: available === "1" || available === "True" || available === "true",
                    published: published === "1" || published === "True" || published === "true",
                    edited: edited === "1" || edited === "True" || edited === "true",
                    name: name.join(":")
                };
            })
            .filter((state) => state.iso);
    }

    private _renderCultureStates(content: ContentItem) {
        const states = this._cultureStates(content);

        if (!states.length) {
            return html`<span class="culture-chip invariant">Invariant</span>`;
        }

        return html`
            <div class="culture-states">
                ${states.map((state) => {
                    const statusClass = state.published ? "published" : state.available ? "draft" : "missing";
                    const title = state.published
                        ? state.edited
                            ? `${state.name || state.iso}: published with unpublished changes`
                            : `${state.name || state.iso}: published`
                        : state.available
                          ? `${state.name || state.iso}: draft only`
                          : `${state.iso}: missing`;
                    return html`
                        <span class=${`culture-chip ${statusClass} ${state.edited ? "edited" : ""}`} title=${title}>
                            ${state.iso}${state.edited ? "*" : ""}
                        </span>
                    `;
                })}
            </div>
        `;
    }

    private async _openDetails(content: ContentItem, e: Event) {
        const detail = await godmodeGet<ContentMediaDetail>(`content/${content.id}/detail`);
        const ancestorPath = this._ancestorPath(detail);

        openEvidenceDrawer(
            this,
            {
                title: `Content: ${detail.name}`,
                subtitle: `${detail.contentTypeName} (${detail.contentTypeAlias})`,
                summary: [
                    { label: "Id", value: detail.id },
                    { label: "Key", value: detail.key },
                    { label: "Document Type", value: detail.contentTypeName },
                    { label: "Alias", value: detail.contentTypeAlias },
                    { label: "Location", value: ancestorPath },
                    { label: "Depth", value: Math.max(detail.ancestors.length - 1, 0) },
                    { label: "Published", value: detail.state.published },
                    { label: "Edited", value: detail.state.edited },
                    { label: "Trashed", value: detail.trashed }
                ],
                sections: [
                    {
                        heading: "Location",
                        items: {
                            name: detail.name,
                            location: ancestorPath,
                            rawPath: detail.path,
                            parentId: detail.parentId,
                            level: detail.level,
                            created: truncate(detail.createDate, 22),
                            updated: truncate(detail.updateDate, 22)
                        }
                    },
                    {
                        heading: "Ancestor Path",
                        description: "Decoded from the Umbraco path IDs, including missing ancestors if an ID cannot be resolved.",
                        items: detail.ancestors.map((ancestor, index) => ({
                            position: index,
                            id: ancestor.id,
                            name: ancestor.name,
                            alias: ancestor.alias,
                            level: ancestor.level,
                            current: ancestor.isCurrent
                        }))
                    },
                    {
                        heading: "Publishing",
                        items: {
                            published: detail.state.published,
                            edited: detail.state.edited,
                            templateId: detail.state.templateId,
                            publishedVersionId: detail.state.publishedVersionId,
                            publishDate: truncate(detail.state.publishDate ?? "", 22),
                            availableCultures: detail.state.availableCultures,
                            publishedCultures: detail.state.publishedCultures,
                            editedCultures: detail.state.editedCultures
                        }
                    },
                    {
                        heading: "Recent Audit Trail",
                        description: "Latest entity audit entries from Umbraco, including the backoffice user where it can be resolved.",
                        items: detail.auditTrail.map((entry) => ({
                            action: entry.auditType,
                            user: entry.userName,
                            userId: entry.userId,
                            entityType: entry.entityType,
                            comment: entry.comment,
                            parameters: entry.parameters
                        }))
                    }
                ]
            },
            e
        );
    }

    private _ancestorPath(detail: ContentMediaDetail) {
        return detail.ancestors.length ? detail.ancestors.map((ancestor) => ancestor.name).join(" / ") : detail.path;
    }

    private async _explainSubject(content: ContentItem) {
        const detail = await godmodeGet<ContentMediaDetail>(`content/${content.id}/detail`);

        return {
            subjectType: "Umbraco content item",
            title: `${content.name} (${content.alias})`,
            data: {
                id: content.id,
                udi: content.udi,
                name: content.name,
                documentTypeAlias: content.alias,
                path: content.path,
                level: content.level,
                parentId: content.parentId,
                trashed: content.trashed,
                createDate: content.createDate,
                updateDate: content.updateDate,
                creatorName: content.creatorName,
                updaterName: content.updaterName,
                cultureStates: this._cultureStates(content)
            },
            context: {
                detail,
                detailSources: [
                    "IContentService.GetById",
                    "IRelationService parent/child relations",
                    "IAuditService.GetLogs",
                    "God Mode reference graph"
                ]
            }
        };
    }

    override render() {
        return html`
            <godmode-page
                heading="Content Browser"
                description="Browse, search and filter all content pages."
                show-reload
                @reload=${() => void this._fetch()}
            >
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Search</label>
                            <uui-input
                                type="search"
                                autocomplete="off"
                                autocorrect="off"
                                autocapitalize="off"
                                spellcheck="false"
                                placeholder="Filter by name, ID or key"
                                .value=${this._name}
                                @change=${(e: Event) => this._filterChange(() => (this._name = (e.target as HTMLInputElement).value))}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Doc Type</label>
                            <select @change=${(e: Event) => this._filterChange(() => (this._alias = (e.target as HTMLSelectElement).value))}>
                                <option value="">Any</option>
                                ${this._aliases.map((a) => html`<option value=${a}>${a}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Language</label>
                            <select @change=${(e: Event) => this._languageFilter(e, (value) => (this._languageId = value))}>
                                <option value="">Any</option>
                                <option value="-1">Invariant</option>
                                ${this._languages.map((l) => html`<option value=${l.id}>${l.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Missing</label>
                            <select @change=${(e: Event) => this._languageFilter(e, (value) => (this._missingLanguageId = value))}>
                                <option value="">Any</option>
                                ${this._languages.map((l) => html`<option value=${l.id}>${l.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Published In</label>
                            <select @change=${(e: Event) => this._languageFilter(e, (value) => (this._publishedLanguageId = value))}>
                                <option value="">Any</option>
                                ${this._languages.map((l) => html`<option value=${l.id}>${l.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Unpublished?</label>
                            <select @change=${(e: Event) => this._filterChange(() => (this._edited = (e.target as HTMLSelectElement).value as "any" | "yes" | "no"))}>
                                <option value="any">Any</option>
                                <option value="yes">Yes</option>
                                <option value="no">No</option>
                            </select>
                        </div>
                        <div>
                            <label>Trashed?</label>
                            <select @change=${(e: Event) => this._filterChange(() => (this._trashed = (e.target as HTMLSelectElement).value as "any" | "yes" | "no"))}>
                                <option value="any">Any</option>
                                <option value="no">No</option>
                                <option value="yes">Yes</option>
                            </select>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._page
                      ? html`
                            <div class="results-block">
                                <uui-table>
                                    <uui-table-head>
                                        <uui-table-head-cell>Name</uui-table-head-cell>
                                        <uui-table-head-cell>Doc Type</uui-table-head-cell>
                                        <uui-table-head-cell>Cultures</uui-table-head-cell>
                                        <uui-table-head-cell>Creator</uui-table-head-cell>
                                        <uui-table-head-cell>Updated</uui-table-head-cell>
                                        <uui-table-head-cell>Trashed?</uui-table-head-cell>
                                        <uui-table-head-cell>Actions</uui-table-head-cell>
                                    </uui-table-head>
                                    ${this._page.items.map(
                                        (c) => html`
                                            <uui-table-row>
                                                <uui-table-cell>
                                                    <a href=${editUrl("content", c.udi)} @click=${(e: Event) => openEditorModal(this, "content", c.udi, e)}
                                                        ><strong>${c.name}</strong></a
                                                    >
                                                </uui-table-cell>
                                                <uui-table-cell><code>${c.alias}</code></uui-table-cell>
                                                <uui-table-cell>${this._renderCultureStates(c)}</uui-table-cell>
                                                <uui-table-cell>${c.creatorName}</uui-table-cell>
                                                <uui-table-cell><small>${truncate(c.updateDate, 22)}</small></uui-table-cell>
                                                <uui-table-cell><godmode-yes-no .value=${c.trashed}></godmode-yes-no></uui-table-cell>
                                                <uui-table-cell class="action-cell">
                                                    <div class="action-wrap">
                                                        <uui-button compact look="secondary" label="Details" @click=${(e: Event) => void this._openDetails(c, e)}>Details</uui-button>
                                                        <godmode-ai-explain-host .subjectProvider=${() => this._explainSubject(c)}></godmode-ai-explain-host>
                                                    </div>
                                                </uui-table-cell>
                                            </uui-table-row>
                                        `
                                    )}
                                </uui-table>
                                <godmode-pager
                                    .currentPage=${this._page.currentPage}
                                    .totalPages=${this._page.totalPages}
                                    .totalItems=${this._page.totalItems}
                                    @page-change=${this._onPageChange}
                                ></godmode-pager>
                            </div>
                        `
                      : ""}
            </godmode-page>
        `;
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: minmax(220px, 2fr) repeat(6, minmax(130px, 1fr));
            gap: var(--uui-size-space-4);
            align-items: end;
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .filters uui-input {
            width: 100%;
        }
        .filters select {
            width: 100%;
            padding: var(--uui-size-space-2);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            color: var(--uui-color-text);
        }
        .results-block {
            margin-top: var(--uui-size-space-4);
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .culture-states {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-1);
        }
        .culture-chip {
            display: inline-flex;
            align-items: center;
            min-height: 22px;
            padding: 0 var(--uui-size-space-2);
            border-radius: 999px;
            background: var(--uui-color-surface-alt);
            color: var(--uui-color-text);
            font-size: 12px;
            line-height: 1;
            white-space: nowrap;
        }
        .culture-chip.published {
            background: var(--uui-color-positive-emphasis);
            color: var(--uui-color-positive-contrast);
        }
        .culture-chip.draft {
            background: var(--uui-color-warning-emphasis);
            color: var(--uui-color-warning-contrast);
        }
        .culture-chip.missing {
            border: 1px dashed var(--uui-color-border);
            color: var(--uui-color-text-alt);
            background: transparent;
        }
        .culture-chip.edited {
            box-shadow: inset 0 0 0 2px currentColor;
        }
        .culture-chip.invariant {
            color: var(--uui-color-text-alt);
        }
        .action-cell {
            text-align: right;
            width: 10rem;
        }
        .action-wrap {
            display: flex;
            align-items: center;
            justify-content: flex-end;
            gap: var(--uui-size-space-2);
            min-height: 2rem;
        }
        .action-wrap godmode-ai-explain-host {
            display: inline-flex;
            justify-content: flex-end;
        }
        @media (max-width: 1200px) {
            .filters {
                grid-template-columns: repeat(2, minmax(0, 1fr));
            }
        }
        @media (max-width: 700px) {
            .filters {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeContentBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-content-browser": GodModeContentBrowserElement;
    }
}
