using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class Meet : Function
    {
        public Meet() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{meet}";

            return "meet";
        }

        public override Operand New()
        {
            return new Meet();
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: This can be calculated from the join.  Return in frozen/factored state.

            return null;
        }
    }
}