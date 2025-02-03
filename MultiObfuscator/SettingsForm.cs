using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiObfuscator
{
    public partial class SettingsForm : Form
    {
        public ObfuscatorSettings Settings { get; private set; }

        public SettingsForm(ObfuscatorSettings settings)
        {
            InitializeComponent();
            // Store a reference (or clone) of the settings.
            Settings = settings;

            // Initialize controls with current settings.
            numericDeadCodeProbability.Value = (decimal)Settings.DeadCodeProbability * 100;
            checkBoxControlFlowFlattening.Checked = Settings.EnableControlFlowFlattening;
            checkBoxStringSplitting.Checked = Settings.EnableStringSplitting;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Save values from the controls back to the settings.
            Settings.DeadCodeProbability = (double)numericDeadCodeProbability.Value / 100;
            Settings.EnableControlFlowFlattening = checkBoxControlFlowFlattening.Checked;
            Settings.EnableStringSplitting = checkBoxStringSplitting.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
