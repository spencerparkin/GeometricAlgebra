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

        public Join(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{join}";

            return "join";
        }

        public override string ShortDescription
        {
            get { return "Compute the join of the given blades."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Blades are represenative of vector sub-spaces.");
            context.Log("This function computes a blade representative of the union of such vector sub-spaces.");
            context.Log("The magnitude of the computed blade will be non-zero, but otherwise left undefined.");
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
            
            int j = -1;

            for (int i = 0; i < bladeArray.Length; i++)
            {
                try
                {
                    bladeArray[i] = FactorBlade.Factor(operandList[i], context);

                    if(j < 0 || bladeArray[i].Grade > bladeArray[j].Grade)
                        j = i;
                }
                catch (MathException exc)
                {
                    throw new MathException($"Failed to factor argument {i} as blade.", exc);
                }
            }

            OuterProduct join = bladeArray[j];

            for(int i = 0; i < bladeArray.Length; i++)
            {
                if(i != j)
                {
                    foreach(Operand vector in bladeArray[i].operandList)
                    {
                        Operand operand = Operand.ExhaustEvaluation(new Trim(new List<Operand>() { new OuterProduct(new List<Operand>() { vector.Copy(), join.Copy() }) }), context);
                        if (!operand.IsAdditiveIdentity)
                            join.operandList.Add(vector);
                    }
                }
            }

            return join;
        }
    }
}
