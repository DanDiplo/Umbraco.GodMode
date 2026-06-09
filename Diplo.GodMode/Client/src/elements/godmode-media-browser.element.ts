import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import "@umbraco-cms/backoffice/imaging";
import { godmodeGet } from "../api/client";
import "../shared";
import type { ContentMediaDetail, ItemBase, MediaMap, Page } from "../shared/types";
import { formatBytes, truncate } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openLazyEvidenceDrawer } from "../shared/evidence-drawer";

@customElement("godmode-media-browser")
export class GodModeMediaBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _page: Page<MediaMap> | null = null;
    @state() private _mediaTypes: ItemBase[] = [];
    @state() private _loading = false;
    @state() private _currentPage = 1;
    @state() private _pageSize = 20;
    @state() private _name = "";
    @state() private _mediaTypeId: number | null = null;
    @state() private _minSizeMb = 0;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadFilters();
        void this._fetch();
    }

    private async _loadFilters() {
        try {
            this._mediaTypes = await godmodeGet<ItemBase[]>("media-types");
        } catch {
            // non-fatal
        }
    }

    private async _fetch() {
        this._loading = true;
        try {
            this._page = await godmodeGet<Page<MediaMap>>("media", {
                page: this._currentPage,
                pageSize: this._pageSize,
                name: this._name,
                mediaTypeId: this._mediaTypeId,
                minSizeBytes: this._minSizeBytes || undefined
            });
        } finally {
            this._loading = false;
        }
    }

    private get _minSizeBytes() {
        return Math.max(0, Math.round(this._minSizeMb * 1024 * 1024));
    }

    private _setSizeFilter(value: number) {
        this._minSizeMb = Math.max(0, Math.min(30, value));
        this._currentPage = 1;
        void this._fetch();
    }

    private _onPageChange = (e: CustomEvent<number>) => {
        this._currentPage = e.detail;
        void this._fetch();
    };

    private _openDetails(media: MediaMap, e: Event) {
        openLazyEvidenceDrawer(
            this,
            {
                title: `Media: ${media.name}`,
                subtitle: media.type,
                sections: [],
                load: async () => {
                    const detail = await godmodeGet<ContentMediaDetail>(`media/${media.id}/detail`);
                    const ancestorPath = this._ancestorPath(detail);

                    return {
                        title: `Media: ${detail.name}`,
                        subtitle: `${detail.contentTypeName} (${detail.mediaFile?.fileType || media.type})`,
                        summary: [
                            { label: "Id", value: detail.id },
                            { label: "Key", value: detail.key },
                            { label: "Media Type", value: detail.contentTypeName },
                            { label: "Alias", value: detail.contentTypeAlias },
                            { label: "Location", value: ancestorPath },
                            { label: "Depth", value: Math.max(detail.ancestors.length - 1, 0) },
                            { label: "Extension", value: detail.mediaFile?.extension || media.ext },
                            { label: "Size", value: formatBytes(detail.mediaFile?.size ?? media.size) },
                            { label: "Updated", value: truncate(detail.updateDate, 22) },
                            { label: "Trashed", value: detail.trashed }
                        ],
                        sections: [
                            {
                                heading: "File",
                                items: {
                                    name: detail.name,
                                    fileType: detail.mediaFile?.fileType || media.type,
                                    extension: detail.mediaFile?.extension || media.ext,
                                    size: formatBytes(detail.mediaFile?.size ?? media.size),
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
                                heading: "Properties",
                                description: "Property metadata from the media item. Values are summarized to avoid dumping large file JSON payloads.",
                                items: detail.properties.map((property) => ({
                                    name: property.name,
                                    alias: property.alias,
                                    editor: property.editorAlias,
                                    storage: property.storageType,
                                    values: property.valueCount,
                                    edited: property.hasEditedValue,
                                    published: property.hasPublishedValue,
                                    cultures: property.cultures
                                }))
                            },
                            {
                                heading: "Incoming Relations",
                                items: detail.incomingRelations.map((relation) => ({
                                    type: relation.relationTypeName || relation.relationTypeAlias,
                                    relatedId: relation.relatedId,
                                    relatedName: relation.relatedName,
                                    relatedKey: relation.relatedKey,
                                    relatedPath: relation.relatedPath,
                                    comment: relation.comment
                                }))
                            },
                            {
                                heading: "Outgoing Relations",
                                items: detail.outgoingRelations.map((relation) => ({
                                    type: relation.relationTypeName || relation.relationTypeAlias,
                                    relatedId: relation.relatedId,
                                    relatedName: relation.relatedName,
                                    relatedKey: relation.relatedKey,
                                    relatedPath: relation.relatedPath,
                                    comment: relation.comment
                                }))
                            },
                            {
                                heading: "Used By",
                                items: detail.usedBy.map((edge) => ({
                                    sourceType: edge.sourceType,
                                    sourceName: edge.sourceName,
                                    relation: edge.relation,
                                    targetType: edge.targetType,
                                    targetName: edge.targetName,
                                    context: edge.context
                                }))
                            },
                            {
                                heading: "Uses",
                                items: detail.uses.map((edge) => ({
                                    sourceType: edge.sourceType,
                                    sourceName: edge.sourceName,
                                    relation: edge.relation,
                                    targetType: edge.targetType,
                                    targetName: edge.targetName,
                                    context: edge.context
                                }))
                            },
                            {
                                heading: "Audit Trail",
                                items: detail.auditTrail.map((entry) => ({
                                    type: entry.auditType,
                                    entityType: entry.entityType,
                                    user: entry.userName,
                                    userId: entry.userId,
                                    comment: entry.comment,
                                    parameters: entry.parameters
                                }))
                            }
                        ]
                    };
                }
            },
            e
        );
    }

    private _ancestorPath(detail: ContentMediaDetail) {
        return detail.ancestors.length ? detail.ancestors.map((ancestor) => ancestor.name).join(" / ") : detail.path;
    }

    private _isImage(media: MediaMap) {
        return media.mediaTypeAlias?.toLowerCase() === "image";
    }

    private _isVectorImage(media: MediaMap) {
        return media.ext?.toLowerCase() === "svg";
    }

    override render() {
        return html`
            <godmode-page
                heading="Media Browser"
                description="Search media items and filter by type."
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
                                @change=${(e: Event) => {
                                    this._name = (e.target as HTMLInputElement).value;
                                    this._currentPage = 1;
                                    void this._fetch();
                                }}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Media Type</label>
                            <select
                                @change=${(e: Event) => {
                                    const v = (e.target as HTMLSelectElement).value;
                                    this._mediaTypeId = v === "" ? null : Number(v);
                                    this._currentPage = 1;
                                    void this._fetch();
                                }}
                            >
                                <option value="">Any</option>
                                ${this._mediaTypes.map((mt) => html`<option value=${mt.id}>${mt.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Minimum Size</label>
                            <div class="size-filter">
                                <input
                                    type="range"
                                    min="0"
                                    max="30"
                                    step="1"
                                    .value=${String(this._minSizeMb)}
                                    @change=${(e: Event) => this._setSizeFilter(Number((e.target as HTMLInputElement).value))}
                                />
                                <uui-input
                                    type="number"
                                    min="0"
                                    max="30"
                                    step="1"
                                    label="Minimum size in MB"
                                    .value=${String(this._minSizeMb)}
                                    @change=${(e: Event) => this._setSizeFilter(Number((e.target as HTMLInputElement).value || 0))}
                                ></uui-input>
                                <span>${this._minSizeMb > 0 ? formatBytes(this._minSizeBytes) : "Any"}</span>
                            </div>
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
                                        <uui-table-head-cell class="thumbnail-head">Thumbnail</uui-table-head-cell>
                                        <uui-table-head-cell>Type</uui-table-head-cell>
                                        <uui-table-head-cell>Ext</uui-table-head-cell>
                                        <uui-table-head-cell>Size</uui-table-head-cell>
                                        <uui-table-head-cell>Updated</uui-table-head-cell>
                                        <uui-table-head-cell class="action-head">Actions</uui-table-head-cell>
                                    </uui-table-head>
                                    ${this._page.items.map(
                                        (m) => html`
                                            <uui-table-row>
                                                <uui-table-cell>
                                                    <a href=${editUrl("media", m.udi)} @click=${(e: Event) => openEditorModal(this, "media", m.udi, e)}
                                                        ><strong>${m.name}</strong></a
                                                    >
                                                </uui-table-cell>
                                                <uui-table-cell class="thumbnail-cell">${this._renderThumbnail(m)}</uui-table-cell>
                                                <uui-table-cell><code>${m.type}</code></uui-table-cell>
                                                <uui-table-cell><code>${m.ext ?? ""}</code></uui-table-cell>
                                                <uui-table-cell>${formatBytes(m.size)}</uui-table-cell>
                                                <uui-table-cell><small>${truncate(m.updateDate, 22)}</small></uui-table-cell>
                                                <uui-table-cell class="action-cell">
                                                    <div class="action-wrap">
                                                        <uui-button compact look="secondary" label="Details" @click=${(e: Event) => void this._openDetails(m, e)}>Details</uui-button>
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

    private _renderThumbnail(media: MediaMap) {
        if (this._isVectorImage(media)) {
            return html`
                <span class="thumbnail-frame">
                    <umb-media-image
                        class="thumbnail"
                        .unique=${media.udi}
                        .alt=${media.name}
                        loading="lazy"
                        icon="icon-picture"
                    ></umb-media-image>
                </span>
            `;
        }

        if (!this._isImage(media)) {
            return this._renderMediaTypeIcon(media);
        }

        return html`
            <umb-imaging-thumbnail
                class="thumbnail"
                .unique=${media.udi}
                .width=${64}
                .height=${64}
                .alt=${media.name}
                loading="lazy"
                icon="icon-picture"
            ></umb-imaging-thumbnail>
        `;
    }

    private _renderMediaTypeIcon(media: MediaMap) {
        return html`
            <span class="media-type-icon" title=${media.alias}>
                <umb-icon name=${media.mediaTypeIcon || "icon-document"}></umb-icon>
            </span>
        `;
    }

    static override styles = css`
        .filters {
            display: grid;
            grid-template-columns: minmax(220px, 2fr) minmax(160px, 1fr) minmax(260px, 1.5fr);
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
        .size-filter {
            display: grid;
            grid-template-columns: minmax(120px, 1fr) 4.5rem auto;
            gap: var(--uui-size-space-2);
            align-items: center;
        }
        .size-filter input[type="range"] {
            width: 100%;
        }
        .size-filter uui-input {
            width: 4.5rem;
        }
        .size-filter span {
            color: var(--uui-color-text-alt);
            white-space: nowrap;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
        .thumbnail-head,
        .thumbnail-cell {
            width: 4.5rem;
        }
        .thumbnail-cell {
            padding-block: var(--uui-size-space-2);
        }
        .thumbnail {
            display: block;
            width: 3rem;
            height: 3rem;
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
            overflow: hidden;
        }
        .thumbnail-frame {
            display: block;
            width: 3rem;
            height: 3rem;
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
            overflow: hidden;
        }
        umb-media-image.thumbnail::part(img) {
            width: 100%;
            height: 100%;
            object-fit: contain;
            object-position: center;
        }
        .media-type-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 3rem;
            height: 3rem;
            color: var(--uui-color-text-alt);
        }
        .media-type-icon umb-icon {
            font-size: var(--uui-size-8);
        }
        .action-cell {
            width: 7rem;
            vertical-align: middle;
            text-align: right;
        }
        .action-head {
            width: 7rem;
            text-align: right;
        }
        .action-wrap {
            display: flex;
            align-items: center;
            justify-content: flex-end;
            min-height: 3rem;
        }
    `;
}

export default GodModeMediaBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-media-browser": GodModeMediaBrowserElement;
    }
}
