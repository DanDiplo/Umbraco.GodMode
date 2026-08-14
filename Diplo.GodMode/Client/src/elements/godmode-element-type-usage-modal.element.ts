import { LitElement, css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import type { ElementTypeUsageDetail, ElementTypeUsageEntityType, ElementTypeUsageSourceType } from "../shared/types";
import type { GodModeElementTypeUsageModalData } from "../shared/element-type-usage-modal";
import { editUrl } from "../shared/edit-links";
import { truncate } from "../shared/format";
import "../shared";

const SOURCE_GROUPS: Array<{ sourceTypes: ElementTypeUsageSourceType[]; heading: string; description: string }> = [
    {
        sourceTypes: ["BlockContent"],
        heading: "Block Content",
        description: "Embedded as content in a Block List / Block Grid property."
    },
    {
        sourceTypes: ["BlockSettings"],
        heading: "Block Settings",
        description: "Embedded as settings in a Block List / Block Grid property."
    },
    {
        sourceTypes: ["ElementPicker"],
        heading: "Element Picker",
        description: "Referenced by an Element Picker property (Umbraco 18+)."
    },
    {
        sourceTypes: ["LibraryItem", "LibraryItem (Trashed)"],
        heading: "Library Items",
        description: "Standing Library items of this Element Type (Umbraco 18+)."
    }
];

@customElement("godmode-element-type-usage-modal")
export class GodModeElementTypeUsageModalElement extends UmbElementMixin(LitElement) {
    @property({ attribute: false }) modalContext?: { reject: () => void };
    @property({ type: Object, attribute: false }) data?: GodModeElementTypeUsageModalData;
    @state() private _items: ElementTypeUsageDetail[] = [];
    @state() private _loading = true;
    @state() private _search = "";
    @state() private _error = "";

    override connectedCallback(): void {
        super.connectedCallback();
        window.addEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
        void this._load();
    }

    override disconnectedCallback(): void {
        window.removeEventListener("pointerdown", this._onOutsidePointerDown, { capture: true });
        super.disconnectedCallback();
    }

    private async _load() {
        if (!this.data?.elementTypeKey) {
            this._loading = false;
            return;
        }

        this._loading = true;
        this._error = "";
        try {
            this._items = await godmodeGet<ElementTypeUsageDetail[]>("element-type-usage/detail", {
                elementTypeKey: this.data.elementTypeKey
            });
        } catch (error) {
            this._error = error instanceof Error ? error.message : "Unable to load Element Type usage.";
        } finally {
            this._loading = false;
        }
    }

    private _filtered(): ElementTypeUsageDetail[] {
        const q = this._search.trim().toLowerCase();
        if (!q) return this._items;
        return this._items.filter((item) =>
            [item.contentName, item.parentName ?? "", item.contentPath, item.sourceType].some((value) => value.toLowerCase().includes(q))
        );
    }

    private _close = () => {
        this.modalContext?.reject();
    };

    private _onOutsidePointerDown = (e: PointerEvent) => {
        if (e.composedPath().includes(this)) {
            return;
        }

        this._close();
    };

    private _usageEditUrl(entityType: ElementTypeUsageEntityType, key: string): string {
        switch (entityType) {
            case "content":
                return editUrl("content", key);
            case "media":
                return editUrl("media", key);
            case "member":
                return editUrl("member", key);
            case "element":
                // Umbraco 18+ Library item. GodMode's client package targets Umbraco 17
                // (@umbraco-cms/backoffice ^17.3.0), which has no typed path pattern for the
                // Library section yet, so the route is built manually here rather than via
                // the shared edit-links helpers. This matches the route Umbraco 18's `elements`
                // package registers: section/library/workspace/element/edit/{key}.
                return `section/library/workspace/element/edit/${key}`;
            default:
                return "";
        }
    }

    override render() {
        const target = this.data?.elementTypeName || this.data?.elementTypeAlias || this.data?.elementTypeKey || "Selected Element Type";
        const results = this._filtered();

        return html`
            <godmode-modal-layout headline=${`Element Type Usage: ${target}`} @close=${this._close}>
                <div class="summary">
                    ${this.data?.elementTypeAlias ? html`<code>${this.data.elementTypeAlias}</code>` : ""}
                    ${this.data?.elementTypeKey ? html`<small><code>${this.data.elementTypeKey}</code></small>` : ""}
                </div>

                <uui-box>
                    <label>Search</label>
                    <uui-input
                        type="search"
                        autocomplete="off"
                        autocorrect="off"
                        autocapitalize="off"
                        spellcheck="false"
                        placeholder="Filter by name, parent, path or source"
                        .value=${this._search}
                        @input=${(e: Event) => (this._search = (e.target as HTMLInputElement).value)}
                    ></uui-input>
                </uui-box>

                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._error
                      ? html`<uui-box><p class="empty">${this._error}</p></uui-box>`
                      : html`
                            <p class="results"><strong>${results.length}</strong> / <strong>${this._items.length}</strong> usages</p>
                            ${results.length ? SOURCE_GROUPS.map((group) => this._renderGroup(group, results)) : html`<uui-box><p>No usages found. This Element Type is unused.</p></uui-box>`}
                        `}
            </godmode-modal-layout>
        `;
    }

    private _renderGroup(group: (typeof SOURCE_GROUPS)[number], results: ElementTypeUsageDetail[]) {
        const rows = results.filter((item) => group.sourceTypes.includes(item.sourceType));
        if (!rows.length) return "";

        return html`
            <uui-box headline=${`${group.heading} (${rows.length})`}>
                <p class="section-description">${group.description}</p>
                <uui-table>
                    <uui-table-head>
                        <uui-table-head-cell>Content Name</uui-table-head-cell>
                        <uui-table-head-cell>Parent Name</uui-table-head-cell>
                        <uui-table-head-cell>Version Date</uui-table-head-cell>
                        <uui-table-head-cell>Source Type</uui-table-head-cell>
                    </uui-table-head>
                    ${rows.map((row) => {
                        const link = this._usageEditUrl(row.entityType, row.contentKey);
                        return html`
                            <uui-table-row>
                                <uui-table-cell>
                                    ${link
                                        ? html`<a href=${link} target="_blank" rel="noopener noreferrer"><strong>${row.contentName}</strong></a>`
                                        : html`<strong>${row.contentName}</strong>`}
                                </uui-table-cell>
                                <uui-table-cell>${row.parentName ?? html`<span class="muted">None</span>`}</uui-table-cell>
                                <uui-table-cell><small>${truncate(row.versionDate, 22)}</small></uui-table-cell>
                                <uui-table-cell><span class="pill">${row.sourceType}</span></uui-table-cell>
                            </uui-table-row>
                        `;
                    })}
                </uui-table>
            </uui-box>
        `;
    }

    static override styles = css`
        .summary {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: var(--uui-size-space-2);
            margin-bottom: var(--uui-size-space-4);
        }
        label {
            display: block;
            font-weight: 600;
            margin-bottom: var(--uui-size-space-1);
        }
        uui-input {
            width: 100%;
        }
        .results {
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
        .section-description {
            color: var(--uui-color-text-alt);
            margin-top: 0;
        }
        uui-box + uui-box {
            margin-top: var(--uui-size-space-4);
        }
        .muted {
            color: var(--uui-color-text-alt);
        }
        .empty {
            color: var(--uui-color-text-alt);
        }
        .pill {
            display: inline-block;
            padding: 2px 8px;
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface-alt);
            font-size: 12px;
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: none;
        }
        a:hover {
            text-decoration: underline;
        }
    `;
}

export default GodModeElementTypeUsageModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-element-type-usage-modal": GodModeElementTypeUsageModalElement;
    }
}
