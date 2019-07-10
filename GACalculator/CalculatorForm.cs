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
        private List<string> history = new List<string>();
        private int historyLocation = 0;

        public CalculatorForm()
        {
            context = new GeometricAlgebra.ConformalModel.Conformal3D_EvaluationContext();

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
            if (e.KeyCode == Keys.Enter && inputTextBox.Text.Length > 0)
            {
                try
                {
                    string expression = inputTextBox.Text;
                    Parser parser = new Parser(context);

                    Operand operand = parser.Parse(expression);
                    outputTextBox.AppendText("Input: " + operand.Print(Operand.Format.PARSEABLE) + "\r\n\r\n");

                    operand = Operand.FullyEvaluate(operand, context, true);
                    outputTextBox.AppendText("Output: " + operand.Print(Operand.Format.PARSEABLE) + "\r\n\r\n");

                    inputTextBox.Clear();

                    history.Add(expression);
                    historyLocation = 0;
                }
                catch (Exception exc)
                {
                    outputTextBox.Text += "Error: " + exc.Message + "\n\n";
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (history.Count > 0 && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
            {
                if (e.KeyCode == Keys.Up)
                {
                    historyLocation--;
                    if (historyLocation < 0)
                        historyLocation = history.Count - 1;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    historyLocation++;
                    if (historyLocation > history.Count - 1)
                        historyLocation = 0;
                }

                inputTextBox.Text = history[historyLocation];
            }
        }
    }
}
