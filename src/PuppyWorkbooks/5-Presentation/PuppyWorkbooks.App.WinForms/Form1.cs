using PuppyWorkbooks.ViewModels;
using System.ComponentModel;
using System.Reactive;

namespace PuppyWorkbooks.App.WinForms;

public partial class Form1 : Form
{
    private BindingList<WorkSheetViewModel> _workbooks = new();
    private WorkSheetViewModel? _currentWorkbook;
    private TabControl _leftTabControl = new();
    private TabControl _rightTabControl = new();
    private SplitContainer _splitContainer = new();

    public Form1()
    {
        InitializeComponent();
        SetupUI();
        SetupDataBinding();
    }

    private void SetupUI()
    {
        this.Text = "Puppy Workbooks";
        this.Size = new Size(1000, 700);

        // Menu
        var menuStrip = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("New Workbook", null, NewWorkbook));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Load", null, LoadWorkbook));
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save", null, SaveWorkbook));
        menuStrip.Items.Add(fileMenu);
        this.MainMenuStrip = menuStrip;

        // Split container for side-by-side tabs
        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.Orientation = Orientation.Vertical;
        _splitContainer.SplitterDistance = this.Width / 2;
        _splitContainer.Panel1MinSize = 200;
        _splitContainer.Panel2MinSize = 200;

        // Left tab control
        _leftTabControl.Dock = DockStyle.Fill;
        _leftTabControl.ContextMenuStrip = CreateTabContextMenu(true); // true for left
        _leftTabControl.MouseDown += TabControl_MouseDown;

        // Right tab control
        _rightTabControl.Dock = DockStyle.Fill;
        _rightTabControl.ContextMenuStrip = CreateTabContextMenu(false); // false for right
        _rightTabControl.MouseDown += TabControl_MouseDown;

        _splitContainer.Panel1.Controls.Add(_leftTabControl);
        _splitContainer.Panel2.Controls.Add(_rightTabControl);

        this.Controls.Add(_splitContainer);

