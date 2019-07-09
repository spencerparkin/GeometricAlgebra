using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    class PolynomialFactor : Operation
    {
        public PolynomialFactor() : base()
        {
        }

        public override Operand New()
        {
            return new PolynomialFactor();
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            // TODO: Here we evaluate our argument until, if ever, we find that it
            //       becomes a symbolic polynomial in the scalar algebra.  In that case,
            //       we attempt to factor the polynomial in whole or in parts recursively.
            //       Note that to preserve our result, we must have a way of telling
            //       the evaluation algorithm to terminate once we've done our work.
            return null;
        }
    }
}
