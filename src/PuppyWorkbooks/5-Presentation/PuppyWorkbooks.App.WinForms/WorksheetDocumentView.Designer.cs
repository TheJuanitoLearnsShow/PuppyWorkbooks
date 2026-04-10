using System.ComponentModel;

namespace PuppyWorkbooks.App.WinForms;

partial class WorksheetDocumentView
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

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        splitContainer1 = new SplitContainer();
        flowLayoutPanel1 = new FlowLayoutPanel();
        dataGridView1 = new DataGridView();
        leftButtonPanel = new FlowLayoutPanel();
        NameCol = new DataGridViewTextBoxColumn();
        Formula = new DataGridViewTextBoxColumn();
        Comments = new DataGridViewTextBoxColumn();
        Result = new DataGridViewTextBoxColumn();
        ((ISupportInitialize)splitContainer1).BeginInit();
        splitContainer1.Panel1.SuspendLayout();
        splitContainer1.SuspendLayout();
        flowLayoutPanel1.SuspendLayout();
        ((ISupportInitialize)dataGridView1).BeginInit();
        SuspendLayout();
        // 
        // splitContainer1
        // 
        splitContainer1.Dock = DockStyle.Fill;
        splitContainer1.Location = new Point(0, 0);
        splitContainer1.Name = "splitContainer1";
        // 
        // splitContainer1.Panel1
        // 
        splitContainer1.Panel1.Controls.Add(flowLayoutPanel1);
        splitContainer1.Size = new Size(800, 600);
        splitContainer1.SplitterDistance = 389;
        splitContainer1.TabIndex = 0;
        splitContainer1.Text = "splitContainer1";
        // 
        // flowLayoutPanel1
        // 
        flowLayoutPanel1.Controls.Add(leftButtonPanel);
        flowLayoutPanel1.Controls.Add(dataGridView1);
        flowLayoutPanel1.Dock = DockStyle.Fill;
        flowLayoutPanel1.Location = new Point(0, 0);
        flowLayoutPanel1.Name = "flowLayoutPanel1";
        flowLayoutPanel1.Size = new Size(389, 600);
        flowLayoutPanel1.TabIndex = 0;
        // 
        // dataGridView1
        // 
        dataGridView1.AllowUserToAddRows = false;
        dataGridView1.AllowUserToDeleteRows = false;
        dataGridView1.BackgroundColor = Color.FromArgb(250, 250, 252);
        dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView1.Columns.AddRange(new DataGridViewColumn[] { NameCol, Formula, Comments, Result });
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = SystemColors.Window;
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(0, 120, 215);
        dataGridViewCellStyle1.SelectionForeColor = Color.White;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
        dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
        dataGridView1.Dock = DockStyle.Fill;
        dataGridView1.GridColor = Color.FromArgb(230, 230, 235);
        dataGridView1.Location = new Point(3, 109);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.ReadOnly = true;
        dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView1.Size = new Size(240, 0);
        dataGridView1.TabIndex = 0;
        // 
        // leftButtonPanel
        // 
        leftButtonPanel.Location = new Point(3, 3);
        leftButtonPanel.Name = "leftButtonPanel";
        leftButtonPanel.Size = new Size(200, 100);
        leftButtonPanel.TabIndex = 1;
        // 
        // NameCol
        // 
        NameCol.HeaderText = "Name";
        NameCol.Name = "NameCol";
        NameCol.ReadOnly = true;
        // 
        // Formula
        // 
        Formula.HeaderText = "Formula";
        Formula.Name = "Formula";
        Formula.ReadOnly = true;
        Formula.Width = 200;
        // 
        // Comments
        // 
        Comments.HeaderText = "Comments";
        Comments.Name = "Comments";
        Comments.ReadOnly = true;
        // 
        // Result
        // 
        Result.HeaderText = "Result";
        Result.Name = "Result";
        Result.ReadOnly = true;
        // 
        // WorksheetDocumentView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.ControlLight;
        Controls.Add(splitContainer1);
        Name = "WorksheetDocumentView";
        Size = new Size(800, 600);
        splitContainer1.Panel1.ResumeLayout(false);
        ((ISupportInitialize)splitContainer1).EndInit();
        splitContainer1.ResumeLayout(false);
        flowLayoutPanel1.ResumeLayout(false);
        ((ISupportInitialize)dataGridView1).EndInit();
        ResumeLayout(false);
    }

    private System.Windows.Forms.SplitContainer splitContainer1;

    #endregion

    private FlowLayoutPanel flowLayoutPanel1;
    private DataGridView dataGridView1;
    private FlowLayoutPanel leftButtonPanel;
    private DataGridViewTextBoxColumn NameCol;
    private DataGridViewTextBoxColumn Formula;
    private DataGridViewTextBoxColumn Comments;
    private DataGridViewTextBoxColumn Result;
}