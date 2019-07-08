using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeometricAlgebra;

namespace GAWebApp.Models
{
    public class State
    {
        public EvaluationContext context;
        public List<KeyValuePair<string, string>> history;
        public string errorMessage;

        public State()
        {
            context = new EvaluationContext();
            history = new List<KeyValuePair<string, string>>();
        }

        public void Calculate(string expression)
        {
            try
            {
                // TODO: We should probably do this in a task and then wait with time-out.
                errorMessage = null;

                Parser parser = new Parser();
                Operand operand = parser.Parse(expression);
                string inputExpression = operand.Print(Operand.Format.LATEX);

                operand = Operand.FullyEvaluate(operand, context);
                string outputExpression = operand.Print(Operand.Format.LATEX);

                history.Add(new KeyValuePair<string, string>(inputExpression, outputExpression));
            }
            catch (Exception exc)
            {
                // TODO: We need to display this somewhere on the page.
                errorMessage = exc.Message;
            }
        }
    }
}
