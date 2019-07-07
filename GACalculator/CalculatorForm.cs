using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeometricAlgebra;

namespace GACalculator
{
    public partial class CalculatorForm : Form
    {
        private EvaluationContext context;

        public CalculatorForm()
        {
            context = new EvaluationContext();

            InitializeComponent();

            inputTextBox.KeyDown += new KeyEventHandler(InputTextBox_KeyDown);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("GA Calculator -- (c) 2019", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InputTextBox_TextChanged(object sender, EventArgs e)
        {
        }
        
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter && inputTextBox.Text.Length > 0)
            {
                try
                {
                    string expression = inputTextBox.Text;
                    Parser parser = new Parser();

                    Operand operand = parser.Parse(expression);
                    outputTextBox.AppendText("Input: " + operand.Print(Operand.Format.PARSEABLE) + "\r\n\r\n");

                    operand = Operand.FullyEvaluate(operand, context);
                    outputTextBox.AppendText("Output: " + operand.Print(Operand.Format.PARSEABLE) + "\r\n\r\n");

                    inputTextBox.Clear();
                }
                catch(Exception exc)
                {
                    outputTextBox.Text += "Error: " + exc.ToString() + "\n\n";
                }
            }
        }
    }
}
