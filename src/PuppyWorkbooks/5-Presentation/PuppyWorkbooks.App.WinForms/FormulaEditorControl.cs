namespace PuppyWorkbooks.App.WinForms;

public partial class FormulaEditorControl : UserControl
{
    public event Action? RequestClose;

    public FormulaEditorControl()
    {
        InitializeComponent();
        Font = new Font("Segoe UI", 10);
    }

    public FormulaDocument ToDocument() =>
        new FormulaDocument
        {
            Name = txtName.Text,
            Formula = txtFormula.Text,
            Comments = txtComments.Text,
            Result = txtResult.Text
        };

    public void LoadDocument(FormulaDocument doc)
    {
        txtName.Text = doc.Name;
        txtFormula.Text = doc.Formula;
        txtComments.Text = doc.Comments;
        txtResult.Text = doc.Result;
    }

    private void btnRemove_Click(object sender, EventArgs e)
    {
        RequestClose?.Invoke();
    }

}
