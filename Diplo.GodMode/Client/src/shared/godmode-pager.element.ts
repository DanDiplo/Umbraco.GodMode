import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";

@customElement("godmode-pager")
export class GodModePagerElement extends LitElement {
    @property({ type: Number }) currentPage = 1;
    @property({ type: Number }) totalPages = 1;
    @property({ type: Number }) totalItems = 0;

    private _onChange(e: Event) {
        const page = (e.target as HTMLElement & { current?: number }).current;
        if (typeof page === "number") {
            this._go(page);
        }
    }

    private _go(page: number) {
        if (page < 1 || page > this.totalPages || page === this.currentPage) return;
        this.dispatchEvent(new CustomEvent<number>("page-change", { detail: page, bubbles: true, composed: true }));
    }

    override render() {
        const totalPages = Math.max(1, this.totalPages);
        const currentPage = Math.min(Math.max(1, this.currentPage), totalPages);

        return html`
            <div class="pager">
                <span>${this.totalItems} items</span>
                <uui-pagination
                    label="GodMode pagination"
                    .current=${currentPage}
                    .total=${totalPages}
                    @change=${this._onChange}
                ></uui-pagination>
            </div>
        `;
    }

    static override styles = css`
        .pager {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: var(--uui-size-space-3);
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }

        uui-pagination {
            flex: 0 1 auto;
            min-width: min(100%, 560px);
        }

        @media (max-width: 640px) {
            .pager {
                align-items: flex-start;
                flex-direction: column;
            }

            uui-pagination {
                max-width: 100%;
                width: 100%;
            }
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-pager": GodModePagerElement;
    }
}
