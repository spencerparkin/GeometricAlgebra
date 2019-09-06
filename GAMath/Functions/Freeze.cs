using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class Freeze : Function
    {
        public Freeze() : base()
        {
        }

        public override string ShortDescription => "Return the given argument without evaluation, unless it's a variable.";

        public override string Name(Format format)
        {
            return (format == Format.LATEX) ? @"\mbox{freeze}" : "freeze";
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Freeze function expected exactly 1 argument; got {operandList.Count}.");

            Operand operand = operandList[0];
            if(operand is Variable)
            {
                operand = base.EvaluationStep(context);
                if(operand != null)
                    return operand;
            }

            operand.freezeFlags |= FreezeFlag.BAIL;
            return operand;
        }
    }
}