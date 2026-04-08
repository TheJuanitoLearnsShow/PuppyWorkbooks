using System.ComponentModel;

namespace PuppyWorkbooks.App.WinForms;

partial class FormulaEditorControl
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private SplitContainer splitContainer;
    private DataGridView dgvFormulas;
    private TextBox txtName, txtFormula, txtComments, txtResult;
    private Button btnSave, btnCancel;
    private BindingList<FormulaEntry> formulas;

    public void InitializeComponent()
    {
        Text = "My Formulas";
        Font = new Font("Segoe UI", 10);
        Width = 1000;
        Height = 600;

        InitializeLayout();
        InitializeData();
    }

    private void InitializeLayout()
    {
        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 600,
            BackColor = Color.WhiteSmoke
        };
        Controls.Add(splitContainer);

        // Left panel: formula list
        var leftPanel = splitContainer.Panel1;
        dgvFormulas = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White
        };
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 150 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Formula", DataPropertyName = "Expression", Width = 200 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Comments", DataPropertyName = "Comments", Width = 150 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Result", DataPropertyName = "Result", Width = 100 });
        dgvFormulas.SelectionChanged += DgvFormulas_SelectionChanged;
        
        var btnAdd = new Button { Text = "Add Formula", Dock = DockStyle.Bottom };
        var btnRunAll = new Button { Text = "Run All", Dock = DockStyle.Bottom };
        btnAdd.Click += (s, e) => formulas.Add(new FormulaEntry() { Id = formulas.Count });
        btnRunAll.Click += (s, e) => RunAllFormulas();
        leftPanel.Controls.Add(btnAdd);
        leftPanel.Controls.Add(dgvFormulas);
        leftPanel.Controls.Add(btnRunAll);

        // Right panel: editor
        var editorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        splitContainer.Panel2.Controls.Add(editorPanel);

        editorPanel.Controls.Add(new Label { Text = "Formula Name:", AutoSize = true }, 0, 0);
        txtName = new TextBox { Dock = DockStyle.Fill };
        editorPanel.Controls.Add(txtName, 1, 0);

        editorPanel.Controls.Add(new Label { Text = "Formula:", AutoSize = true }, 0, 1);
        txtFormula = new TextBox { Dock = DockStyle.Fill };
        editorPanel.Controls.Add(txtFormula, 1, 1);

        editorPanel.Controls.Add(new Label { Text = "Comments:", AutoSize = true }, 0, 2);
        txtComments = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
        editorPanel.Controls.Add(txtComments, 1, 2);

        editorPanel.Controls.Add(new Label { Text = "Result:", AutoSize = true }, 0, 3);
        txtResult = new TextBox { Dock = DockStyle.Fill };
        editorPanel.Controls.Add(txtResult, 1, 3);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft };
        btnSave = new Button { Text = "Save", BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White };
        btnCancel = new Button { Text = "Cancel" };
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += BtnCancel_Click;
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);
        splitContainer.Panel2.Controls.Add(buttonPanel);
    }

    private void InitializeData()
    {
        formulas = new BindingList<FormulaEntry>
        {
            new FormulaEntry { Name = "Compound Interest", Expression = "A = P(1 + r/n)^nt", Comments = "Interest calculation", Result = "$15,932.48", Id = 0 },
            new FormulaEntry { Name = "Quadratic Equation", Expression = "x = (-b ± √(b² - 4ac)) / 2a", Comments = "Finding roots", Result = "x₁ = 2, x₂ = -3", Id = 1 },
            new FormulaEntry { Name = "BMI Calculation", Expression = "BMI = weight / height²", Comments = "Body mass index", Result = "24.8", Id = 2 },
            new FormulaEntry { Name = "Velocity Formula", Expression = "v = d / t", Comments = "Speed calculation", Result = "20 m/s", Id = 3 }
        };
        dgvFormulas.DataSource = formulas;
    }

    private void DgvFormulas_SelectionChanged(object sender, EventArgs e)
    {
        if (dgvFormulas.CurrentRow?.DataBoundItem is FormulaEntry f)
        {
            txtName.Text = f.Name;
            txtFormula.Text = f.Expression;
            txtComments.Text = f.Comments;
            txtResult.Text = f.Result;
        }
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        if (dgvFormulas.CurrentRow?.DataBoundItem is FormulaEntry f)
        {
            f.Name = txtName.Text;
            f.Expression = txtFormula.Text;
            f.Comments = txtComments.Text;
            f.Result = txtResult.Text;
            dgvFormulas.Refresh();
        }
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        DgvFormulas_SelectionChanged(null, null);
    }

}