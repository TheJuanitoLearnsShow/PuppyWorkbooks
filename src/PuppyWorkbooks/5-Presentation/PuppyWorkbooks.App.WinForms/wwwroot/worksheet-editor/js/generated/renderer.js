import { EditorState, RangeSetBuilder } from '@codemirror/state';
import { Decoration, EditorView } from '@codemirror/view';
import { basicSetup } from 'codemirror';
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
let codeMirrorView = null;
let isProgrammaticEditorUpdate = false;
const powerFxCommentMark = Decoration.mark({ class: 'cm-powerfx-comment' });
const powerFxKeywordMark = Decoration.mark({ class: 'cm-powerfx-keyword' });
const powerFxNumberMark = Decoration.mark({ class: 'cm-powerfx-number' });
const powerFxStringMark = Decoration.mark({ class: 'cm-powerfx-string' });
function buildPowerFxDecorations(view) {
    const builder = new RangeSetBuilder();
    const doc = view.state.doc;
    for (let lineNumber = 1; lineNumber <= doc.lines; lineNumber += 1) {
        const line = doc.line(lineNumber);
        const text = line.text;
        const patterns = [
            { regex: /\/\/.*$/g, mark: powerFxCommentMark },
            { regex: /\b(true|false)\b/g, mark: powerFxKeywordMark },
            { regex: /\b(If|Then|Else|And|Or|Not|IsBlank|IsError|Switch|Filter|Lookup|Patch|Collect|Remove|Update|ForAll|Sum|Average|Count|Round)\b/g, mark: powerFxKeywordMark },
            { regex: /\b([0-9]+(\.[0-9]+)?)\b/g, mark: powerFxNumberMark },
            { regex: /"([^"\\]|\\.)*"|'([^'\\]|\\.)*'/g, mark: powerFxStringMark }
        ];
        patterns.forEach(({ regex, mark }) => {
            regex.lastIndex = 0;
            let match;
            while ((match = regex.exec(text)) !== null) {
                const from = line.from + match.index;
                const to = from + match[0].length;
                builder.add(from, to, mark);
            }
        });
    }
    return builder.finish();
}
const powerFxSyntaxExtension = EditorView.decorations.of((view) => buildPowerFxDecorations(view));
export function initRenderer(sendToHost) {
    initRefs();
    initCodeMirror();
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
function initCodeMirror() {
    if (!refs.formulaEditorDiv) {
        return;
    }
    const state = EditorState.create({
        doc: '',
        extensions: [
            basicSetup,
            powerFxSyntaxExtension,
            EditorView.updateListener.of(update => {
                if (update.docChanged && selectedIndex !== null && !isProgrammaticEditorUpdate) {
                    formulas[selectedIndex].formula = update.state.doc.toString();
                    renderList();
                }
            }),
            EditorView.theme({
                '&': { height: '100%', fontSize: '13px' },
                '.cm-scroller': { overflow: 'auto' },
                '.cm-content': { fontFamily: 'Consolas, monospace' },
                '.cm-gutters': { background: '#f5f5f5', borderRight: '1px solid #ddd' },
                '.cm-line': { padding: '0 8px' }
            })
        ]
    });
    codeMirrorView = new EditorView({
        state,
        parent: refs.formulaEditorDiv
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
    if (codeMirrorView) {
        isProgrammaticEditorUpdate = true;
        const value = f.formula || '';
        codeMirrorView.dispatch({
            changes: { from: 0, to: codeMirrorView.state.doc.length, insert: value }
        });
        isProgrammaticEditorUpdate = false;
        codeMirrorView.focus();
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
