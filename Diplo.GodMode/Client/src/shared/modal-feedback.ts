type ModalOpenAction = () => void | Promise<void>;

const OVERLAY_ID = "godmode-modal-opening-feedback";

export async function openWithModalFeedback(e: Event | undefined, action: ModalOpenAction): Promise<void> {
    e?.preventDefault();
    e?.stopPropagation();

    const feedback = e ? beginModalFeedback(e) : undefined;
    const overlay = showModalOpeningOverlay();

    try {
        await nextFrame();

        await action();
    } finally {
        feedback?.complete();
        overlay.complete();
    }
}

export async function runWithModalFeedback(e: Event | undefined, action: ModalOpenAction): Promise<void> {
    await openWithModalFeedback(e, action);
}

function beginModalFeedback(e: Event): { complete: () => void } | undefined {
    const button = e.composedPath().find(isButtonLikeElement);
    if (!button) return undefined;

    const previousDisabled = button.hasAttribute("disabled");
    const previousState = button.getAttribute("state");
    const previousLabel = button.getAttribute("label");

    button.setAttribute("disabled", "");
    button.setAttribute("state", "waiting");

    if (previousLabel) {
        button.setAttribute("label", `Opening ${previousLabel}`);
    }

    return {
        complete: () => {
            if (previousDisabled) {
                button.setAttribute("disabled", "");
            } else {
                button.removeAttribute("disabled");
            }

            restoreAttribute(button, "state", previousState);
            restoreAttribute(button, "label", previousLabel);
        }
    };
}

function isButtonLikeElement(value: EventTarget): value is HTMLElement {
    return value instanceof HTMLElement && (value.localName === "uui-button" || value.localName === "button");
}

function restoreAttribute(element: HTMLElement, name: string, value: string | null): void {
    if (value === null) {
        element.removeAttribute(name);
    } else {
        element.setAttribute(name, value);
    }
}

function nextFrame(): Promise<void> {
    return new Promise((resolve) => requestAnimationFrame(() => resolve()));
}

function showModalOpeningOverlay(): { complete: () => void } {
    const existing = document.getElementById(OVERLAY_ID);
    existing?.remove();

    const overlay = document.createElement("div");
    overlay.id = OVERLAY_ID;
    overlay.setAttribute("role", "status");
    overlay.setAttribute("aria-live", "polite");
    overlay.innerHTML = `
        <div class="godmode-modal-opening-panel">
            <uui-loader></uui-loader>
            <span>Opening modal...</span>
        </div>
    `;

    const style = document.createElement("style");
    style.textContent = `
        #${OVERLAY_ID} {
            position: fixed;
            inset: 0;
            z-index: 100000;
            display: grid;
            place-items: center;
            background: rgba(0, 0, 0, 0.18);
            pointer-events: none;
        }

        #${OVERLAY_ID} .godmode-modal-opening-panel {
            display: grid;
            grid-template-columns: auto 1fr;
            align-items: center;
            gap: var(--uui-size-space-3, 12px);
            padding: var(--uui-size-space-4, 18px) var(--uui-size-space-5, 24px);
            border-radius: var(--uui-border-radius, 3px);
            background: var(--uui-color-surface, #fff);
            color: var(--uui-color-text, #1b1b1b);
            box-shadow: var(--uui-shadow-depth-3, 0 16px 40px rgba(0, 0, 0, 0.24));
            font-weight: 700;
        }
    `;

    overlay.append(style);
    document.body.append(overlay);

    return {
        complete: () => overlay.remove()
    };
}
