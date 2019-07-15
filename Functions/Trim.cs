using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Trim : Function
    {
        public Trim() : base()
        {
        }

        public Trim(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Trim();
        }

        public override string Name(Format format)
        {
            return "trim";
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException(string.Format("Trim function expected exactly 1 operand, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            operand = operandList[0];

            if(operand is Operation operation)
            {
                for(int i = 0; i < operation.operandList.Count; i++)
                    operation.operandList[i] = new Trim(new List<Operand>() { operation.operandList[i] });

                return operation;
            }

            if(operand is Collectable collectable)
            {
                collectable.scalar = new Trim(new List<Operand>() { collectable.scalar });
                return collectable;
            }

            if(operand is NumericScalar scalar)
            {
                double epsilon = 1e-7;
                if(Math.Abs(scalar.value) < epsilon)
                {
                    scalar.value = 0.0;
                    return scalar;
                }
            }

            return operand;
        }
    }
}
