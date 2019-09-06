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
            get { return "Delete the given variable."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Delete the given variable.");
            context.Log("Once deleted, the variable is treated as an unknown multivector value.");
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException(string.Format("Delete expected exactly 1 opreand, got {0}.", operandList.Count));

            Operand operand = operandList[0];
            if(!(operand is Variable variable))
                throw new MathException("Delete expected operand to be a variable.");

            context.operandStorage.ClearStorage(variable.name);
            return operand;
        }
    }
}
