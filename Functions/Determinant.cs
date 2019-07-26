using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Determinant : Function
    {
        public Determinant() : base()
        {
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{det}";

            return "det";
        }

        public override string Print(Format format, Context context)
        {
            if(operandList.Count == 1 && operandList[0] is Matrix matrix)
            {
                return matrix.PrintLatex(@"\left|", @"\right|", context);
            }

            return base.Print(format, context);
        }

        public override Operand New()
        {
            return new Determinant();
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Determinant function expected exactly 1 argument, got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            operand = operandList[0];
            if(operand is Matrix matrix)
                return matrix.Determinant();

            return operand;
        }
    }
}
