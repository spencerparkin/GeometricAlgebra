using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class FactorDot : Function
    {
        public FactorDot() : base()
        {
        }

        public FactorDot(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            return format == Format.LATEX ? @"\mbox{factor_dot}" : "factor_dot";
        }

        public override string ShortDescription => "Factor symbolic vectors in terms of the inner product and basis vectors.";

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException(string.Format("Factor operation expected exactly one operand; got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            List<string> basisVectorList = context.ReturnBasisVectors();
            Sum sum = Operand.CanonicalizeMultivector(operandList[0].Copy());

            // Go find all the symbolic vectors involved in dot products.
            HashSet<string> symbolicVectorNameSet = new HashSet<string>();
            foreach(Blade blade in sum.operandList.Select(term => term as Blade))
            {
                if(blade.scalar is SymbolicScalarTerm scalarTerm)
                {
                    foreach(SymbolicScalarTerm.Factor factor in scalarTerm.factorList)
                    {
                        if(factor is SymbolicScalarTerm.SymbolicDot symbolicDot)
                        {
                            if(!basisVectorList.Contains(symbolicDot.vectorNameA))
                                symbolicVectorNameSet.Add(symbolicDot.vectorNameA);

                            if (!basisVectorList.Contains(symbolicDot.vectorNameB))
                                symbolicVectorNameSet.Add(symbolicDot.vectorNameB);
                        }
                    }
                }
            }

            // Try factoring each symbolic vector out in terms of the inner product until we find one that works.
            foreach(string vectorName in symbolicVectorNameSet)
            {
                Operand result = FactorScalar(vectorName, sum, context);
                if(result != null)
                    return result;

                result = FactorBlade(vectorName, sum, context);
                if(result != null)
                    return result;
            }

            return operandList[0];
        }

        private Operand FactorScalar(string vectorName, Sum sum, Context context)
        {
            List<string> basisVectorList = context.ReturnBasisVectors();

            Sum remainderSum = sum.Copy() as Sum;
            Sum modifiedSum = new Sum();

            foreach(string basisVectorName in basisVectorList)
            {
                if(context.BilinearForm(vectorName, basisVectorName).IsAdditiveIdentity)
                    continue;

                bool found = false;

                foreach (Blade blade in remainderSum.operandList.Select(term => term as Blade))
                {
                    if (blade.Grade == 0 && blade.scalar is SymbolicScalarTerm scalarTerm)
                    {
                        foreach (SymbolicScalarTerm.Factor factor in scalarTerm.factorList)
                        {
                            if (factor is SymbolicScalarTerm.SymbolicDot symbolicDot && symbolicDot.InvolvesVectors(vectorName, basisVectorName))
                            {
                                remainderSum.operandList.Remove(blade);
                                scalarTerm.factorList.Remove(symbolicDot);
                                blade.vectorList.Add(basisVectorName);
                                modifiedSum.operandList.Add(blade);
                                found = true;
                                break;
                            }
                        }
                    }

                    if(found)
                        break;
                }

                if(!found)
                    return null;
            }

            Sum resultSum = new Sum();
            resultSum.operandList.Add(new InnerProduct(new List<Operand>() { new Blade(vectorName), new FactorDot(new List<Operand>() { modifiedSum }) }));
            resultSum.operandList.Add(new FactorDot(new List<Operand>() { remainderSum }));
            return resultSum;
        }

        private Operand FactorBlade(string vectorName, Sum sum, Context context)
        {
            List<string> basisVectorList = context.ReturnBasisVectors();
            basisVectorList = basisVectorList.Where(basisVectorName => !context.BilinearForm(vectorName, basisVectorName).IsAdditiveIdentity).ToList();

            //Sum remainderSum = sum.Copy() as Sum;
            //Sum modifiedSum = new Sum();

            // account for sign in extraction...
            // might have r = (y.e2)e2^C with C != B.
            // factor_dot((y.e1)e1^B + (y.e2)e2^B + (y.e3)e3^B + r) becomes...
            // y^B + factor_dot(r)

            return null;
        }
    }
}