        _workbooks.ListChanged += Workbooks_ListChanged;
        _leftTabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        _rightTabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
    }

    private void SetupDataBinding()
    {
        // TODO
    }

    private void Workbooks_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (e.ListChangedType == ListChangedType.ItemAdded)
        {
            var vm = _workbooks[e.NewIndex];
            var tabPage = CreateWorkbookTab(vm);
            _leftTabControl.TabPages.Add(tabPage); // Add to left by default
            _leftTabControl.SelectedTab = tabPage;
            _currentWorkbook = vm;

            // Select first cell if any
            if (vm.Cells.Count > 0)
            {
                var listBox = (ListBox)tabPage.Controls[0].Controls[0].Controls[1]; // panelLeft.Controls[1]
                listBox.SelectedIndex = 0;
            }
        }
    }

    private TabPage CreateWorkbookTab(WorkSheetViewModel vm)
    {
        var tabPage = new TabPage(vm.Name);
        tabPage.Tag = vm; // Store the vm in tag

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 300
        };

        // Left: List of formulas
        var panelLeft = new Panel { Dock = DockStyle.Fill };
        var addButton = new Button { Text = "Add Cell", Dock = DockStyle.Top };
        addButton.Click += (s, e) => vm.AddCellCommand.Execute(Unit.Default);
        panelLeft.Controls.Add(addButton);

        var listBox = new ListBox { Dock = DockStyle.Fill };
        listBox.DataSource = vm.Cells;
        listBox.DisplayMember = "Name";
        listBox.SelectedIndexChanged += (s, e) =>
        {
            if (listBox.SelectedItem is WorkCellViewModel cell)
            {
                UpdateRightPanel(splitContainer.Panel2, cell);
            }
        };
        panelLeft.Controls.Add(listBox);

        splitContainer.Panel1.Controls.Add(panelLeft);

        // Right: Editable form
        var panel = new Panel { Dock = DockStyle.Fill };
        splitContainer.Panel2.Controls.Add(panel);

        tabPage.Controls.Add(splitContainer);

        return tabPage;
    }

    private void UpdateRightPanel(Panel panel, WorkCellViewModel cell)
    {
        panel.Controls.Clear();

        var tableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tableLayout.RowCount = 5;

        // Name
        tableLayout.Controls.Add(new Label { Text = "Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
        var nameTextBox = new TextBox { Dock = DockStyle.Fill };
        nameTextBox.DataBindings.Add("Text", cell, "Name");
        tableLayout.Controls.Add(nameTextBox, 1, 0);

        // Formula
        tableLayout.Controls.Add(new Label { Text = "Formula:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
        var formulaTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80 };
        formulaTextBox.DataBindings.Add("Text", cell, "Formula");
        tableLayout.Controls.Add(formulaTextBox, 1, 1);

        // Comments
        tableLayout.Controls.Add(new Label { Text = "Comments:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        var commentsTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true };
        commentsTextBox.DataBindings.Add("Text", cell, "Comments");
        tableLayout.Controls.Add(commentsTextBox, 1, 2);

        // Result
        tableLayout.Controls.Add(new Label { Text = "Result:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
        var resultTextBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        resultTextBox.DataBindings.Add("Text", cell, "Result");
        tableLayout.Controls.Add(resultTextBox, 1, 3);

        // Execute button
        var executeButton = new Button { Text = "Execute", Dock = DockStyle.Fill };
        executeButton.Click += (s, e) => cell.ExecuteCommand.Execute(Unit.Default);
        tableLayout.Controls.Add(executeButton, 1, 4);

        panel.Controls.Add(tableLayout);
    }

    private void LoadWorkbook(object? sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            var vm = new WorkSheetViewModel();
            vm.LoadFromXmlFile(openFileDialog.FileName);
            _workbooks.Add(vm);
        }
    }

    private void SaveWorkbook(object? sender, EventArgs e)
    {
        if (_currentWorkbook == null) return;
        using var saveFileDialog = new SaveFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
        };
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            _currentWorkbook.SaveToXmlFile(saveFileDialog.FileName);
        }
    }

    private ContextMenuStrip CreateTabContextMenu(bool isLeft)
    {
        var menu = new ContextMenuStrip();
        var moveToLeft = new ToolStripMenuItem("Move to Left");
        moveToLeft.Click += (s, e) => MoveTabToLeft();
        var moveToRight = new ToolStripMenuItem("Move to Right");
        moveToRight.Click += (s, e) => MoveTabToRight();

        if (isLeft)
        {
            menu.Items.Add(moveToRight);
        }
        else
        {
            menu.Items.Add(moveToLeft);
        }

        return menu;
    }

    private void MoveTabToLeft()
    {
        var tabControl = _rightTabControl; // Assuming context menu is on right
        if (tabControl.SelectedTab != null)
        {
            var tabPage = tabControl.SelectedTab;
            tabControl.TabPages.Remove(tabPage);
            _leftTabControl.TabPages.Add(tabPage);
            _leftTabControl.SelectedTab = tabPage;
        }
    }

    private void MoveTabToRight()
    {
        var tabControl = _leftTabControl; // Assuming context menu is on left
        if (tabControl.SelectedTab != null)
        {
            var tabPage = tabControl.SelectedTab;
            tabControl.TabPages.Remove(tabPage);
            _rightTabControl.TabPages.Add(tabPage);
            _rightTabControl.SelectedTab = tabPage;
        }
    }

    private void NewWorkbook(object? sender, EventArgs e)
    {
        var vm = new WorkSheetViewModel();
        _workbooks.Add(vm);
    }
    private void TabControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            var tabControl = sender as TabControl;
            if (tabControl != null)
            {
                for (int i = 0; i < tabControl.TabPages.Count; i++)
                {
                    var rect = tabControl.GetTabRect(i);
                    if (rect.Contains(e.Location))
                    {
                        tabControl.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var tabControl = sender as TabControl;
        if (tabControl == null) return;

        if (tabControl.SelectedIndex >= 0 && tabControl.SelectedIndex < tabControl.TabPages.Count)
        {
            var tabPage = tabControl.TabPages[tabControl.SelectedIndex];
            // Assuming the tag is set to the vm
            if (tabPage.Tag is WorkSheetViewModel vm)
            {
                _currentWorkbook = vm;
            }
        }
    }
}
