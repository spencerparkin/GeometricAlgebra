using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Sine : Function
    {
        public Sine() : base()
        {
        }

        public Sine(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\sin";

            return "sin";
        }

        public override string ShortDescription
        {
            get { return "Calculate the trigonemetric sine of the given argument."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Sine function expected exactly one argument, got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            operand = operandList[0];
            if(operand is NumericScalar numericScalar)
                return new NumericScalar(Math.Sin(numericScalar.value));

            // TODO: What's the sine of a multivector?!  Taylor series expansion?
            return null;
        }
    }

    public class Cosine : Function
    {
        public Cosine() : base()
        {
        }

        public Cosine(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\cos";

            return "cos";
        }

        public override string ShortDescription
        {
            get { return "Calculate the trigonemetric cosine of the given argument."; }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException($"Cosine function expected exactly one argument, got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            operand = operandList[0];
            if (operand is NumericScalar numericScalar)
                return new NumericScalar(Math.Cos(numericScalar.value));

            // TODO: What's the cosine of a multivector?!  Taylor series expansion?
            return null;
        }
    }
}