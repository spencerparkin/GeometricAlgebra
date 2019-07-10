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
        public List<(string, string)> history;
        public string errorMessage;

        public State()
        {
            context = new GeometricAlgebra.ConformalModel.Conformal3D_EvaluationContext();
            history = new List<(string, string)>();
        }

        public void Calculate(string expression)
        {
            try
            {
                // TODO: We should probably do this in a task and then wait with time-out.
                errorMessage = null;

                Parser parser = new Parser();
                Operand operand = parser.Parse(expression);
                string inputExpression = operand.Print(Operand.Format.LATEX, context);

                operand = Operand.FullyEvaluate(operand, context);
                string outputExpression = operand.Print(Operand.Format.LATEX, context);

                // Ugh...this fixes an encoding issue in the URIs, but sometimes a space is needed for valid latex.
                inputExpression = inputExpression.Replace(" ", "");
                outputExpression = outputExpression.Replace(" ", "");

                history.Add((inputExpression, outputExpression));
            }
            catch (Exception exc)
            {
                // TODO: We need to display this somewhere on the page.
                errorMessage = exc.Message;
            }
        }
    }
}
