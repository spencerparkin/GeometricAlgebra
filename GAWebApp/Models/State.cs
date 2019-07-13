using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeometricAlgebra;

namespace GAWebApp.Models
{
    public class HistoryItem
    {
        public string inputLatex;
        public string outputLatex;
        public string inputPlain;
        public string outputPlain;
        public string expression;
        public string error;

        public HistoryItem()
        {
            inputLatex = "?";
            outputLatex = "?";
            inputPlain = "?";
            outputPlain = "?";
            error = "";
        }
    }

    public class State
    {
        public EvaluationContext context;
        public List<HistoryItem> history;

        public State()
        {
            context = new GeometricAlgebra.ConformalModel.Conformal3D_EvaluationContext();
            history = new List<HistoryItem>();
        }

        public void Calculate(string expression)
        {
            HistoryItem item = new HistoryItem();

            // TODO: We should probably do this in a task and then wait with time-out.
            var result = Operand.Evaluate(expression, context);

            item.expression = expression;
            item.inputLatex = result.input == null ? "" : result.input.Print(Operand.Format.LATEX, context);
            item.outputLatex = result.output == null ? "" : result.output.Print(Operand.Format.LATEX, context);
            item.inputPlain = result.input == null ? "" : result.input.Print(Operand.Format.PARSEABLE, context);
            item.outputPlain = result.output == null ? "" : result.output.Print(Operand.Format.PARSEABLE, context);
            item.error = result.error;

            // Ugh...this fixes an encoding issue in the URIs, but sometimes a space is needed for valid latex.
            // Can we replace spaces with something else?
            item.inputLatex = item.inputLatex.Replace(" ", "");
            item.outputLatex = item.outputLatex.Replace(" ", "");

            history.Add(item);
        }

        public void RunScript(string script)
        {
            List<string> expressionList = script.Split(';').ToList();
            foreach(string expression in expressionList)
                if(expression.Length > 0)
                    Calculate(expression);
        }
    }
}
