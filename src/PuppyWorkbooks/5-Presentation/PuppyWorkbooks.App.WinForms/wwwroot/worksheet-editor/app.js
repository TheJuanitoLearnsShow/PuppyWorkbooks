import { sendToHost, initHostListener } from './host.js';
import { initRenderer, loadWorksheetFromHost, applyHostResult } from './renderer.js';

initRenderer(sendToHost);

initHostListener({
    onWorksheet: loadWorksheetFromHost,
    onResult: applyHostResult,
    onOtherMessage: msg => {
        const logElement = document.getElementById('log');
        if (logElement) {
            logElement.textContent += '\nFrom .NET: ' + JSON.stringify(msg);
        }
    }
});
