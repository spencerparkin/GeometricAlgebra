using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class FactorVersor : Function
    {
        public FactorVersor() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{factor\_versor}";

            return "factor_versor";
        }

        public override Operand New()
        {
            return new FactorVersor();
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: Write this.  Make use of BladeFactor.Factor function.

            return null;
        }
    }
}
