using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class Magnitude : Function
    {
        public Magnitude() : base()
        {
        }

        public Magnitude(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{mag}";

            return "mag";
        }

        public override string ShortDescription => "Calculate the magnitude of the given multivector.";

        public override string Print(Format format, Context context)
        {
            if(operandList.Count != 1 || format == Format.PARSEABLE)
                return base.Print(format, context);

            return @"\left\|" + operandList[0].Print(format, context) + @"\right\|";
        }

        // Note that we should always return a non-negative scalar here.
        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Magnitude function expected exactly one argument; got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            operand = operandList[0];

            int grade = operand.Grade;
            if(grade == 0)
            {
                if(operand is NumericScalar numericScalar)
                    return new NumericScalar(Math.Abs(numericScalar.value));
                else
                    return null;
            }
            else
            {
                Operand innerProduct = ExhaustEvaluation(new InnerProduct(new List<Operand>() { operand.Copy(), new Reverse(new List<Operand>() { operand.Copy() }) }), context);
                if (innerProduct.Grade == 0)
                {
                    if (innerProduct is NumericScalar numericScalar)
                        return new NumericScalar(Math.Sqrt(Math.Abs(numericScalar.value)));
                    else
                    {
                        operandList[0] = new SquareRoot(new List<Operand>() { innerProduct });
                        return this;
                    }
                }

                throw new MathException("Inner product of multivector with its reverse did not yield a scalar.");
            }
        }
    }
}
