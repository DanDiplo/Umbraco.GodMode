import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeDelete, godmodeGet } from "../api/client";
import "../shared";
import { editUrl, openEditorModal } from "../shared/edit-links";
import type { ContentTags, Tag, TagMapping } from "../shared/types";

@customElement("godmode-tag-browser")
export class GodModeTagBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _tags: TagMapping[] = [];
    @state() private _orphans: Tag[] = [];
    @state() private _loading = true;
    @state() private _tagName = "";
    @state() private _tagGroup = "";
    @state() private _tagContent = "";

    override connectedCallback(): void {
        super.connectedCallback();
        void this._load();
    }

    private async _load() {
        this._loading = true;
        try {
            const [tags, orphans] = await Promise.all([
                godmodeGet<TagMapping[]>("tags"),
                godmodeGet<Tag[]>("tags/orphaned")
            ]);
            this._tags = tags;
            this._orphans = orphans;
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): TagMapping[] {
        const n = this._tagName.trim().toLowerCase();
        const g = this._tagGroup.trim().toLowerCase();
        const c = this._tagContent.trim().toLowerCase();
        return this._tags.filter((t) => {
            if (n && !t.tag.text.toLowerCase().includes(n)) return false;
            if (g && !t.tag.group.toLowerCase().includes(g)) return false;
            if (c && !(t.content ?? []).some((item) => this._matchesContent(item, c))) return false;
            return true;
        });
    }

    private _matchesContent(item: ContentTags, query: string): boolean {
        return [item.name, item.alias, item.type, String(item.id), item.udi, ...(item.tags ?? []).map((tag) => tag.text)]
            .filter(Boolean)
            .some((value) => value.toString().toLowerCase().includes(query));
    }

    private async _delete(tag: Tag) {
        if (!confirm(`Delete the tag '${tag.text}'?`)) return;
        try {
            await godmodeDelete<boolean>(`tags/${tag.id}`);
            await this._load();
        } catch (e) {
            console.error(e);
        }
    }

    override render() {
        const results = this._filtered();
        return html`
            <godmode-page heading="Tag Browser" description="View tags and the content using them." show-reload @reload=${() => void this._load()}>
                <uui-box>
                    <div class="filters">
                        <div>
                            <label>Tag Name</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by tag"
                                .value=${this._tagName}
                                @input=${(e: Event) => (this._tagName = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Tag Group</label>
                            <uui-input
                                type="search"
                                placeholder="Filter by group"
                                .value=${this._tagGroup}
                                @input=${(e: Event) => (this._tagGroup = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Content</label>
                            <uui-input
                                type="search"
                                placeholder="Filter tagged content"
                                .value=${this._tagContent}
                                @input=${(e: Event) => (this._tagContent = (e.target as HTMLInputElement).value)}
                            ></uui-input>
                        </div>
                    </div>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : html`
                          <p class="results"><strong>${results.length}</strong> / <strong>${this._tags.length}</strong></p>
                          ${results.map(
                              (m) => html`
                                  <uui-box class="tag-card">
                                      <div class="tag-headline">
                                          <span class="tag-title">
                                              <uui-icon name="icon-tag"></uui-icon>
                                              <strong title=${`ID: ${m.tag.id}`}>${m.tag.text}</strong>
                                              <code>${m.tag.nodeCount}</code>
                                              <span class="pill">${m.tag.group}</span>
                                              ${m.tag.culture ? html`<span class="pill">${m.tag.culture}</span>` : ""}
                                          </span>
                                          <span class="actions">
                                              <uui-button look="secondary" color="danger" label=${`Delete ${m.tag.text}`} @click=${() => void this._delete(m.tag)}>
                                                  Delete
                                              </uui-button>
                                              <godmode-ai-explain-host .subject=${this._explainSubject(m)}></godmode-ai-explain-host>
                                          </span>
                                      </div>
                                      ${(m.content ?? []).length
                                          ? this._renderContentTable(m.content ?? [])
                                          : html`<p class="empty">No matching tagged content.</p>`}
                                  </uui-box>
                              `
                          )}
                          ${this._orphans.length
                              ? html`
                                    <uui-box headline="Orphaned Tags" style="margin-top: var(--uui-size-space-4)">
                                        <p>Tags that exist in the database but aren't assigned to any content.</p>
                                        <uui-table>
                                            <uui-table-head>
                                                <uui-table-head-cell>Tag</uui-table-head-cell>
                                                <uui-table-head-cell>Group</uui-table-head-cell>
                                                <uui-table-head-cell>Action</uui-table-head-cell>
                                            </uui-table-head>
                                            ${this._orphans.map(
                                                (t) => html`
                                                    <uui-table-row>
                                                        <uui-table-cell><strong>${t.text}</strong></uui-table-cell>
                                                        <uui-table-cell>${t.group}</uui-table-cell>
                                                        <uui-table-cell>
                                                            <godmode-ai-explain-host .subject=${this._orphanExplainSubject(t)}></godmode-ai-explain-host>
                                                            <uui-button look="secondary" color="danger" label="Delete" @click=${() => void this._delete(t)}>Delete</uui-button>
                                                        </uui-table-cell>
                                                    </uui-table-row>
                                                `
                                            )}
                                        </uui-table>
                                    </uui-box>
                                `
                              : ""}
                      `}
            </godmode-page>
        `;
    }

    private _renderContentTable(items: ContentTags[]) {
        const q = this._tagContent.trim().toLowerCase();
        const filtered = q ? items.filter((item) => this._matchesContent(item, q)) : items;

        return html`
            <uui-table class="tag-content">
                ${filtered.map((item) => {
                    const editableType = item.type === "Media" ? "media" : "content";
                    return html`
                        <uui-table-row>
                            <uui-table-cell class="name-cell">
                                <a href=${editUrl(editableType, item.udi)} @click=${(e: Event) => openEditorModal(this, editableType, item.udi, e)}>
                                    <strong>${item.name}</strong>
                                </a>
                            </uui-table-cell>
                            <uui-table-cell class="type-cell">
                                <span class="type">
                                    ${item.icon ? html`<umb-icon name=${item.icon}></umb-icon>` : ""}
                                    ${item.alias}
                                </span>
                            </uui-table-cell>
                            <uui-table-cell class="tags-cell">
                                <span class="tags">${(item.tags ?? []).map((tag) => html`<span class="tag-pill">${tag.text}</span>`)}</span>
                            </uui-table-cell>
                            <uui-table-cell class="id-cell">
                                <span>${item.id}</span>
                                <code>${item.udi}</code>
                            </uui-table-cell>
                        </uui-table-row>
                    `;
                })}
            </uui-table>
        `;
    }

    private _explainSubject(mapping: TagMapping) {
        const content = mapping.content ?? [];
        return {
            subjectType: "Umbraco tag",
            title: `${mapping.tag.text} (${mapping.tag.group})`,
            data: {
                id: mapping.tag.id,
                text: mapping.tag.text,
                group: mapping.tag.group,
                culture: mapping.tag.culture,
                nodeCount: mapping.tag.nodeCount,
                taggedItemCount: content.length,
                taggedItems: content.slice(0, 25).map((item) => ({
                    id: item.id,
                    udi: item.udi,
                    name: item.name,
                    type: item.type,
                    alias: item.alias,
                    tags: (item.tags ?? []).map((tag) => tag.text)
                })),
                taggedItemSampleLimit: 25
            },
            context: {
                isOrphaned: false,
                relatedOrphanedTagsWithSameText: this._orphans.filter((tag) => tag.text === mapping.tag.text)
            }
        };
    }

    private _orphanExplainSubject(tag: Tag) {
        return {
            subjectType: "Orphaned Umbraco tag",
            title: `${tag.text} (${tag.group})`,
            data: {
                id: tag.id,
                text: tag.text,
                group: tag.group,
                culture: tag.culture,
                nodeCount: tag.nodeCount
            },
            context: {
                isOrphaned: true,
                matchingAssignedTags: this._tags.filter((mapping) => mapping.tag.text === tag.text)
            }
        };
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-4);
        }
        .filters label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        .filters uui-input {
            width: 100%;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        .tag-card {
            margin-top: var(--uui-size-space-4);
        }
        .tag-headline,
        .tag-title,
        .type,
        .tags {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }
        .tag-headline {
            justify-content: space-between;
            width: 100%;
            margin-bottom: var(--uui-size-space-4);
        }
        .actions {
            display: inline-flex;
            align-items: center;
            justify-content: flex-end;
            gap: var(--uui-size-space-2);
            margin-left: auto;
            flex: 0 0 auto;
        }
        .actions uui-button,
        .actions godmode-ai-explain-host {
            --uui-button-padding-left-factor: 1;
            --uui-button-padding-right-factor: 1;
        }
        .tag-title strong {
            font-size: 1.15rem;
        }
        .pill,
        .tag-pill {
            display: inline-flex;
            align-items: center;
            border-radius: 3px;
            padding: 1px var(--uui-size-space-1);
            font-size: 0.75rem;
            line-height: 1.3;
            background: var(--uui-color-border);
            color: var(--uui-color-text);
        }
        .tag-pill {
            background: var(--uui-color-text);
            color: var(--uui-color-surface);
            font-weight: 600;
        }
        .tag-content {
            margin-top: var(--uui-size-space-4);
        }
        .tag-content uui-table-row {
            display: grid;
            grid-template-columns: minmax(14rem, 2fr) minmax(8rem, 0.8fr) minmax(14rem, 1.4fr) minmax(18rem, 1.8fr);
            align-items: center;
        }
        .tag-content uui-table-cell {
            min-width: 0;
        }
        .tag-content .name-cell,
        .tag-content .type-cell,
        .tag-content .tags-cell,
        .tag-content .id-cell {
            width: auto;
        }
        .tag-content .name-cell a,
        .tag-content .type,
        .tag-content .tags,
        .tag-content .id-cell code {
            min-width: 0;
            overflow-wrap: anywhere;
        }
        .tag-content .tags {
            flex-wrap: wrap;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        uui-table-cell code {
            display: block;
            width: fit-content;
            margin-top: var(--uui-size-space-1);
        }
        .empty {
            color: var(--uui-color-text-alt);
            margin: var(--uui-size-space-4) 0 0;
        }
        @media (max-width: 900px) {
            .filters {
                grid-template-columns: 1fr;
            }
            .tag-content uui-table-row {
                grid-template-columns: 1fr;
                gap: var(--uui-size-space-1);
                padding: var(--uui-size-space-3) 0;
            }
        }
    `;
}

export default GodModeTagBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-tag-browser": GodModeTagBrowserElement;
    }
}
