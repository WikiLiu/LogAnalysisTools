namespace LogAnalysisTools
{
    partial class FormHistory
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.del_hostory_button = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ckboxList = new System.Windows.Forms.CheckedListBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.see_button = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(329, 567);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // del_hostory_button
            // 
            this.del_hostory_button.Dock = System.Windows.Forms.DockStyle.Right;
            this.del_hostory_button.Location = new System.Drawing.Point(210, 3);
            this.del_hostory_button.Name = "del_hostory_button";
            this.del_hostory_button.Size = new System.Drawing.Size(110, 32);
            this.del_hostory_button.TabIndex = 0;
            this.del_hostory_button.Text = "删除历史记录";
            this.del_hostory_button.UseVisualStyleBackColor = true;
            this.del_hostory_button.Click += new System.EventHandler(this.del_hostory_button_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ckboxList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 47);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(323, 517);
            this.panel1.TabIndex = 1;
            // 
            // ckboxList
            // 
            this.ckboxList.CheckOnClick = true;
            this.ckboxList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ckboxList.FormattingEnabled = true;
            this.ckboxList.Location = new System.Drawing.Point(0, 0);
            this.ckboxList.Name = "ckboxList";
            this.ckboxList.Size = new System.Drawing.Size(323, 517);
            this.ckboxList.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.del_hostory_button, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.see_button, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(323, 38);
            this.tableLayoutPanel2.TabIndex = 2;
            // 
            // see_button
            // 
            this.see_button.Dock = System.Windows.Forms.DockStyle.Left;
            this.see_button.Location = new System.Drawing.Point(3, 3);
            this.see_button.Name = "see_button";
            this.see_button.Size = new System.Drawing.Size(93, 32);
            this.see_button.TabIndex = 1;
            this.see_button.Text = "确定";
            this.see_button.UseVisualStyleBackColor = true;
            this.see_button.Click += new System.EventHandler(this.see_button_Click);
            // 
            // FormHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 567);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormHistory";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "历史记录表";
            this.Load += new System.EventHandler(this.FormHistory_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button del_hostory_button;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckedListBox ckboxList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button see_button;
    }
}