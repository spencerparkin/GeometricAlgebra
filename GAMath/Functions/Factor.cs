using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Factor : Function
    {
        public Factor() : base()
        {
        }

        public Factor(List<Operand> operandList) : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override Operand New()
        {
            return new Factor();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{factor}";

            return "factor";
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new MathException(string.Format("Factor operation expects exactly one operand, got {0}.", operandList.Count));

            operand = operandList[0];
            
            if(operand.Grade < 0)
                throw new MathException("Unable to determine grade of argument.");
            else if(operand.Grade == 0)
                throw new MathException("Polynomial factorization not yet implemented.");
            else
            {
                // TODO: Try to factor the multivector as a blade here.
            }

            return null;
        }
    }
}
