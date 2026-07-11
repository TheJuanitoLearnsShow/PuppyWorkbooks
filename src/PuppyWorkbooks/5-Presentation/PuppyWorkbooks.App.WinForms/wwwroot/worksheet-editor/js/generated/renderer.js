export const formulas = [
    {
        name: 'Quadratic Formula',
        formula: 'x = (-b ± √(b² - 4ac)) / 2a',
        comments: 'Solutions of ax² + bx + c = 0',
        result: '-1, -0.5'
    },
    {
        name: 'Pythagorean Theorem',
        formula: 'a² + b² = c²',
        comments: 'Right-angled triangles',
        result: '5'
    },
    {
        name: 'Area of a Circle',
        formula: 'A = πr²',
        comments: 'r = radius',
        result: '153.9'
    },
    {
        name: "Euler's Formula",
        formula: 'e^(iπ) + 1 = 0',
        comments: 'Relation to complex exponentials',
        result: '0'
    }
];
export let selectedIndex = null;
const refs = {};
let monacoEditor = null;
export function initRenderer(sendToHost) {
    initRefs();
    initMonaco();
    attachDomListeners(sendToHost);
    renderList();
}
function initRefs() {
    refs.listContainer = document.getElementById('formulaList');
    refs.editPanel = document.getElementById('editPanel');
    refs.addBtn = document.getElementById('addBtn');
    refs.nameField = document.getElementById('nameField');
    refs.formulaEditorDiv = document.getElementById('formulaEditor');
    refs.commentsField = document.getElementById('commentsField');
    refs.resultField = document.getElementById('resultField');
    refs.removeBtn = document.getElementById('removeBtn');
    refs.worksheetNameField = document.getElementById('worksheetNameField');
    refs.runSelectedBtn = document.getElementById('runSelectedBtn');
    refs.runAllBtn = document.getElementById('runAllBtn');
}
function initMonaco() {
    if (typeof require === 'undefined')
        return;
    if (require && require.config) {
        try {
            require.config({ paths: { vs: 'https://unpkg.com/monaco-editor@0.41.0/min/vs' } });
        }
        catch (e) {
            // ignore if already configured
        }
    }
    require(['vs/editor/editor.main'], function () {
        try {
            monaco.languages.register({ id: 'powerfx' });
            monaco.languages.setMonarchTokensProvider('powerfx', {
                tokenizer: {
                    root: [
                        [/\/\/.*$/, 'comment'],
                        [/\b(true|false)\b/, 'keyword'],
                        [/\b(If|Then|Else|And|Or|Not|IsBlank|IsError|Sum|Round|Count|Average)\b/, 'keyword'],
                        [/\b([0-9]+(\.[0-9]+)?)\b/, 'number'],
                        [/"([^"\\]|\\.)*"/, 'string'],
                        [/\'([^'\\]|\\.)*\'/, 'string'],
                        [/[,;:\.\(\)\[\]\{\}]/, 'delimiter'],
                        [/[-+/*=<>!]+/, 'operator'],
                        [/[a-zA-Z_][\w\.]*?/, 'identifier']
                    ]
                }
            });
            monaco.editor.defineTheme('powerfxTheme', {
                base: 'vs',
                inherit: true,
                rules: [
                    { token: 'comment', foreground: '6A6A6A' },
                    { token: 'keyword', foreground: '0000FF', fontStyle: 'bold' },
                    { token: 'number', foreground: '09885A' },
                    { token: 'string', foreground: 'A31515' },
                    { token: 'identifier', foreground: '001080' }
                ]
            });
            registerPowerFxCompletions();
            monacoEditor = monaco.editor.create(refs.formulaEditorDiv, {
                value: '',
                language: 'powerfx',
                theme: 'powerfxTheme',
                minimap: { enabled: false },
                automaticLayout: true,
                fontSize: 13
            });
        }
        catch (err) {
            console.error('Monaco initialization failed, falling back to default theme:', err);
            monacoEditor = monaco.editor.create(refs.formulaEditorDiv, {
                value: '',
                language: 'powerfx',
                theme: 'vs',
                minimap: { enabled: false },
                automaticLayout: true,
                fontSize: 13
            });
        }
        monacoEditor.getModel().onDidChangeContent(() => {
            if (selectedIndex !== null && monacoEditor) {
                formulas[selectedIndex].formula = monacoEditor.getValue();
                renderList();
            }
        });
    });
}
function registerPowerFxCompletions() {
    const functionsList = ['If', 'Switch', 'Filter', 'Lookup', 'Patch', 'Collect', 'Remove', 'Update', 'ForAll', 'Sum', 'Average', 'Count', 'Round'];
    const keywordsList = ['And', 'Or', 'Not', 'IsBlank', 'IsError', 'Then', 'Else', 'true', 'false'];
    const suggestions = [];
    functionsList.forEach(fn => suggestions.push({
        label: fn,
        kind: monaco.languages.CompletionItemKind.Function,
        insertText: `${fn}($0)`,
        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
        documentation: `${fn} function`
    }));
    keywordsList.forEach(kw => suggestions.push({
        label: kw,
        kind: monaco.languages.CompletionItemKind.Keyword,
        insertText: kw,
        documentation: `${kw} keyword`
    }));
    monaco.languages.registerCompletionItemProvider('powerfx', {
        triggerCharacters: ['.', '(', ' '],
        provideCompletionItems(model, position) {
            const word = model.getWordUntilPosition(position);
            const range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn
            };
            const filtered = suggestions.filter(s => s.label.toLowerCase().startsWith(word.word.toLowerCase()));
            filtered.forEach(s => s.range = range);
            return { suggestions: filtered };
        }
    });
}
function attachDomListeners(sendToHost) {
    refs.addBtn.onclick = () => {
        const newFormula = {
            name: 'New Formula',
            formula: '',
            comments: '',
            result: ''
        };
        formulas.push(newFormula);
        const newIndex = formulas.length - 1;
        renderList();
        openEditor(newIndex);
    };
    refs.nameField.addEventListener('input', () => {
        if (selectedIndex !== null) {
            formulas[selectedIndex].name = refs.nameField.value;
            renderList();
        }
    });
    refs.commentsField.addEventListener('input', () => {
        if (selectedIndex !== null) {
            formulas[selectedIndex].comments = refs.commentsField.value;
            renderList();
        }
    });
    refs.resultField.addEventListener('input', () => {
        if (selectedIndex !== null) {
            formulas[selectedIndex].result = refs.resultField.value;
            renderList();
        }
    });
    refs.removeBtn.onclick = () => {
        if (selectedIndex !== null) {
            formulas.splice(selectedIndex, 1);
            selectedIndex = null;
            refs.editPanel.classList.remove('visible');
            renderList();
        }
    };
    refs.runSelectedBtn.onclick = () => {
        if (selectedIndex === null || selectedIndex < 0 || selectedIndex >= formulas.length) {
            console.warn('No selected formula to run up to.');
            return;
        }
        sendToHost(getRunPayload('runUpToSelected'));
    };
    refs.runAllBtn.onclick = () => {
        sendToHost(getRunPayload('runAll'));
    };
}
export function getRunPayload(type) {
    const payload = {
        type,
        payload: {
            name: refs.worksheetNameField.value || '',
            cells: formulas.map(({ id, name, formula, comments }) => ({
                id,
                name,
                formula,
                comments
            }))
        }
    };
    if (type === 'runUpToSelected') {
        payload.selectedIndex = selectedIndex;
    }
    return payload;
}
export function renderList() {
    refs.listContainer.innerHTML = '';
    formulas.forEach((f, index) => {
        const card = document.createElement('fluent-card');
        card.innerHTML = `
            <div style="font-size:16px;font-weight:600;color:#222;">
                ${f.name}
                <span style="float:right;">${f.result}</span>
            </div>
            <div style="margin-top:6px;color:#666;">${f.formula}</div>
            <div style="margin-top:6px;color:#666;font-size:13px;">${f.comments}</div>
        `;
        card.onclick = () => openEditor(index);
        refs.listContainer.appendChild(card);
    });
}
export function openEditor(index) {
    selectedIndex = index;
    const f = formulas[index];
    refs.nameField.value = f.name;
    refs.commentsField.value = f.comments;
    refs.resultField.value = f.result;
    if (monacoEditor) {
        monacoEditor.setValue(f.formula || '');
        monacoEditor.focus();
    }
    else {
        refs.formulaEditorDiv.textContent = f.formula || '';
    }
    refs.editPanel.classList.add('visible');
}
export function loadWorksheetFromHost(data) {
    const worksheetName = data.Name || data.name || '';
    refs.worksheetNameField.value = worksheetName;
    const cells = data.Cells || data.cells || data.formulas || [];
    if (!Array.isArray(cells)) {
        console.warn('Received worksheet payload with no cells array:', data);
        return;
    }
    formulas.length = 0;
    cells.forEach((cell, index) => {
        formulas.push({
            id: cell.Id ?? cell.id ?? index,
            name: cell.Name || cell.name || '',
            formula: cell.Formula || cell.formula || '',
            comments: cell.Comments || cell.comments || '',
            result: cell.Result || cell.result || ''
        });
    });
    selectedIndex = null;
    refs.editPanel.classList.remove('visible');
    renderList();
}
export function applyHostResult(msg) {
    const resultValue = msg.displayOutput ?? msg.DisplayOutput;
    const cellId = msg.cellId ?? msg.CellId ?? msg.Id ?? msg.id;
    if (resultValue === undefined) {
        console.warn('Host result message missing result value:', msg);
        return;
    }
    let targetIndex = -1;
    targetIndex = formulas.findIndex(f => f.id == cellId);
    if (targetIndex === -1) {
        console.warn('Could not map host result to a formula cell:', msg);
        return;
    }
    formulas[targetIndex].result = resultValue;
    if (selectedIndex === targetIndex) {
        refs.resultField.value = resultValue;
    }
    renderList();
}
