using System;
using System.Collections.Generic;
using System.Text;

namespace GeometricAlgebra
{
    public class Join : Function
    {
        public Join() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{join}";

            return "join";
        }

        public override Operand New()
        {
            return new Join();
        }

        public override Operand EvaluationStep(Context context)
        {
            // TODO: Return join in frozen/factored state.

            return null;
        }
    }
}
