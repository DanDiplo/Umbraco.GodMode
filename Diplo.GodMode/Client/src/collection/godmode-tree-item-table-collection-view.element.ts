import { css, customElement, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UMB_SECTION_CONTEXT } from "@umbraco-cms/backoffice/section";
import type { UmbTableColumn, UmbTableConfig, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { GODMODE_TREE_FOLDER_ENTITY_TYPE } from "../constants";
import { browsers } from "../manifests/catalog";
import type { GodModeTreeItemModel } from "../tree/types";

const DESCRIPTIONS = new Map<string, string>(browsers.map((b) => [b.id, b.description ?? ""]));

/**
 * Table collection view for the GodMode root/folder workspaces. Each row links
 * to the matching destination: folders open their own collection workspace,
 * leaves open the existing standalone browser workspace.
 */
@customElement("godmode-tree-item-table-collection-view")
export class GodModeTreeItemTableCollectionViewElement extends UmbLitElement {
    @state()
    private _tableConfig: UmbTableConfig = { allowSelection: false };

    @state()
    private _tableColumns: Array<UmbTableColumn> = [
        { name: "Name", alias: "name" },
        { name: "Description", alias: "description" }
    ];

    @state()
    private _tableItems: Array<UmbTableItem> = [];

    @state()
    private _pathname?: string;

    #items: GodModeTreeItemModel[] = [];

    constructor() {
        super();

        this.consumeContext(UMB_SECTION_CONTEXT, (ctx) => {
            this.observe(
                ctx?.pathname,
                (pathname: string | undefined) => {
                    this._pathname = pathname;
                    this.#createItems();
                },
                "observePathname"
            );
        });

        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            this.observe(
                ctx?.items,
                (items: GodModeTreeItemModel[] | undefined) => {
                    this.#items = items ?? [];
                    this.#createItems();
                },
                "observeItems"
            );
        });
    }

    #hrefFor(item: GodModeTreeItemModel): string | undefined {
        if (!this._pathname) return undefined;
        if (item.isFolder || item.entityType === GODMODE_TREE_FOLDER_ENTITY_TYPE) {
            return `section/${this._pathname}/workspace/${GODMODE_TREE_FOLDER_ENTITY_TYPE}/edit/${item.unique}`;
        }
        const workspaceEntityType = item.workspaceEntityType;
        return workspaceEntityType ? `section/${this._pathname}/workspace/${workspaceEntityType}` : undefined;
    }

    #createItems() {
        this._tableItems = this.#items.map((item) => ({
            id: String(item.unique),
            icon: item.icon ?? (item.isFolder ? "icon-folder" : "icon-document"),
            data: [
                {
                    columnAlias: "name",
                    value: html`<uui-button
                        look="default"
                        href=${this.#hrefFor(item) ?? ""}
                        label=${item.name}></uui-button>`
                },
                {
                    columnAlias: "description",
                    value: DESCRIPTIONS.get(String(item.unique)) ?? ""
                }
            ]
        }));
    }

    override render() {
        return html`<umb-table
            .config=${this._tableConfig}
            .columns=${this._tableColumns}
            .items=${this._tableItems}></umb-table>`;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                flex-direction: column;
            }
        `
    ];
}

export default GodModeTreeItemTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-tree-item-table-collection-view": GodModeTreeItemTableCollectionViewElement;
    }
}
