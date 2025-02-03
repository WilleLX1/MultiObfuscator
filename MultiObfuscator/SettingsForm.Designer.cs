namespace MultiObfuscator
{
    partial class SettingsForm
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
            numericDeadCodeProbability = new NumericUpDown();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            checkBoxStringSplitting = new CheckBox();
            checkBoxControlFlowFlattening = new CheckBox();
            btnSave = new Button();
            ((System.ComponentModel.ISupportInitialize)numericDeadCodeProbability).BeginInit();
            SuspendLayout();
            // 
            // numericDeadCodeProbability
            // 
            numericDeadCodeProbability.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numericDeadCodeProbability.Location = new Point(181, 52);
            numericDeadCodeProbability.Name = "numericDeadCodeProbability";
            numericDeadCodeProbability.Size = new Size(120, 27);
            numericDeadCodeProbability.TabIndex = 0;
            numericDeadCodeProbability.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(12, 54);
            label1.Name = "label1";
            label1.Size = new Size(163, 20);
            label1.TabIndex = 1;
            label1.Text = "Dead Code Probability:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(12, 98);
            label2.Name = "label2";
            label2.Size = new Size(166, 20);
            label2.TabIndex = 2;
            label2.Text = "Control Flow Flattening:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(12, 149);
            label3.Name = "label3";
            label3.Size = new Size(111, 20);
            label3.TabIndex = 3;
            label3.Text = "String Splitting:";
            // 
            // checkBoxStringSplitting
            // 
            checkBoxStringSplitting.AutoSize = true;
            checkBoxStringSplitting.Checked = true;
            checkBoxStringSplitting.CheckState = CheckState.Checked;
            checkBoxStringSplitting.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxStringSplitting.Location = new Point(129, 155);
            checkBoxStringSplitting.Name = "checkBoxStringSplitting";
            checkBoxStringSplitting.Size = new Size(15, 14);
            checkBoxStringSplitting.TabIndex = 4;
            checkBoxStringSplitting.UseVisualStyleBackColor = true;
            // 
            // checkBoxControlFlowFlattening
            // 
            checkBoxControlFlowFlattening.AutoSize = true;
            checkBoxControlFlowFlattening.Checked = true;
            checkBoxControlFlowFlattening.CheckState = CheckState.Checked;
            checkBoxControlFlowFlattening.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkBoxControlFlowFlattening.Location = new Point(184, 104);
            checkBoxControlFlowFlattening.Name = "checkBoxControlFlowFlattening";
            checkBoxControlFlowFlattening.Size = new Size(15, 14);
            checkBoxControlFlowFlattening.TabIndex = 5;
            checkBoxControlFlowFlattening.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnSave.Location = new Point(62, 223);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(187, 56);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(309, 305);
            Controls.Add(btnSave);
            Controls.Add(checkBoxControlFlowFlattening);
            Controls.Add(checkBoxStringSplitting);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(numericDeadCodeProbability);
            Name = "SettingsForm";
            Text = "SettingsForm";
            ((System.ComponentModel.ISupportInitialize)numericDeadCodeProbability).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private NumericUpDown numericDeadCodeProbability;
        private Label label1;
        private Label label2;
        private Label label3;
        private CheckBox checkBoxStringSplitting;
        private CheckBox checkBoxControlFlowFlattening;
        private Button btnSave;
    }
}