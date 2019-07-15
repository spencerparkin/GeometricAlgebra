using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Reverse : Function
    {
        public Reverse() : base()
        {
        }

        public Reverse(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            // The reverse of a sum is the sum of the reverses.
            return operation is Sum;
        }

        public override Operand New()
        {
            return new Reverse();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{reverse}";

            return "reverse";
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new MathException(string.Format("Reverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].Grade == 0)
                return operandList[0];

            Operand reverse = operandList[0].Reverse();
            return reverse;
        }

        public override string Print(Format format, Context context)
        {
            if (operandList.Count != 1)
                return "?";

            switch (format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format, context) + @"\right)^{\tilde}";
                case Format.PARSEABLE:
                    return base.Print(format, context);
            }

            return "?";
        }
    }
}