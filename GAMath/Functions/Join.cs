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
            if (operandList.Count <= 0)
                throw new MathException(string.Format("Join operation expects one or more operands, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            return CalculateJoin(this.operandList, context);
        }

        public static Operand CalculateJoin(List<Operand> operandList, Context context)
        {
            OuterProduct[] bladeArray = new OuterProduct[operandList.Count];
            
            for (int i = 0; i < bladeArray.Length; i++)
            {
                try
                {
                    bladeArray[i] = FactorBlade.Factor(operandList[i], context);
                }
                catch (MathException exc)
                {
                    throw new MathException($"Failed to factor argument {i} as blade.", exc);
                }
            }

            OuterProduct join = new OuterProduct();

            for(int i = 0; i < bladeArray.Length; i++)
            {
                foreach(Operand vector in bladeArray[i].operandList)
                {
                    Operand operand = Operand.ExhaustEvaluation(new Trim(new List<Operand>() { new OuterProduct(new List<Operand>() { vector.Copy(), join.Copy() }) }), context);
                    if (!operand.IsAdditiveIdentity)
                        join.operandList.Add(vector.Copy());
                }
            }

            return join;
        }
    }
}
