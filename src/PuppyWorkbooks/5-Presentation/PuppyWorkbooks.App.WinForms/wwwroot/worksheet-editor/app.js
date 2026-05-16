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
const formulaField = document.getElementById("formulaField");
const commentsField = document.getElementById("commentsField");
const resultField = document.getElementById("resultField");
const removeBtn = document.getElementById("removeBtn");

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
    formulaField.value = f.formula;
    commentsField.value = f.comments;
    resultField.value = f.result;

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

// Auto-sync editor changes back to the list
nameField.addEventListener('input', () => {
    if (selectedIndex !== null) {
        formulas[selectedIndex].name = nameField.value;
        renderList();
    }
});

formulaField.addEventListener('input', () => {
    if (selectedIndex !== null) {
        formulas[selectedIndex].formula = formulaField.value;
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
