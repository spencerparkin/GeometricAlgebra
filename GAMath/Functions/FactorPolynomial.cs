using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class FactorPolynomial : Function
    {
        public FactorPolynomial() : base()
        {
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{factor\_poly}";

            return "factor_poly";
        }

        public override Operand New()
        {
            return new FactorPolynomial();
        }

        public override string ShortDescription
        {
            get { return "Factor the given polynomial (not yet implemented.)"; }
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: I have absolutely no idea how to do this and all the literature about the subject scares me.
            //       The naive approach, of course, is to find zeros, but that would probably quickly become impractical.
            return null;
        }
    }
}
