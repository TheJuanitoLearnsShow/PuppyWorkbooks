using DynamicData;

namespace PuppyWorkbooks.App.WinForms;

public partial class FormulaEditorControl : UserControl
{
    private Label lblWorksheetName;
    private TextBox txtWorksheetName;

    public string SheetName => txtWorksheetName.Text;
    public event Action? RequestClose;

    public FormulaEditorControl()
    {
        InitializeComponent();
        
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        RequestClose?.Invoke();
    }

    private async Task RunAllFormulas()
    {
        var workbook = ToModel();
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true))
        {
            formulas[result.CellId].Result = result.DisplayOutput;
        }
        dgvFormulas.Refresh();
    }
    public PuppyWorkbooks.WorkSheet ToModel()
    {
        var model = new PuppyWorkbooks.WorkSheet();
        model.Name = txtWorksheetName.Text;
        foreach (var cellViewModel in formulas)
        {
            model.Cells.Add(cellViewModel.ToModel());
        }
        return model;
    }
    public void FromModel(WorkSheet model)
    {
        formulas.Clear();
        foreach (var cell in model.Cells)
        {
            AddFormula(cell);
        }
    }
    private void ReIndexCells()
    {
        for (var i = 0; i < formulas.Count; i++)
        {
            formulas[i].Id = i;
        }
    }

    private void AddBlankFormula()
    {
        formulas.Add(new FormulaEntry() { Id = formulas.Count });
    }

    private void AddFormula(WorkCell cell)
    {
        formulas.Add(new FormulaEntry() { Id = cell.Id, Comments = cell.Comments, Expression = cell.Formula, Name = cell.Name});
    }

    private void BuildFormulasGrid()
    {
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
    }
}
