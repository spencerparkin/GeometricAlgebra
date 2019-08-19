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

        public static OuterProduct CalculateMeet(List<Operand> operandList, Context context)
        {
            OuterProduct outerJoin = Join.CalculateJoin(operandList, context);

            // The following works in a purely eucliden GA, but not if null vectors are present.
#if false
            List<Operand> subspaceList = new List<Operand>();
            for(int i = 0; i < operandList.Count; i++)
                subspaceList.Add(ExhaustEvaluation(new InnerProduct(new List<Operand>() { operandList[i].Copy(), outerJoin.Copy() }), context));

            Operand innerJoin = Join.CalculateJoin(subspaceList, context);
            Operand meet = ExhaustEvaluation(new InnerProduct(new List<Operand>() { innerJoin, outerJoin }), context);
#else
            OuterProduct meet = new OuterProduct();

            foreach(Operand vector in outerJoin.operandList)
            {
                bool containedInAllSubspaces = true;

                foreach(Operand blade in operandList)
                {
                    Operand operand = Operand.ExhaustEvaluation(new Trim(new List<Operand>() { new OuterProduct(new List<Operand>() { vector.Copy(), blade.Copy() }) }), context);
                    if(!operand.IsAdditiveIdentity)
                    {
                        containedInAllSubspaces = false;
                        break;
                    }
                }

                if(containedInAllSubspaces)
                    meet.operandList.Add(vector);
            }
#endif

            return meet;
        }
    }
}