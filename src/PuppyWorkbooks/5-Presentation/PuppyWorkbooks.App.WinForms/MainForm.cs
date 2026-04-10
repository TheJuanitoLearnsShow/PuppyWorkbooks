using System.Text.Json;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.App.WinForms;

public partial class MainForm : Form
{
    private Dictionary<TabPage, FormulaEditorControl> editors = new();
    private Dictionary<FormulaEditorControl, FloatingDocumentForm> floating = new();
    private readonly WorkSheetSerializer _workSheetSerializer = new ();
    private const string FileFilter = "Formula Files (*.xml)|*.xml";

    public MainForm()
    {
        InitializeComponent();
        Font = new Font("Segoe UI", 10);
    }

    private void newDocumentToolStripMenuItem_Click(object sender, EventArgs e)
    {
        CreateNewDocument();
    }

    private void tabControl1_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(e.Location))
                {
                    var tab = tabControl1.TabPages[i];
                    var menu = new ContextMenuStrip();
                    menu.Items.Add("Detach", null, (s, ev) => DetachTab(tab));
                    menu.Show(tabControl1, e.Location);
                    break;
                }
            }
        }
    }
    private void DetachTab(TabPage tab)
    {
        var editor = editors[tab];

        tab.Controls.Remove(editor);
        tabControl1.TabPages.Remove(tab);

        var floatForm = new FloatingDocumentForm(editor);
        floating[editor] = floatForm;

        floatForm.RequestReattach += ReattachDocument;
        floatForm.Show();
    }
    private void ReattachDocument(FloatingDocumentForm floatForm)
    {
        var editor = floatForm.Editor;

        floatForm.Controls.Remove(editor);
        floatForm.Hide();

        var tab = new TabPage(editor.Name);
        tab.Controls.Add(editor);
        editor.Dock = DockStyle.Fill;

        editors[tab] = editor;
        floating.Remove(editor);

        tabControl1.TabPages.Add(tab);
        tabControl1.SelectedTab = tab;
    }

    private void CreateNewDocumentOld()
    {
        var editor = new FormulaEditorControl();
        editor.Dock = DockStyle.Fill;

        var tab = new TabPage("New Formula");
        tab.Controls.Add(editor);

        // Allow the editor to remove its own tab
        editor.RequestClose += () =>
        {
            tabControl1.TabPages.Remove(tab);
            tab.Dispose();
        };

        tabControl1.TabPages.Add(tab);
        tabControl1.SelectedTab = tab;
    }

    private void CreateNewDocument()
    {
        var editor = new FormulaEditorControl();
        editor.RequestClose += () => CloseDocument(editor);

        var tab = new TabPage("New Formula");
        tab.Controls.Add(editor);
        editor.Dock = DockStyle.Fill;

        editors[tab] = editor;

        tabControl1.TabPages.Add(tab);
        tabControl1.SelectedTab = tab;
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = FileFilter;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var doc = _workSheetSerializer.DeserializeFromXmlFile(dlg.FileName);

            var editor = new FormulaEditorControl();
            editor.FromModel(doc);

            var tab = new TabPage(doc.Name);
            tab.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;

            editors[tab] = editor;
            tabControl1.TabPages.Add(tab);
            tabControl1.SelectedTab = tab;
        }
    }
    private void saveDocumentToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (tabControl1.SelectedTab == null) return;

        var editor = editors[tabControl1.SelectedTab];
        var doc = editor.ToModel();

        using var dlg = new SaveFileDialog();
        dlg.Filter = FileFilter;
        dlg.FileName = doc.Name + ".xml";

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _workSheetSerializer.SerializeToXmlFile(dlg.FileName, doc);
        }
    }
    private void CloseDocument(FormulaEditorControl editor)
    {
        // If floating
        if (floating.ContainsKey(editor))
        {
            var floatForm = floating[editor];
            floating.Remove(editor);
            floatForm.Close();
            return;
        }

        // If tabbed
        var tab = editors.First(kvp => kvp.Value == editor).Key;
        editors.Remove(tab);
        tabControl1.TabPages.Remove(tab);
    }

}