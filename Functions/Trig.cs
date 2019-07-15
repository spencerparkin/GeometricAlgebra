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

        public override Operand New()
        {
            return new Sine();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\sin";

            return "sin";
        }

        public override Operand EvaluationStep(Context context)
        {
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

        public override Operand New()
        {
            return new Cosine();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\cos";

            return "cos";
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: What's the cosine of a multivector?!  Taylor series expansion?
            return null;
        }
    }
}