using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Delete : Function
    {
        public Delete() : base()
        {
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{del}";

            return "del";
        }

        public override string ShortDescription
        {
            get { return "Delete the given variables."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Delete the given variables.");
            context.Log("Once deleted, the variables are treated as an unknown multivector values.");
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count == 0)
                throw new MathException(string.Format("Delete expected at least one operand, got {0}.", operandList.Count));

            for(int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                if(!(operand is Variable variable))
                    throw new MathException($"Delete expected operand {i + 1} to be a variable.");

                context.operandStorage.ClearStorage(variable.name);
            }

            return new NumericScalar(0.0);
        }
    }
}
