using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class Help : Function
    {
        public Help() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{help}";

            return "help";
        }

        public override string ShortDescription
        {
            get { return "Provide help for given functions."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Invoke this function with no arguments for a short description of all functions.");
            context.Log("Invoke this function with one or more function instances for detailed help on each one.");
        }

        public override Operand EvaluationStep(Context context)
        {
            if(!operandList.All(operand => operand is Function))
                throw new MathException("Help expects all arguments, if any, to be functions.");

            if(operandList.Count == 0)
            {
                foreach(Function function in context.GenerateFunctionList().OrderBy(function => function.Name(Format.PARSEABLE)))
                {
                    context.Log($"{function.Name(Format.PARSEABLE)} -- {function.ShortDescription}");
                }
            }
            else
            {
                foreach(Function function in operandList.Where(operand => operand is Function).Select(operand => operand as Function).OrderBy(function => function.Name(Format.PARSEABLE)))
                {
                    context.Log(function.Name(Format.PARSEABLE) + ":");
                    function.LogDetailedHelp(context);
                }
            }

            return new NumericScalar(0.0);
        }
    }
}
