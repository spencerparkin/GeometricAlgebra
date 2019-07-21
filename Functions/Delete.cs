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

        public override Operand New()
        {
            return new Delete();
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{del}";

            return "del";
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException(string.Format("Delete expected exactly 1 opreand, got {0}.", operandList.Count));

            Operand operand = operandList[0];
            if(!(operand is Variable variable))
                throw new MathException("Delete expected operand to be a variable.");

            context.ClearStorage(variable.name);
            return operand;
        }
    }
}
