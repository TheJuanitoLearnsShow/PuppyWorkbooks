using System.Text.Json;
using PuppyWorkbooks.App.WinForms.ViewModels;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.App.WinForms;

public partial class MainForm : Form
{
    private Dictionary<TabPage, TabView> editors = new();
    private Dictionary<TabView, FloatingDocumentForm> floating = new();
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

    private void CreateNewDocument()
    {
        var viewModel = new TabViewModel("New", "wwwroot/worksheet-editor/index.html");
        viewModel.FromModel(new WorkSheet()
        {
            Name = "Test WorkSheet",
            Cells = 
                [
                    new ()
                    {
                        Id = 0,
                        Name = "Name1",
                        Formula = "\"Juanito\""
                    },
                    new ()
                    {
                        Id = 0,
                        Name = "Greeting",
                        Formula = "\"Hello\" & Name1 "
                    }
                ]
        });
        NewEditorTab(viewModel);
    }

    private void NewEditorTab(TabViewModel viewModel)
    {
        var editor = new TabView(viewModel);
        // editor.RequestClose += () => CloseDocument(editor);

        var tab = new TabPage(viewModel.Title);
        tab.Controls.Add(editor);
        editor.Dock = DockStyle.Fill;

        editors[tab] = editor;

        tabControl1.TabPages.Add(tab);
        tabControl1.SelectedTab = tab;
    }
    // private void CreateNewDocumentOld()
    // {
    //     var editor = new FormulaEditorControl();
    //     editor.RequestClose += () => CloseDocument(editor);
    //
    //     var tab = new TabPage("New Formula");
    //     tab.Controls.Add(editor);
    //     editor.Dock = DockStyle.Fill;
    //
    //     editors[tab] = editor;
    //
    //     tabControl1.TabPages.Add(tab);
    //     tabControl1.SelectedTab = tab;
    // }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog();
        dlg.Filter = FileFilter;

        if (dlg.ShowDialog() != DialogResult.OK) return;
        var viewModel = new TabViewModel("New", "wwwroot/worksheet-editor/index.html");
        var model = _workSheetSerializer.DeserializeFromXmlFile(dlg.FileName);
        viewModel.FromModel(model);
        NewEditorTab(viewModel);
    }
    private void saveDocumentToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (tabControl1.SelectedTab == null) return;

        var editor = editors[tabControl1.SelectedTab];
        var doc = editor.Vm.Model;

        using var dlg = new SaveFileDialog();
        dlg.Filter = FileFilter;
        dlg.FileName = doc.Name + ".xml";

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _workSheetSerializer.SerializeToXmlFile(dlg.FileName, doc);
        }
    }
    private void CloseDocument(TabView editor)
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