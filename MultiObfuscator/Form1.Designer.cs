namespace MultiObfuscator
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtPathToObfuscate = new System.Windows.Forms.TextBox();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnObfuscate = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerCode = new System.Windows.Forms.SplitContainer();
            this.panelOriginal = new System.Windows.Forms.Panel();
            this.lblOriginal = new System.Windows.Forms.Label();
            this.txtOriginal = new System.Windows.Forms.RichTextBox();
            this.panelObfuscated = new System.Windows.Forms.Panel();
            this.lblObfuscated = new System.Windows.Forms.Label();
            this.txtObfuscated = new System.Windows.Forms.RichTextBox();
            this.panelLog = new System.Windows.Forms.Panel();
            this.lblLog = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            // 
            // panelTop
            // 
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Height = 40;
            this.panelTop.BackColor = System.Drawing.Color.DimGray;
            this.panelTop.Controls.Add(this.btnBrowse);
            this.panelTop.Controls.Add(this.txtPathToObfuscate);
            this.panelTop.Controls.Add(this.btnSettings);
            this.panelTop.Controls.Add(this.btnObfuscate);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(10, 8);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 24);
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            this.toolTip.SetToolTip(this.btnBrowse, "Select a C# file to obfuscate");
            // 
            // txtPathToObfuscate
            // 
            this.txtPathToObfuscate.Location = new System.Drawing.Point(95, 9);
            this.txtPathToObfuscate.Name = "txtPathToObfuscate";
            this.txtPathToObfuscate.Size = new System.Drawing.Size(600, 23);
            this.txtPathToObfuscate.ReadOnly = true;
            // 
            // btnSettings
            // 
            this.btnSettings.Location = new System.Drawing.Point(700, 8);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(75, 24);
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            this.toolTip.SetToolTip(this.btnSettings, "Configure obfuscator settings");
            // 
            // btnObfuscate
            // 
            this.btnObfuscate.Location = new System.Drawing.Point(785, 8);
            this.btnObfuscate.Name = "btnObfuscate";
            this.btnObfuscate.Size = new System.Drawing.Size(100, 24);
            this.btnObfuscate.Text = "Obfuscate";
            this.btnObfuscate.UseVisualStyleBackColor = true;
            this.btnObfuscate.Click += new System.EventHandler(this.btnObfuscate_Click);
            this.toolTip.SetToolTip(this.btnObfuscate, "Start the obfuscation process");
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainerMain.SplitterDistance = 400;
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerCode);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.panelLog);
            // 
            // splitContainerCode
            // 
            this.splitContainerCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerCode.Name = "splitContainerCode";
            // 
            // splitContainerCode.Panel1
            // 
            this.splitContainerCode.Panel1.Controls.Add(this.panelOriginal);
            // 
            // splitContainerCode.Panel2
            // 
            this.splitContainerCode.Panel2.Controls.Add(this.panelObfuscated);
            this.splitContainerCode.SplitterDistance = 400;
            // 
            // panelOriginal
            // 
            this.panelOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelOriginal.Controls.Add(this.txtOriginal);
            this.panelOriginal.Controls.Add(this.lblOriginal);
            this.panelOriginal.BackColor = System.Drawing.Color.Black;
            // 
            // lblOriginal
            // 
            this.lblOriginal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOriginal.Text = "Original Code";
            this.lblOriginal.ForeColor = System.Drawing.Color.White;
            this.lblOriginal.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOriginal.Height = 25;
            this.lblOriginal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtOriginal
            // 
            this.txtOriginal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOriginal.BackColor = System.Drawing.Color.Black;
            this.txtOriginal.ForeColor = System.Drawing.Color.White;
            this.txtOriginal.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtOriginal.Name = "txtOriginal";
            this.txtOriginal.ReadOnly = true;
            this.txtOriginal.WordWrap = false;
            // 
            // panelObfuscated
            // 
            this.panelObfuscated.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelObfuscated.Controls.Add(this.txtObfuscated);
            this.panelObfuscated.Controls.Add(this.lblObfuscated);
            this.panelObfuscated.BackColor = System.Drawing.Color.Black;
            // 
            // lblObfuscated
            // 
            this.lblObfuscated.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblObfuscated.Text = "Obfuscated Code";
            this.lblObfuscated.ForeColor = System.Drawing.Color.White;
            this.lblObfuscated.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblObfuscated.Height = 25;
            this.lblObfuscated.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtObfuscated
            // 
            this.txtObfuscated.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtObfuscated.BackColor = System.Drawing.Color.Black;
            this.txtObfuscated.ForeColor = System.Drawing.Color.White;
            this.txtObfuscated.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtObfuscated.Name = "txtObfuscated";
            this.txtObfuscated.ReadOnly = true;
            this.txtObfuscated.WordWrap = false;
            // 
            // panelLog
            // 
            this.panelLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLog.Controls.Add(this.txtLog);
            this.panelLog.Controls.Add(this.lblLog);
            this.panelLog.BackColor = System.Drawing.Color.Black;
            // 
            // lblLog
            // 
            this.lblLog.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLog.Text = "Log";
            this.lblLog.ForeColor = System.Drawing.Color.White;
            this.lblLog.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblLog.Height = 25;
            this.lblLog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.ForeColor = System.Drawing.Color.White;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.WordWrap = false;
            // 1) Slow Mode checkbox
            this.chkSlowMode = new System.Windows.Forms.CheckBox();
            this.chkSlowMode.Location = new System.Drawing.Point(895, 10);
            this.chkSlowMode.Name = "chkSlowMode";
            this.chkSlowMode.Size = new System.Drawing.Size(80, 24);
            this.chkSlowMode.Text = "Slow Mode";
            this.chkSlowMode.UseVisualStyleBackColor = true;
            this.panelTop.Controls.Add(this.chkSlowMode);
            // 2) Next Step button (disabled unless slow mode is checked)
            this.btnNextStep = new System.Windows.Forms.Button();
            this.btnNextStep.Location = new System.Drawing.Point(985, 8);
            this.btnNextStep.Name = "btnNextStep";
            this.btnNextStep.Size = new System.Drawing.Size(75, 24);
            this.btnNextStep.Text = "Next";
            this.btnNextStep.UseVisualStyleBackColor = true;
            this.btnNextStep.Enabled = false;
            this.btnNextStep.Click += new System.EventHandler(this.btnNextStep_Click);
            this.panelTop.Controls.Add(this.btnNextStep);

            this.lblStepInfo = new System.Windows.Forms.Label();
            this.lblStepInfo.Location = new System.Drawing.Point(10, 40);
            this.lblStepInfo.Name = "lblStepInfo";
            this.lblStepInfo.Size = new System.Drawing.Size(1150, 20);
            this.lblStepInfo.ForeColor = System.Drawing.Color.Yellow;
            this.panelTop.Controls.Add(this.lblStepInfo);

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.panelTop);
            this.Text = "MultiObfuscator";
            // 
            // Resume Layout
            // 
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            this.splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerCode)).EndInit();
            this.splitContainerCode.Panel1.ResumeLayout(false);
            this.splitContainerCode.Panel2.ResumeLayout(false);
            this.splitContainerCode.ResumeLayout(false);
            this.panelOriginal.ResumeLayout(false);
            this.panelObfuscated.ResumeLayout(false);
            this.panelLog.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtPathToObfuscate;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnObfuscate;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.SplitContainer splitContainerCode;
        private System.Windows.Forms.Panel panelOriginal;
        private System.Windows.Forms.Label lblOriginal;
        private System.Windows.Forms.RichTextBox txtOriginal;
        private System.Windows.Forms.Panel panelObfuscated;
        private System.Windows.Forms.Label lblObfuscated;
        private System.Windows.Forms.RichTextBox txtObfuscated;
        private System.Windows.Forms.Panel panelLog;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox chkSlowMode;
        private System.Windows.Forms.Button btnNextStep;
        private System.Windows.Forms.Label lblStepInfo;


    }
}