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
            // TODO: Use x^y = 1/(x^(-y)) if needed.
            // TODO: Use x^{y+z} = (x^y)(x^z) if needed.
            // TODO: Use x^y = exp(log(x^y)) = exp(y log(x)) if needed.
            return null;
        }

        public override string Print(Format format, Context context)
        {
            // TODO: x^{y}.
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