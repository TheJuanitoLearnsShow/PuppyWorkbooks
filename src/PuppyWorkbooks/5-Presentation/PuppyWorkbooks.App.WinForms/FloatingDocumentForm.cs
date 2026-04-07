namespace PuppyWorkbooks.App.WinForms;

public partial class FloatingDocumentForm : Form
{
    public FormulaEditorControl Editor { get; }

    public event Action<FloatingDocumentForm>? RequestReattach;

    public FloatingDocumentForm(FormulaEditorControl editor)
    {
        Editor = editor;
        Text = editor.txtName.Text;
        Width = 500;
        Height = 600;

        Controls.Add(editor);
        editor.Dock = DockStyle.Fill;

        FormClosing += (s, e) =>
        {
            RequestReattach?.Invoke(this);
            e.Cancel = true; // prevent closing, reattach instead
        };
    }
}