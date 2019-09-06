using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class Reset : Function
    {
        public Reset() : base()
        {
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{reset}";

            return "reset";
        }

        public override string ShortDescription
        {
            get { return "Delete all user variables and regenerate built-in variables."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 0)
                throw new MathException("The reset function does not take any arguments.");

            context.operandStorage.ClearStorage();
            context.GenerateDefaultStorage();

            return null;
        }
    }
}
