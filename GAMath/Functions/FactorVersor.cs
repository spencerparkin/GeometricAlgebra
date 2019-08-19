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

        public override string ShortDescription
        {
            get { return "Factor the given versor as a geometric product of vectors."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("An attempt is made to factor the given multivector as a geometric product of vectors.");
            context.Log("If it is a versor, the factorization procedure should succeed.");
            context.Log("If it is not a versor, an error is generated.");
            context.Log("The computed factorization, when expanded, should always recover the originally given multivector.");
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: Write this.  Make use of BladeFactor.Factor function.

            return null;
        }
    }
}
