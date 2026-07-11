import { sendToHost, initHostListener } from './host.js';
import { initRenderer, loadWorksheetFromHost, applyHostResult } from './renderer.js';
import { fluentButton, provideFluentDesignSystem, fluentTextField, fluentCard, fluentTextArea } from '@fluentui/web-components';

provideFluentDesignSystem().register(fluentButton(), fluentTextField(), fluentCard(), fluentTextArea());
initRenderer(sendToHost);

initHostListener({
    onWorksheet: loadWorksheetFromHost,
    onResult: applyHostResult,
    onOtherMessage: (msg: unknown) => {
        const logElement = document.getElementById('log');
        if (logElement) {
            logElement.textContent += '\nFrom .NET: ' + JSON.stringify(msg);
        }
    }
});
