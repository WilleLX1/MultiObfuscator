namespace MultiObfuscator
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtPathToObfuscate = new TextBox();
            txtOrginal = new RichTextBox();
            txtObfuscated = new RichTextBox();
            label1 = new Label();
            btnObfuscate = new Button();
            txtLog = new RichTextBox();
            btnBrowse = new Button();
            btnSettings = new Button();
            SuspendLayout();
            // 
            // txtPathToObfuscate
            // 
            txtPathToObfuscate.Location = new Point(92, 12);
            txtPathToObfuscate.Name = "txtPathToObfuscate";
            txtPathToObfuscate.Size = new Size(734, 23);
            txtPathToObfuscate.TabIndex = 0;
            txtPathToObfuscate.Text = "C:\\projects\\C#\\MultiObfuscator\\TestApplication\\Program.cs";
            // 
            // txtOrginal
            // 
            txtOrginal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            txtOrginal.BackColor = Color.Black;
            txtOrginal.ForeColor = Color.White;
            txtOrginal.Location = new Point(12, 41);
            txtOrginal.Name = "txtOrginal";
            txtOrginal.Size = new Size(396, 524);
            txtOrginal.TabIndex = 1;
            txtOrginal.Text = "";
            // 
            // txtObfuscated
            // 
            txtObfuscated.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            txtObfuscated.BackColor = Color.Black;
            txtObfuscated.ForeColor = Color.White;
            txtObfuscated.Location = new Point(536, 46);
            txtObfuscated.Name = "txtObfuscated";
            txtObfuscated.Size = new Size(678, 524);
            txtObfuscated.TabIndex = 2;
            txtObfuscated.Text = "";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Transparent;
            label1.Location = new Point(414, 296);
            label1.Name = "label1";
            label1.Size = new Size(116, 21);
            label1.TabIndex = 3;
            label1.Text = "Obfuscated -->";
            // 
            // btnObfuscate
            // 
            btnObfuscate.Location = new Point(832, 12);
            btnObfuscate.Name = "btnObfuscate";
            btnObfuscate.Size = new Size(114, 23);
            btnObfuscate.TabIndex = 4;
            btnObfuscate.Text = "OBFUSCATE";
            btnObfuscate.UseVisualStyleBackColor = true;
            btnObfuscate.Click += btnObfuscate_Click;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.White;
            txtLog.Location = new Point(1220, 17);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(406, 553);
            txtLog.TabIndex = 5;
            txtLog.Text = "";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(12, 12);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(74, 23);
            btnBrowse.TabIndex = 6;
            btnBrowse.Text = "BROWSE";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // btnSettings
            // 
            btnSettings.Location = new Point(414, 41);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(116, 30);
            btnSettings.TabIndex = 7;
            btnSettings.Text = "Settings";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.DimGray;
            ClientSize = new Size(1638, 577);
            Controls.Add(btnSettings);
            Controls.Add(btnBrowse);
            Controls.Add(txtLog);
            Controls.Add(btnObfuscate);
            Controls.Add(label1);
            Controls.Add(txtObfuscated);
            Controls.Add(txtOrginal);
            Controls.Add(txtPathToObfuscate);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtPathToObfuscate;
        private RichTextBox txtOrginal;
        private RichTextBox txtObfuscated;
        private Label label1;
        private Button btnObfuscate;
        private RichTextBox txtLog;
        private Button btnBrowse;
        private Button btnSettings;
    }
}
