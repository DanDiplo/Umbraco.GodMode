import { LitElement, css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";

@customElement("godmode-pager")
export class GodModePagerElement extends LitElement {
    @property({ type: Number }) currentPage = 1;
    @property({ type: Number }) totalPages = 1;
    @property({ type: Number }) totalItems = 0;

    private _go(page: number) {
        if (page < 1 || page > this.totalPages || page === this.currentPage) return;
        this.dispatchEvent(new CustomEvent<number>("page-change", { detail: page, bubbles: true, composed: true }));
    }

    override render() {
        const prev = Math.max(1, this.currentPage - 1);
        const next = Math.min(this.totalPages, this.currentPage + 1);
        return html`
            <div class="pager">
                <span>${this.totalItems} items</span>
                <span>
                    <uui-button look="secondary" ?disabled=${this.currentPage <= 1} @click=${() => this._go(prev)}>‹ Prev</uui-button>
                    Page ${this.currentPage} of ${this.totalPages}
                    <uui-button look="secondary" ?disabled=${this.currentPage >= this.totalPages} @click=${() => this._go(next)}>Next ›</uui-button>
                </span>
            </div>
        `;
    }

    static override styles = css`
        .pager {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: var(--uui-size-space-3) 0;
            color: var(--uui-color-text-alt);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "godmode-pager": GodModePagerElement;
    }
}
