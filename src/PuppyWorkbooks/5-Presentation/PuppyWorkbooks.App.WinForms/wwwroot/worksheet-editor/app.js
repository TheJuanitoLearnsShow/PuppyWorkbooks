const formulas = [
    {
        name: "Quadratic Formula",
        formula: "x = (-b ± √(b² - 4ac)) / 2a",
        comments: "Solutions of ax² + bx + c = 0",
        result: "-1, -0.5"
    },
    {
        name: "Pythagorean Theorem",
        formula: "a² + b² = c²",
        comments: "Right-angled triangles",
        result: "5"
    },
    {
        name: "Area of a Circle",
        formula: "A = πr²",
        comments: "r = radius",
        result: "153.9"
    },
    {
        name: "Euler's Formula",
        formula: "e^(iπ) + 1 = 0",
        comments: "Relation to complex exponentials",
        result: "0"
    }
];

let selectedIndex = null;

const listContainer = document.getElementById("formulaList");
const editPanel = document.getElementById("editPanel");
const addBtn = document.getElementById("addBtn");

const nameField = document.getElementById("nameField");
const formulaEditorDiv = document.getElementById("formulaEditor");
const commentsField = document.getElementById("commentsField");
const resultField = document.getElementById("resultField");
const removeBtn = document.getElementById("removeBtn");

let monacoEditor = null;

function initMonaco() {
    if (typeof require === 'undefined') return;
    // Configure Monaco loader path if not already configured by the page
    if (require && require.config) {
        try {
            require.config({ paths: { vs: 'https://unpkg.com/monaco-editor@0.41.0/min/vs' } });
        } catch (e) {
            // ignore if already configured
        }
    }

    require(['vs/editor/editor.main'], function () {
        try {
            // Register a minimal Power Fx language using Monarch tokenizer
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
                        [/[a-zA-Z_][\w\.]*/, 'identifier']
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

            // Register a simple completion provider for Power Fx keywords and functions
            (function registerPowerFxCompletions() {
                const functionsList = [
                    'If', 'Switch', 'Filter', 'Lookup', 'Patch', 'Collect', 'Remove', 'Update', 'ForAll',
                    'Sum', 'Average', 'Count', 'Round'
                ];

                const keywordsList = ['And', 'Or', 'Not', 'IsBlank', 'IsError', 'Then', 'Else', 'true', 'false'];

                const suggestions = [];

                functionsList.forEach(fn => {
                    suggestions.push({
                        label: fn,
                        kind: monaco.languages.CompletionItemKind.Function,
                        insertText: `${fn}($0)`,
                        insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                        documentation: `${fn} function`
                    });
                });

                keywordsList.forEach(kw => {
                    suggestions.push({
                        label: kw,
                        kind: monaco.languages.CompletionItemKind.Keyword,
                        insertText: kw,
                        documentation: `${kw} keyword`
                    });
                });

                monaco.languages.registerCompletionItemProvider('powerfx', {
                    triggerCharacters: ['.', '(', ' '],
                    provideCompletionItems: function (model, position) {
                        // Basic prefix filtering
                        const word = model.getWordUntilPosition(position);
                        const range = {
                            startLineNumber: position.lineNumber,
                            endLineNumber: position.lineNumber,
                            startColumn: word.startColumn,
                            endColumn: word.endColumn
                        };

                        const filtered = suggestions.filter(s => s.label.toLowerCase().startsWith(word.word.toLowerCase()));
                        // attach range for replacement
                        filtered.forEach(s => s.range = range);

                        return { suggestions: filtered };
                    }
                });
            })();

            monacoEditor = monaco.editor.create(formulaEditorDiv, {
                value: '',
                language: 'powerfx',
                theme: 'powerfxTheme',
                minimap: { enabled: false },
                automaticLayout: true,
                fontSize: 13
            });
        } catch (err) {
            // Fallback: if theme/theme-colors fail on some environments, create editor with default theme
            console.error('Monaco initialization failed, falling back to default theme:', err);
            monacoEditor = monaco.editor.create(formulaEditorDiv, {
                value: '',
                language: 'powerfx',
                theme: 'vs',
                minimap: { enabled: false },
                automaticLayout: true,
                fontSize: 13
            });
        }

        // Wire change to update the formulas list
        monacoEditor.getModel().onDidChangeContent(() => {
            if (selectedIndex !== null && monacoEditor) {
                formulas[selectedIndex].formula = monacoEditor.getValue();
                renderList();
            }
        });
    });
}

// Initialize Monaco when loader is present
if (typeof require !== 'undefined') {
    initMonaco();
} else {
    // If require isn't available yet, retry shortly
    window.addEventListener('load', () => setTimeout(initMonaco, 100));
}

function renderList() {
    listContainer.innerHTML = "";

    formulas.forEach((f, index) => {
        const card = document.createElement("fluent-card");
        card.innerHTML = `
            <div style="font-size:16px;font-weight:600;color:#222;">
                ${f.name}
                <span style="float:right;">${f.result}</span>
            </div>
            <div style="margin-top:6px;color:#666;">${f.formula}</div>
            <div style="margin-top:6px;color:#666;font-size:13px;">
                ${f.comments}
            </div>
        `;
        card.onclick = () => openEditor(index);
        listContainer.appendChild(card);
    });
}

function openEditor(index) {
    selectedIndex = index;
    const f = formulas[index];

    nameField.value = f.name;
    commentsField.value = f.comments;
    resultField.value = f.result;

    // If Monaco editor is ready, set its value; otherwise set placeholder text
    if (monacoEditor) {
        monacoEditor.setValue(f.formula || '');
        monacoEditor.focus();
    } else {
        // fallback: show plain text in the container
        formulaEditorDiv.textContent = f.formula || '';
    }

    editPanel.classList.add("visible");
}

// Create a new formula and open it for editing
addBtn.onclick = () => {
    const newFormula = {
        name: "New Formula",
        formula: "",
        comments: "",
        result: ""
    };

    formulas.push(newFormula);
    const newIndex = formulas.length - 1;
    renderList();
    openEditor(newIndex);
};

// Auto-sync editor changes back to the list for name
nameField.addEventListener('input', () => {
    if (selectedIndex !== null) {
        formulas[selectedIndex].name = nameField.value;
        renderList();
    }
});

commentsField.addEventListener('input', () => {
    if (selectedIndex !== null) {
        formulas[selectedIndex].comments = commentsField.value;
        renderList();
    }
});

resultField.addEventListener('input', () => {
    if (selectedIndex !== null) {
        formulas[selectedIndex].result = resultField.value;
        renderList();
    }
});

removeBtn.onclick = () => {
    if (selectedIndex !== null) {
        formulas.splice(selectedIndex, 1);
        selectedIndex = null;
        editPanel.classList.remove("visible");
        renderList();
    }
};

renderList();

function sendToDotNet() {
    const payload = JSON.stringify({
        type: "ping",
        time: new Date().toISOString()
    });
    chrome.webview.postMessage(payload);
}

chrome.webview.addEventListener("message", event => {
    const msg = event.data;
    document.getElementById("log").textContent += "\nFrom .NET: " + msg;
});