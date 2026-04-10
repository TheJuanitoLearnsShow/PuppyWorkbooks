using System.ComponentModel;

namespace PuppyWorkbooks.App.WinForms;

partial class MainForm
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
    private MenuStrip menuStrip1;
    private ToolStripMenuItem fileToolStripMenuItem;
    private ToolStripMenuItem newDocumentToolStripMenuItem;
    private ToolStripMenuItem saveDocumentToolStripMenuItem;
    private ToolStripMenuItem openDocumentToolStripMenuItem;
    
    private TabControl tabControl1;

    private void InitializeComponent()
    {
        menuStrip1 = new MenuStrip();
        fileToolStripMenuItem = new ToolStripMenuItem();
        newDocumentToolStripMenuItem = new ToolStripMenuItem();
        tabControl1 = new TabControl();

        menuStrip1.Items.AddRange(new ToolStripItem[] {
            fileToolStripMenuItem
        });


        newDocumentToolStripMenuItem.Text = "New Document";
        newDocumentToolStripMenuItem.Click += new EventHandler(newDocumentToolStripMenuItem_Click);

        saveDocumentToolStripMenuItem = new ToolStripMenuItem();
        saveDocumentToolStripMenuItem.Text = "Save Document";
        saveDocumentToolStripMenuItem.Click += new EventHandler(saveDocumentToolStripMenuItem_Click);
        
        openDocumentToolStripMenuItem = new ToolStripMenuItem();
        openDocumentToolStripMenuItem.Text = "Open Document";
        openDocumentToolStripMenuItem.Click += new EventHandler(openToolStripMenuItem_Click);
        
        fileToolStripMenuItem.Text = "File";
        fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
            newDocumentToolStripMenuItem,
            openDocumentToolStripMenuItem,
            saveDocumentToolStripMenuItem
        });
        tabControl1.Dock = DockStyle.Fill;

        Controls.Add(tabControl1);
        Controls.Add(menuStrip1);

        MainMenuStrip = menuStrip1;
        Text = "Formula Editor";
        WindowState = FormWindowState.Maximized;
    }
    // #region Windows Form Designer generated code
    //
    // /// <summary>
    // /// Required method for Designer support - do not modify
    // /// the contents of this method with the code editor.
    // /// </summary>
    // private void InitializeComponent()
    // {
    //     this.components = new System.ComponentModel.Container();
    //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    //     this.ClientSize = new System.Drawing.Size(800, 450);
    //     this.Text = "MainForm";
    // }
    //
    // #endregion
}