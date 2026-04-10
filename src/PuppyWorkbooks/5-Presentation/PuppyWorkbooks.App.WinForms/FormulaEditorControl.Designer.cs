using System.ComponentModel;

namespace PuppyWorkbooks.App.WinForms;

partial class FormulaEditorControl
{
    private Panel editorPanel;
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
        Width = 1000;
        Height = 600;

        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 245, 248); // Fluent neutral background
        Dock = DockStyle.Fill;
        
        InitializeLayout();
        InitializeData();
    }

    private void InitializeLayout()
    {
        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 600,
            BackColor = Color.Transparent
        };
        Controls.Add(splitContainer);

        BuildLeftPanel();

        // Right panel: editor
        BuildEditorPanel();
    }
    private TextBox CreateFluentTextBox(bool multiline = false)
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = multiline,
            BackColor = Color.FromArgb(250, 250, 252),
            ForeColor = Color.Black
        };
    }

    private Button CreateFluentButton(string text, Color color)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            Width = 100,
            Height = 32
        };
    }
    private void InitializeData()
    {
        formulas = new BindingList<FormulaEntry>
        {
            new FormulaEntry { Name = "Velocity", Expression = "20", Comments = "Initial velocity", Result = "", Id = 0 },
            new FormulaEntry { Name = "Mass", Expression = "2", Comments = "Mass", Result = "", Id = 1 },
            new FormulaEntry { Name = "KineticEnergy", Expression = "(1/2) * Mass * Velocity * Velocity", Comments = "", Result = "", Id = 2 }
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

    private void BuildLeftPanel()
    {
        // Left panel: formula list
        // var leftPanel  = new Panel
        // {
        //     Dock = DockStyle.Fill,
        //     Padding = new Padding(20),
        //     BackColor = Color.FromArgb(255, 255, 255, 255)
        // };
        
        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.FromArgb(255, 255, 255, 255)
        };
        dgvFormulas = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.FromArgb(250, 250, 252),
            GridColor = Color.FromArgb(230, 230, 235),
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        dgvFormulas.DefaultCellStyle.Font = new Font("Segoe UI", 10);
        dgvFormulas.DefaultCellStyle.BackColor = Color.White;
        dgvFormulas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
        dgvFormulas.DefaultCellStyle.SelectionForeColor = Color.White;
        
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 150 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Formula", DataPropertyName = "Expression", Width = 200 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Comments", DataPropertyName = "Comments", Width = 150 });
        dgvFormulas.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Result", DataPropertyName = "Result", Width = 100 });
        dgvFormulas.SelectionChanged += DgvFormulas_SelectionChanged;
        
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };
        var btnAdd = CreateFluentButton("Add Formula", Color.FromArgb(0, 120, 215));
        var btnRunAll = CreateFluentButton("Run All", Color.FromArgb(0, 120, 215));
        btnAdd.Click += (s, e) => AddBlankFormula();
        btnRunAll.Click += (s, e) => RunAllFormulas();
        buttonPanel.Controls.Add(btnAdd);
        buttonPanel.Controls.Add(btnRunAll);
        
        leftPanel.Controls.Add(dgvFormulas);
        //leftPanel.Controls.Add(buttonPanel);
        //splitContainer.Panel1.Controls.Add(leftPanel)
        splitContainer.Panel1.Controls.Add(dgvFormulas);
        splitContainer.Panel1.Controls.Add(buttonPanel);
    }

    private void BuildEditorPanel()
    {
        editorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(255, 255, 255, 255)
        };
        splitContainer.Panel2.Controls.Add(editorPanel);
        var lblTitle = new Label
        {
            Text = "Edit Formula",
            Font = new Font("Segoe UI Semibold", 12),
            Dock = DockStyle.Top,
            Height = 30
        };
        editorPanel.Controls.Add(lblTitle);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 10)
        };
        editorPanel.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, 0);
        txtName = CreateFluentTextBox();
        layout.Controls.Add(txtName, 1, 0);

        layout.Controls.Add(new Label { Text = "Formula:", AutoSize = true }, 0, 1);
        txtFormula = CreateFluentTextBox();
        layout.Controls.Add(txtFormula, 1, 1);

        layout.Controls.Add(new Label { Text = "Comments:", AutoSize = true }, 0, 2);
        txtComments = CreateFluentTextBox(multiline: true);
        layout.Controls.Add(txtComments, 1, 2);

        layout.Controls.Add(new Label { Text = "Result:", AutoSize = true }, 0, 3);
        txtResult = CreateFluentTextBox();
        layout.Controls.Add(txtResult, 1, 3);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };
        btnSave = CreateFluentButton("Save", Color.FromArgb(0, 120, 215));
        btnCancel = CreateFluentButton("Cancel", Color.FromArgb(200, 200, 200));
        
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += BtnCancel_Click;
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);
        splitContainer.Panel2.Controls.Add(buttonPanel);
    }
}