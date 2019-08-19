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
            if(format == Format.LATEX)
                return @"\mbox{trim}";

            return "trim";
        }

        public override string ShortDescription
        {
            get { return "Round scalars to the nearest integer, provided they're within a small epsilon of that integer."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Round scalars to the nearest integer, provided they're within a small epsilon of that integer.");
            context.Log("This function is useful in dealing with round-off error.");
            context.Log("For example, if a result should essentially be zero, this function can be used to get zero.");
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
                collectable.freezeFlags &= ~FreezeFlag.ALL;
                return collectable;
            }

            if(operand is Matrix matrix)
                return new Matrix(matrix, typeof(Trim));

            if(operand is NumericScalar scalar)
            {
                double roundedValue = Math.Round(scalar.value);
                if(Math.Abs(scalar.value - roundedValue) < context.epsilon)
                {
                    scalar.value = roundedValue;
                    return scalar;
                }
            }

            return operand;
        }
    }
}
