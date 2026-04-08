namespace PuppyWorkbooks.App.WinForms;

public partial class FormulaEditorControl : UserControl
{
    public event Action? RequestClose;

    public FormulaEditorControl()
    {
        InitializeComponent();
        Font = new Font("Segoe UI", 10);
    }

    public FormulaEntry ToDocument() =>
        new FormulaEntry
        {
            Name = txtName.Text,
            Expression = txtFormula.Text,
            Comments = txtComments.Text,
            Result = txtResult.Text
        };

    public void LoadDocument(FormulaEntry doc)
    {
        txtName.Text = doc.Name;
        txtFormula.Text = doc.Expression;
        txtComments.Text = doc.Comments;
        txtResult.Text = doc.Result;
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        RequestClose?.Invoke();
    }

    private async void RunAllFormulas()
    {
        var workbook = ToModel();
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true))
        {
            formulas[result.CellId].Result = result.DisplayOutput;
        }
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
    private void ReIndexCells()
    {
        for (var i = 0; i < formulas.Count; i++)
        {
            formulas[i].Id = i;
        }
    }
}
