using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    class Sequence : Operation
    {
        public Sequence() : base()
        {
        }

        public override bool IsAssociative()
        {
            return true;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override Operand New()
        {
            return new Sequence();
        }

        // The only purpose of this operation is to evaluate its operands left to right.  The last operand is returned as the result.
        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            return operandList.Count > 0 ? operandList[operandList.Count - 1] : null;
        }

        public override string PrintJoiner(Format format)
        {
            return "; ";
        }
    }
}
