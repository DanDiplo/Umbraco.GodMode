import { LitElement, css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { godmodeGet } from "../api/client";
import "../shared";
import type { MemberGroupModel, MemberModel, Page } from "../shared/types";
import { truncate } from "../shared/format";
import { editUrl, openEditorModal } from "../shared/edit-links";
import { openEvidenceDrawer } from "../shared/evidence-drawer";

@customElement("godmode-member-browser")
export class GodModeMemberBrowserElement extends UmbElementMixin(LitElement) {
    @state() private _page: Page<MemberModel> | null = null;
    @state() private _groups: MemberGroupModel[] = [];
    @state() private _memberTypes: MemberGroupModel[] = [];
    @state() private _loading = false;
    @state() private _currentPage = 1;
    @state() private _pageSize = 15;
    @state() private _search = "";
    @state() private _groupId: number | null = null;
    @state() private _memberTypeId: number | null = null;
    @state() private _isApproved: boolean | null = null;
    @state() private _isLockedOut: boolean | null = null;
    @state() private _usesTwoFactor: boolean | null = null;

    override connectedCallback(): void {
        super.connectedCallback();
        void this._loadLookups();
        void this._fetch();
    }

    private async _loadLookups() {
        try {
            const [groups, memberTypes] = await Promise.all([
                godmodeGet<MemberGroupModel[]>("member-groups"),
                godmodeGet<MemberGroupModel[]>("member-types")
            ]);
            this._groups = groups;
            this._memberTypes = memberTypes;
        } catch {
            // non-fatal
        }
    }

    private async _fetch() {
        this._loading = true;
        try {
            this._page = await godmodeGet<Page<MemberModel>>("members", {
                page: this._currentPage,
                pageSize: this._pageSize,
                search: this._search,
                groupId: this._groupId,
                memberTypeId: this._memberTypeId,
                isApproved: this._isApproved,
                isLockedOut: this._isLockedOut,
                usesTwoFactor: this._usesTwoFactor
            });
        } finally {
            this._loading = false;
        }
    }

    private _onPageChange = (e: CustomEvent<number>) => {
        this._currentPage = e.detail;
        void this._fetch();
    };

    private _setNullableNumber(value: string): number | null {
        return value === "" ? null : Number(value);
    }

    private _setNullableBoolean(value: string): boolean | null {
        return value === "" ? null : value === "true";
    }

    private _selectFilter(handler: (value: string) => void) {
        return (e: Event) => {
            handler((e.target as HTMLSelectElement).value);
            this._currentPage = 1;
            void this._fetch();
        };
    }

    private _groupNames(member: MemberModel): string[] {
        return member.groups
            ? member.groups
                  .split(",")
                  .map((g) => g.trim())
                  .filter(Boolean)
            : [];
    }

    private _openDetails(member: MemberModel, e: Event) {
        const groups = this._groupNames(member);

        openEvidenceDrawer(
            this,
            {
                title: `Member: ${member.name}`,
                subtitle: `${member.memberTypeName} (${member.memberTypeAlias})`,
                summary: [
                    { label: "Id", value: member.id },
                    { label: "Key", value: member.udi },
                    { label: "Username", value: member.username },
                    { label: "Email", value: member.email },
                    { label: "Member Type", value: member.memberTypeName },
                    { label: "Approved", value: member.isApproved },
                    { label: "Locked Out", value: member.isLockedOut },
                    { label: "2FA", value: member.usesTwoFactor }
                ],
                sections: [
                    {
                        heading: "Account",
                        items: {
                            name: member.name,
                            username: member.username,
                            email: member.email,
                            created: truncate(member.createDate, 22),
                            approved: member.isApproved,
                            lockedOut: member.isLockedOut,
                            usesTwoFactor: member.usesTwoFactor
                        }
                    },
                    {
                        heading: "Type",
                        items: {
                            memberTypeId: member.memberTypeId,
                            memberTypeName: member.memberTypeName,
                            memberTypeAlias: member.memberTypeAlias
                        }
                    },
                    {
                        heading: "Groups",
                        items: groups.length ? groups : ["None"]
                    }
                ]
            },
            e
        );
    }

    private _explainSubject(member: MemberModel) {
        return {
            subjectType: "Umbraco member",
            title: `${member.name} (${member.email})`,
            data: {
                id: member.id,
                udi: member.udi,
                name: member.name,
                username: member.username,
                email: member.email,
                memberTypeId: member.memberTypeId,
                memberTypeName: member.memberTypeName,
                memberTypeAlias: member.memberTypeAlias,
                groups: this._groupNames(member),
                isApproved: member.isApproved,
                isLockedOut: member.isLockedOut,
                usesTwoFactor: member.usesTwoFactor,
                createDate: member.createDate
            },
            context: {
                activeFilters: {
                    search: this._search,
                    groupId: this._groupId,
                    memberTypeId: this._memberTypeId,
                    isApproved: this._isApproved,
                    isLockedOut: this._isLockedOut,
                    usesTwoFactor: this._usesTwoFactor
                },
                availableGroups: this._groups,
                availableMemberTypes: this._memberTypes
            }
        };
    }

    override render() {
        return html`
            <godmode-page
                heading="Member Browser"
                description="Search members and filter by type, group and account status."
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
                                placeholder="Filter by name, email, username, ID or key"
                                .value=${this._search}
                                @change=${(e: Event) => {
                                    this._search = (e.target as HTMLInputElement).value;
                                    this._currentPage = 1;
                                    void this._fetch();
                                }}
                            ></uui-input>
                        </div>
                        <div>
                            <label>Member Type</label>
                            <select @change=${this._selectFilter((v) => (this._memberTypeId = this._setNullableNumber(v)))}>
                                <option value="">Any</option>
                                ${this._memberTypes.map((t) => html`<option value=${t.id}>${t.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Group</label>
                            <select @change=${this._selectFilter((v) => (this._groupId = this._setNullableNumber(v)))}>
                                <option value="">Any</option>
                                ${this._groups.map((g) => html`<option value=${g.id}>${g.name}</option>`)}
                            </select>
                        </div>
                        <div>
                            <label>Approved</label>
                            <select @change=${this._selectFilter((v) => (this._isApproved = this._setNullableBoolean(v)))}>
                                <option value="">Any</option>
                                <option value="true">Approved</option>
                                <option value="false">Not approved</option>
                            </select>
                        </div>
                        <div>
                            <label>Locked Out</label>
                            <select @change=${this._selectFilter((v) => (this._isLockedOut = this._setNullableBoolean(v)))}>
                                <option value="">Any</option>
                                <option value="true">Locked</option>
                                <option value="false">Not locked</option>
                            </select>
                        </div>
                        <div>
                            <label>2FA</label>
                            <select @change=${this._selectFilter((v) => (this._usesTwoFactor = this._setNullableBoolean(v)))}>
                                <option value="">Any</option>
                                <option value="true">Configured</option>
                                <option value="false">Not configured</option>
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
                                        <uui-table-head-cell>Username</uui-table-head-cell>
                                        <uui-table-head-cell>Email</uui-table-head-cell>
                                        <uui-table-head-cell>Type</uui-table-head-cell>
                                        <uui-table-head-cell>Groups</uui-table-head-cell>
                                        <uui-table-head-cell>Status</uui-table-head-cell>
                                        <uui-table-head-cell>Created</uui-table-head-cell>
                                        <uui-table-head-cell>Actions</uui-table-head-cell>
                                    </uui-table-head>
                                    ${this._page.items.map(
                                        (m) => html`
                                            <uui-table-row>
                                                <uui-table-cell>
                                                    <a href=${editUrl("member", m.udi)} @click=${(e: Event) => openEditorModal(this, "member", m.udi, e)}
                                                        ><strong>${m.name}</strong></a
                                                    >
                                                </uui-table-cell>
                                                <uui-table-cell>${m.username}</uui-table-cell>
                                                <uui-table-cell>${m.email}</uui-table-cell>
                                                <uui-table-cell><span class="pill">${m.memberTypeName}</span></uui-table-cell>
                                                <uui-table-cell>
                                                    <div class="tags">
                                                        ${this._groupNames(m).length
                                                            ? this._groupNames(m).map((g) => html`<span class="pill muted">${g}</span>`)
                                                            : html`<span class="muted-text">None</span>`}
                                                    </div>
                                                </uui-table-cell>
                                                <uui-table-cell>
                                                    <div class="tags">
                                                        <span class=${m.isApproved ? "pill ok" : "pill warn"}>
                                                            ${m.isApproved ? "Approved" : "Not approved"}
                                                        </span>
                                                        ${m.isLockedOut ? html`<span class="pill danger">Locked</span>` : ""}
                                                        ${m.usesTwoFactor ? html`<span class="pill">2FA</span>` : ""}
                                                    </div>
                                                </uui-table-cell>
                                                <uui-table-cell><small>${truncate(m.createDate, 22)}</small></uui-table-cell>
                                                <uui-table-cell class="action-cell">
                                                    <div class="action-wrap">
                                                        <uui-button compact look="secondary" label="Details" @click=${(e: Event) => this._openDetails(m, e)}>Details</uui-button>
                                                        <godmode-ai-explain-host .subject=${this._explainSubject(m)}></godmode-ai-explain-host>
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
            grid-template-columns: minmax(220px, 2fr) repeat(5, minmax(140px, 1fr));
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
        .tags {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-1);
        }
        .pill {
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
        .pill.muted {
            color: var(--uui-color-text-alt);
        }
        .pill.ok {
            background: var(--uui-color-positive-emphasis);
            color: var(--uui-color-positive-contrast);
        }
        .pill.warn {
            background: var(--uui-color-warning-emphasis);
            color: var(--uui-color-warning-contrast);
        }
        .pill.danger {
            background: var(--uui-color-danger-emphasis);
            color: var(--uui-color-danger-contrast);
        }
        .muted-text {
            color: var(--uui-color-text-alt);
            font-size: 12px;
        }
        .action-cell {
            text-align: right;
            width: 10rem;
        }
        .action-wrap {
            display: inline-flex;
            justify-content: flex-end;
            align-items: center;
            gap: var(--uui-size-space-2);
        }
        .action-cell godmode-ai-explain-host {
            display: inline-flex;
            justify-content: flex-end;
        }
        @media (max-width: 1100px) {
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

export default GodModeMemberBrowserElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-member-browser": GodModeMemberBrowserElement;
    }
}
