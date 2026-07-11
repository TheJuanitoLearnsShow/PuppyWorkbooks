declare global {
    interface Window {
        chrome?: {
            webview?: {
                postMessage?: (message: unknown) => void;
                addEventListener?: (type: string, listener: (event: MessageEvent) => void) => void;
            };
        };
    }
}

export function sendToHost(message: unknown) {
    const webview = window.chrome?.webview;
    if (webview?.postMessage) {
        webview.postMessage(message);
    } else {
        console.warn('Host postMessage unavailable:', message);
    }
}

export function initHostListener({ onWorksheet, onResult, onOtherMessage }: {
    onWorksheet?: (msg: any) => void;
    onResult?: (msg: any) => void;
    onOtherMessage?: (msg: unknown) => void;
}) {
    const webview = window.chrome?.webview;
    if (!webview?.addEventListener) {
        console.warn('Host listener unavailable.');
        return;
    }

    webview.addEventListener('message', (event: MessageEvent) => {
        let msg = event.data;
        if (typeof msg === 'string') {
            try {
                msg = JSON.parse(msg);
            } catch (err) {
                console.warn('Received non-JSON string from host:', msg);
            }
        }

        if (msg && (msg.Name || msg.name) && (Array.isArray(msg.Cells) || Array.isArray(msg.cells) || Array.isArray(msg.formulas))) {
            onWorksheet?.(msg);
            return;
        }

        if (msg && msg.type === 'worksheet' && msg.payload) {
            onWorksheet?.(msg.payload);
            return;
        }

        if (msg && (msg.type === 'cellResult' || msg.type === 'formulaResult' || msg.type === 'result')) {
            onResult?.(msg.payload);
            return;
        }

        onOtherMessage?.(msg);
    });
}
