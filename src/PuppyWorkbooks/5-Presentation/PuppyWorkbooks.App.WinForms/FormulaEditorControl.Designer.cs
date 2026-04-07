using System.ComponentModel;

namespace PuppyWorkbooks.App.WinForms;

partial class FormulaEditorControl
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

    private Label lblName;
    public TextBox txtName;
    private Label lblFormula;
    private TextBox txtFormula;
    private Label lblComments;
    private TextBox txtComments;
    private Label lblResult;
    private TextBox txtResult;
    private Button btnRemove;

    private void InitializeComponent()
    {
        lblName = new Label();
        txtName = new TextBox();
        lblFormula = new Label();
        txtFormula = new TextBox();
        lblComments = new Label();
        txtComments = new TextBox();
        lblResult = new Label();
        txtResult = new TextBox();
        btnRemove = new Button();

        lblName.Text = "Name";
        lblName.Location = new Point(20, 20);

        txtName.Location = new Point(20, 45);
        txtName.Width = 300;

        lblFormula.Text = "Formula";
        lblFormula.Location = new Point(20, 90);

        txtFormula.Location = new Point(20, 115);
        txtFormula.Width = 300;

        lblComments.Text = "Comments";
        lblComments.Location = new Point(20, 160);

        txtComments.Location = new Point(20, 185);
        txtComments.Width = 300;
        txtComments.Height = 80;
        txtComments.Multiline = true;

        lblResult.Text = "Result";
        lblResult.Location = new Point(20, 280);

        txtResult.Location = new Point(20, 305);
        txtResult.Width = 300;

        btnRemove.Text = "Remove";
        btnRemove.BackColor = Color.FromArgb(217, 44, 44);
        btnRemove.ForeColor = Color.White;
        btnRemove.FlatStyle = FlatStyle.Flat;
        btnRemove.Location = new Point(20, 360);
        btnRemove.Width = 300;
        btnRemove.Height = 40;
        btnRemove.Click += btnRemove_Click;

        Controls.Add(lblName);
        Controls.Add(txtName);
        Controls.Add(lblFormula);
        Controls.Add(txtFormula);
        Controls.Add(lblComments);
        Controls.Add(txtComments);
        Controls.Add(lblResult);
        Controls.Add(txtResult);
        Controls.Add(btnRemove);

        BackColor = Color.FromArgb(243, 247, 251); // Fluent-like neutral background
        Dock = DockStyle.Fill;
    }

}