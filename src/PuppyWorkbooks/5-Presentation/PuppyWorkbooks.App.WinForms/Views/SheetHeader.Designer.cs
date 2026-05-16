namespace PuppyWorkbooks.App.WinForms.Views
{
    partial class SheetHeader
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            flowLayoutPanel1 = new FlowLayoutPanel();
            label1 = new Label();
            txtWorksheetName = new TextBox();
            flowLayoutPanel2 = new FlowLayoutPanel();
            btnAdd = new Button();
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(label1);
            flowLayoutPanel1.Controls.Add(txtWorksheetName);
            flowLayoutPanel1.Location = new Point(3, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new Padding(10);
            flowLayoutPanel1.Size = new Size(573, 86);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(13, 10);
            label1.Name = "label1";
            label1.Padding = new Padding(0, 6, 5, 0);
            label1.Size = new Size(150, 29);
            label1.TabIndex = 0;
            label1.Text = "Worksheet Name:";
            // 
            // txtWorksheetName
            // 
            txtWorksheetName.BackColor = Color.FromArgb(250, 250, 252);
            txtWorksheetName.Location = new Point(169, 13);
            txtWorksheetName.Name = "txtWorksheetName";
            txtWorksheetName.Size = new Size(200, 27);
            txtWorksheetName.TabIndex = 1;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Controls.Add(btnAdd);
            flowLayoutPanel2.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel2.Location = new Point(582, 0);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new Size(365, 86);
            flowLayoutPanel2.TabIndex = 2;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(268, 3);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(94, 29);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Add Formula";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // SheetHeader
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(flowLayoutPanel1);
            Controls.Add(flowLayoutPanel2);
            Name = "SheetHeader";
            Size = new Size(950, 86);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanel1;
        private Label label1;
        private TextBox txtWorksheetName;
        private FlowLayoutPanel flowLayoutPanel2;
        private Button btnAdd;
    }
}
