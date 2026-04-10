using DynamicData;

namespace PuppyWorkbooks.App.WinForms;

public partial class FormulaEditorControl : UserControl
{
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
}
