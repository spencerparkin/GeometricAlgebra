using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
            if (operandList.Count <= 0)
                throw new MathException(string.Format("Meet operation expects one or more operands, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            return CalculateMeet(this.operandList, context);
        }

        public static Operand CalculateMeet(List<Operand> operandList, Context context)
        {
            // The following works in a purely euclidean GA, but not if null vectors are present.
#if false
            Operand outerJoin = Join.CalculateJoin(operandList, context);

            List<Operand> subspaceList = new List<Operand>();
            for(int i = 0; i < operandList.Count; i++)
                subspaceList.Add(ExhaustEvaluation(new InnerProduct(new List<Operand>() { operandList[i].Copy(), outerJoin.Copy() }), context));

            Operand innerJoin = Join.CalculateJoin(subspaceList, context);
            Operand meet = ExhaustEvaluation(new InnerProduct(new List<Operand>() { innerJoin, outerJoin }), context);
#else
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

            List<Operand> vectorList = new List<Operand>();

            for(int i = 0; i < bladeArray.Length; i++)
            {
                OuterProduct blade = bladeArray[i];

                foreach(Operand vector in blade.operandList)
                {
                    bool containdInAllBlades = true;

                    for(int j = 0; j < bladeArray.Length; j++)
                    {
                        if(j != i)
                        {
                            Operand operand = ExhaustEvaluation(new Trim(new List<Operand>() { new OuterProduct(new List<Operand>() { vector.Copy(), bladeArray[j].Copy() }) }), context);
                            if(!operand.IsAdditiveIdentity)
                            {
                                containdInAllBlades = false;
                                break;
                            }
                        }
                    }

                    if(containdInAllBlades)
                        vectorList.Add(vector);
                }
            }

            Operand meet = Join.CalculateJoin(vectorList, context);
#endif
            return meet;
        }
    }
}