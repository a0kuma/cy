using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace cy
{
    public partial class Form2 : Form
    {
        private TextBox inputTextBox;
        private Button okButton;
        private Button cancelButton;

        public string InputText { get; private set; }
        public Form2(string prompt, string title, string defaultValue = "")
        {
            Text = title;
            Label promptLabel = new Label() { Left = 10, Top = 10, Text = prompt, AutoSize = true };
            inputTextBox = new TextBox() { Left = 10, Top = 30, Width = 200, Text = defaultValue };
            okButton = new Button() { Text = "OK", Left = 10, Width = 100, Top = 60, DialogResult = DialogResult.OK };
            cancelButton = new Button() { Text = "Cancel", Left = 120, Width = 100, Top = 60, DialogResult = DialogResult.Cancel };

            okButton.Click += (sender, e) => { InputText = inputTextBox.Text; Close(); };
            cancelButton.Click += (sender, e) => { Close(); };

            Controls.Add(promptLabel);
            Controls.Add(inputTextBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(240, 100);
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        public static string Show(string prompt, string title, string defaultValue = "")
        {
            using (Form2 inputBox = new Form2(prompt, title, defaultValue))
            {
                return inputBox.ShowDialog() == DialogResult.OK ? inputBox.InputText : null;
            }
        }
    }
}
