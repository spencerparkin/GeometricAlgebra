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

            return "rev";
        }

        public override string ShortDescription
        {
            get { return "Calculate the reverse of the given argument."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Calculate the reverse of the given argument.");
            context.Log("The reverse of a sum is the sum of the reverse of each element taken in the sum.");
            context.Log("The reverse of a versor is the product of its vector factors taken in reverse order.");
            context.Log("So the reverse of a blade can be defined in terms of a re-write of that blade as a sum of versors.");
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
                    return @"\left(" + operandList[0].Print(format, context) + @"\right)^{\sim}";
                case Format.PARSEABLE:
                    return base.Print(format, context);
            }

            return "?";
        }
    }
}