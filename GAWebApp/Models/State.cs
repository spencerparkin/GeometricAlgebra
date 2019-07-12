using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeometricAlgebra;

namespace GAWebApp.Models
{
    public class HistoryItem
    {
        public string input;
        public string output;
        public string error;

        public HistoryItem()
        {
            input = "?";
            output = "?";
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

            item.input = result.input == null ? "" : result.input.Print(Operand.Format.LATEX, context);
            item.output = result.output == null ? "" : result.output.Print(Operand.Format.LATEX, context);
            item.error = result.error;

            // Ugh...this fixes an encoding issue in the URIs, but sometimes a space is needed for valid latex.
            // Can we replace spaces with something else?
            item.input = item.input.Replace(" ", "");
            item.output = item.output.Replace(" ", "");

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
