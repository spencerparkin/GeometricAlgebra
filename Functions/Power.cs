using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Power : Function
    {
        public Power() : base()
        {
        }

        public Power(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Power();
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{power}";

            return "pow";
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 2)
                throw new MathException(string.Format("Power function expected exactly 2 arguments, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            Operand baseOperand = operandList[0];
            Operand exponentOperand = operandList[1];
            GeometricProduct geometricProduct = new GeometricProduct();

            if (exponentOperand is Sum sumExponent)
            {
                for(int i = 0; i < sumExponent.operandList.Count; i++)
                    geometricProduct.operandList.Add(new Power(new List<Operand>() { baseOperand.Copy(), sumExponent.operandList[i] }));

                return geometricProduct;
            }

            if(exponentOperand is NumericScalar numericScalarExponent)
            {
                if(baseOperand is NumericScalar numericScalarBase)
                {
                    return new NumericScalar(Math.Pow(numericScalarBase.value, numericScalarExponent.value));
                }
                else if(Math.Round(numericScalarExponent.value) == numericScalarExponent.value)
                {
                    for(int i = 0; i < (int)Math.Abs(numericScalarExponent.value); i++)
                        geometricProduct.operandList.Add(baseOperand.Copy());

                    if(numericScalarExponent.value >= 0.0)
                        return geometricProduct;

                    return new Inverse(new List<Operand>() { geometricProduct });
                }
            }

            geometricProduct.operandList.Add(exponentOperand);
            geometricProduct.operandList.Add(new Logarithm(new List<Operand>() { baseOperand }));
            return new Exponent(new List<Operand>() { geometricProduct });
        }

        public override string Print(Format format, Context context)
        {
            // TODO: Print x^{y}.
            return base.Print(format, context);
        }
    }

    public class Exponent : Function
    {
        public Exponent() : base()
        {
        }

        public Exponent(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Exponent();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\exp";

            return "exp";
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: If arg's a blade and its geometric square is a scalar and < 0, then write in terms of sine and cosine.
            return null;
        }
    }

    public class Logarithm : Function
    {
        public Logarithm() : base()
        {
        }

        public Logarithm(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Logarithm();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\log";

            return "log";
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: Solve e^x = y, for x in terms of y and e.
            return null;
        }
    }
}