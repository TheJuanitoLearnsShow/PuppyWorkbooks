export function sendToHost(message) {
    if (window.chrome && chrome.webview && chrome.webview.postMessage) {
        chrome.webview.postMessage(message);
    } else {
        console.warn('Host postMessage unavailable:', message);
    }
}

export function initHostListener({ onWorksheet, onResult, onOtherMessage }) {
    if (!window.chrome || !chrome.webview || !chrome.webview.addEventListener) {
        console.warn('Host listener unavailable.');
        return;
    }

    chrome.webview.addEventListener('message', event => {
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
            onResult?.(msg);
            return;
        }

        onOtherMessage?.(msg);
    });
}
