export const GODMODE_AI_EXPLAIN_BUTTON_TAG = "godmode-ai-explain-button";

export function isGodModeAiExplainAvailable(): boolean {
    return customElements.get(GODMODE_AI_EXPLAIN_BUTTON_TAG) !== undefined;
}

export function observeGodModeAiExplainAvailability(callback: () => void): () => void {
    if (isGodModeAiExplainAvailable()) {
        callback();
        return () => undefined;
    }

    let disposed = false;
    void customElements.whenDefined(GODMODE_AI_EXPLAIN_BUTTON_TAG).then(() => {
        if (!disposed) callback();
    });

    return () => {
        disposed = true;
    };
}
