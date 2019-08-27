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

        public override string ShortDescription
        {
            get { return "Calculate the first argument raised to the power of the second."; }
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
                    if(numericScalarBase.value < 0.0 && numericScalarExponent.value < 0.0)
                        throw new MathException("Imaginary root avoided in power computation.");

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
            if (operandList.Count != 2 || format != Format.LATEX)
                return base.Print(format, context);

            Operand baseOperand = operandList[0];
            Operand exponentOperand = operandList[1];

            if(!(exponentOperand is NumericScalar scalar))
                return base.Print(format, context);
            
            if(Math.Abs(scalar.value - Math.Round(scalar.value)) < context.epsilon)
            {
                int exponent = (int)Math.Round(scalar.value);
                if(exponent == 1)
                    return @"\left(" + baseOperand.Print(format, context) + @"\right)";
                else
                    return @"\left(" + baseOperand.Print(format, context) + @"\right)^{" + exponent.ToString() + "}";
            }
            else if(Math.Abs(1.0 / scalar.value - Math.Round(1.0 / scalar.value)) < context.epsilon)
            {
                int root = (int)Math.Round(1.0 / scalar.value);
                if(root == 2)
                    return @"\sqrt{" + baseOperand.Print(format, context) + "}";
                else
                    return @"\sqrt[" + root.ToString() + "]{" + baseOperand.Print(format, context) + "}";
            }

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

        public override string ShortDescription
        {
            get { return "Calculate e raised to the power of the given argument."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException(string.Format("Exponential function expected exactly 1 argument, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            Operand exponentOperand = operandList[0];

            if (exponentOperand is Sum sumExponent)
            {
                GeometricProduct geometricProduct = new GeometricProduct();

                for (int i = 0; i < sumExponent.operandList.Count; i++)
                    geometricProduct.operandList.Add(new Exponent(new List<Operand>() { sumExponent.operandList[i] }));

                return geometricProduct;
            }

            if(exponentOperand is NumericScalar numericScalar)
            {
                return new NumericScalar(Math.Exp(numericScalar.value));
            }
            else if(exponentOperand is Blade blade)
            {
                Blade basisBlade = new Blade(new NumericScalar(1.0), blade.vectorList.ToList());
                InnerProduct innerProduct = new InnerProduct();
                innerProduct.operandList.Add(new NumericScalar(-1.0));
                innerProduct.operandList.Add(basisBlade.Copy());
                innerProduct.operandList.Add(basisBlade.Copy());
                Operand result = Operand.ExhaustEvaluation(innerProduct, context);
                if(result.IsMultiplicativeIdentity)
                {
                    Sum sum = new Sum();
                    sum.operandList.Add(new Cosine(new List<Operand>() { blade.scalar.Copy() }));
                    sum.operandList.Add(new GeometricProduct(new List<Operand>() { basisBlade.Copy(), new Sine(new List<Operand>() { blade.scalar.Copy() }) }));
                    return sum;
                }
                else if(result.IsAdditiveIdentity)
                {
                    // What if it's a null blade?
                }
                else
                {
                    // Hyperbolic cosine/sine?
                }
            }

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

        public override string ShortDescription
        {
            get { return "Calculate the natural logarithm of the given argument."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException(string.Format("Logarithm function expected exactly 1 argument, got {0}.", operandList.Count));

            Operand operand = operandList[0];
            if (operand is GeometricProduct geometricProduct)
            {
                Sum sum = new Sum();
                foreach (Operand factor in geometricProduct.operandList)
                    sum.operandList.Add(new Logarithm(new List<Operand>() { factor }));

                return sum;
            }

            operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            operand = operandList[0];

            if(operand is Collectable collectable)
            {
                Operand scalar = collectable.scalar;
                if(!scalar.IsMultiplicativeIdentity)
                {
                    collectable.scalar = new NumericScalar(1.0);
                    return new Sum(new List<Operand>() { new Logarithm(new List<Operand>() { scalar }), new Logarithm(new List<Operand>() { collectable }) });;
                }
            }

            if(operand is NumericScalar numericScalar)
            {
                return new NumericScalar(Math.Log(numericScalar.value));
            }

            if(operand is Inverse inverse && inverse.operandList.Count == 1)
            {
                geometricProduct = new GeometricProduct();
                geometricProduct.operandList.Add(new NumericScalar(-1.0));
                geometricProduct.operandList.Add(new Logarithm(new List<Operand>() { inverse.operandList[0] }));
                return geometricProduct;
            }

            // TODO: Solve e^x = y, for x in terms of y and e.
            return null;
        }
    }

    public class SquareRoot : Function
    {
        public SquareRoot() : base()
        {
        }

        public SquareRoot(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new SquareRoot();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{sqrt}";

            return "sqrt";
        }

        public override string ShortDescription
        {
            get { return "Calculate the square root of the given argument."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException(string.Format("Square-root function expected exactly 1 argument, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            return new Power(new List<Operand>() { operandList[0], new NumericScalar(0.5) });
        }

        public override string Print(Format format, Context context)
        {
            if(operandList.Count != 1 || format != Format.LATEX)
                return base.Print(format, context);

            return @"\sqrt{" + operandList[0].Print(format, context) + "}";
        }
    }
}