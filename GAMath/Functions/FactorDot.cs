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

            operand = operandList[0];
            Sum sum = null;

            List<string> basisVectorList = context.ReturnBasisVectors();

            int grade = operand.Grade;
            if(grade == -1)
            {
                sum = new Sum();
                for(int i = 0; i <= basisVectorList.Count; i++)
                    sum.operandList.Add(new FactorDot(new List<Operand>() { new GradePart(new List<Operand>() { operand.Copy(), new NumericScalar(i) }) }));
                return sum;    
            }

            if(grade >= 2)
                return operand;

            // factor_dot(s1(x.e1)(y.e1) + s2(x.e2)(y.e2) + s3(x.e3)(y.e3) + r) becomes...
            // x.factor_dot(s1(y.e1)e1 + s2(y.e2)e2 + s3(y.e3)e3) + factor_dot(r) becomes...
            // x.(s*y) + factor_dot(r) if s = s1 = s2 = s3.

            sum = Operand.CanonicalizeMultivector(operand.Copy());

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
                if(grade == 0)
                {
                    // Remember that we need to address each basis vector, whether it
                    // can be pulled out, or whether it is zero in the inner product
                    // with our symbolic vector.
                }
                else if(grade == 1)
                {
                }
            }

            return operand;
        }
    }
}
