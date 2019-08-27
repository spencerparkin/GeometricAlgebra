using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Adjugate : Function
    {
        public Adjugate() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{adj}";

            return "adj";
        }

        public override Operand New()
        {
            return new Adjugate();
        }

        public override string ShortDescription
        {
            get { return "Calculate the adjugate/adjoint of the given matrix."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException($"Adjugate function expected exactly 1 argument, got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            operand = operandList[0];
            if (operand is Matrix matrix)
                return matrix.Adjugate();

            return operand;
        }
    }
}
