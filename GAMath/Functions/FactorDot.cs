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

            List<int> emptyBladeOffsetList = new List<int>();

            foreach (List<int> bladeOffsetList in this.IteratePotentiallyFactorableBladesLists(emptyBladeOffsetList, sum, basisVectorList, vectorName))
            {
                List<Operand> modifiedOperandList = new List<Operand>();

                for(int i = 0; i < bladeOffsetList.Count; i++)
                {
                    string basisVectorName = basisVectorList[i];
                    Blade blade = sum.operandList[bladeOffsetList[i]].Copy() as Blade;
                    SymbolicScalarTerm term = blade.scalar as SymbolicScalarTerm;
                    int j = blade.vectorList.IndexOf(basisVectorName);
                    if(j % 2 == 1)
                        term.scalar = new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), term.scalar });
                    blade.vectorList.Remove(basisVectorName);
                    term.factorList = term.factorList.Where(factor => !(factor is SymbolicScalarTerm.SymbolicDot symbolicDot && symbolicDot.InvolvesVectors(vectorName, basisVectorName))).ToList();
                    modifiedOperandList.Add(ExhaustEvaluation(blade, context));
                }

                bool canFactor = Enumerable.Range(0, modifiedOperandList.Count - 1).All(j => {
                    Operand bladeA = modifiedOperandList[j];
                    Operand bladeB = modifiedOperandList[j + 1];
                    Operand difference = new Sum(new List<Operand>() { bladeA.Copy(), new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), bladeB.Copy() }) });
                    difference = ExhaustEvaluation(difference, context);
                    return difference.IsAdditiveIdentity;
                });
                
                if(canFactor)
                {
                    Sum remainderSum = new Sum(sum.operandList.Where(operand => !bladeOffsetList.Contains(sum.operandList.IndexOf(operand))).ToList());
                    Sum resultSum = new Sum();
                    resultSum.operandList.Add(new OuterProduct(new List<Operand>() { new Blade(vectorName), modifiedOperandList[0] }));
                    resultSum.operandList.Add(new FactorDot(new List<Operand>() { remainderSum }));
                    return resultSum;
                }
            }

            return null;
        }

        private IEnumerable<List<int>> IteratePotentiallyFactorableBladesLists(List<int> bladeOffsetList, Sum sum, List<string> basisVectorList, string vectorName)
        {
            if(bladeOffsetList.Count == basisVectorList.Count)
                yield return bladeOffsetList;
            else
            {
                string basisVectorName = basisVectorList[bladeOffsetList.Count];

                for(int i = 0; i < sum.operandList.Count; i++)
                {
                    if(bladeOffsetList.Contains(i))
                        continue;

                    Blade blade = sum.operandList[i] as Blade;

                    if(blade.vectorList.Contains(basisVectorName))
                    {
                        if(blade.scalar is SymbolicScalarTerm term)
                        {
                            if(term.factorList.Any(factor => factor is SymbolicScalarTerm.SymbolicDot symbolicDot && symbolicDot.InvolvesVectors(vectorName, basisVectorName)))
                            {
                                bladeOffsetList.Add(i);
                            
                                foreach(List<int> otherBladeOffsetList in IteratePotentiallyFactorableBladesLists(bladeOffsetList, sum, basisVectorList, vectorName))
                                    yield return otherBladeOffsetList;

                                bladeOffsetList.Remove(i);
                            }
                        }
                    }
                }
            }
        }
    }
}
