using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Sum : Operation
    {
        public Sum() : base()
        {
        }

        public Sum(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return true;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                case Format.PARSEABLE:
                    return " + ";
            }

            return "?";
        }

        public override int Grade
        {
            get
            {
                if (operandList.Count == 0)
                    return 0;

                int grade = operandList[0].Grade;
                if (operandList.All(operand => operand.Grade == grade))
                    return grade;

                return -1;
            }
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            for (int i = 0; i < operandList.Count; i++)
            {
                if (operandList[i].IsAdditiveIdentity)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operandA = operandList[i];

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Operand operandB = operandList[j];
                    Operand sum = null;

                    if(operandA is NumericScalar scalarA && operandB is NumericScalar scalarB)
                    {
                        sum = new NumericScalar(scalarA.value + scalarB.value);
                    }
                    else if(operandA is Collectable collectableA && operandB is Collectable collectableB)
                    {
                        if(collectableA.IsLike(collectableB))
                        {
                            collectableA.scalar = new Sum(new List<Operand>() { collectableA.scalar, collectableB.scalar });
                            collectableA.freezeFlags &= ~FreezeFlag.SUB_EVAL;
                            sum = collectableA;
                        }
                    }
                    else if(operandA is Matrix matrixA && operandB is Matrix matrixB)
                    {
                        if(matrixA.Rows == matrixB.Rows && matrixA.Cols == matrixB.Cols)
                            sum = Matrix.Add(matrixA, matrixB);
                    }

                    if(sum != null)
                    {
                        operandList.RemoveAt(j);    // Remove j before i so as not to invalidate i.
                        operandList.RemoveAt(i);
                        operandList.Add(sum);
                        return this;
                    }
                }
            }

            // TODO: If we're a polynomial, we should try to factor ourselves.

            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Operand operandA = operandList[i];
                Operand operandB = operandList[i + 1];

                bool swapOperands = false;

                if (operandA.Grade > operandB.Grade)
                    swapOperands = true;
                else if (operandA.Grade == operandB.Grade)
                {
                    string keyA = operandA.LexicographicSortKey();
                    string keyB = operandB.LexicographicSortKey();

                    if (string.Compare(keyA, keyB) > 0)
                        swapOperands = true;
                }

                if (swapOperands)
                {
                    operandList[i] = operandB;
                    operandList[i + 1] = operandA;
                    return this;
                }
            }

            // TODO: We might consider looking for (x.b1)b1 + (x.b2)b2 + ... + (x.bn)bn = x.
            //       We don't need all the basis vectors if we know that x.bk = 0 for some k.
            //       Since this is possibly time-consuming, we might only provide this kind
            //       of evaluation in a function, perhaps.  Harder to recognize would be something
            //       like (x.e1)(y.e1) + (x.e2)(y.e2) + (x.e3)(y.e3) = x.y, but maybe we first
            //       discover that it is x.((y.e1)e1 + (y.e2)e2 + (y.e3)e3) and then use the
            //       logic originaly talked about to finish.  Maybe call the function "factor_dot."
            //       factor_dot((x.e1)(y.e1) + (x.e2)(y.e2) + (x.e3)(y.e3)) becomes...
            //       x.factor_dot((y.e1)e1 + (y.e2)e2 + (y.e3)e3) becomes...
            //       x.y

            return null;
        }

        public override Operand Inverse(Context context)
        {
            if (!operandList.All(operand => operand is Blade || operand.Grade == 0))
                return null;

            // Our algorithm here should be correct for multivectors that are just scalars,
            // but it is overkill in that case, and doesn't really do anything useful.
            if (!operandList.Any(operand => operand.Grade > 0))
                return null;

            List<string> basisVectorList = context.ReturnBasisVectors();
            if (!(from operand in operandList where operand is Blade select operand as Blade).All(blade => blade.vectorList.All(vectorName => basisVectorList.Contains(vectorName))))
                return null;

            try
            {
                HashSet<string> subBasis = new HashSet<string>();
                foreach (Blade blade in (from operand in operandList where operand is Blade select operand as Blade))
                    foreach (string vectorName in blade.vectorList)
                        if (!subBasis.Contains(vectorName))
                            subBasis.Add(vectorName);

                Sum multivectorInverse = new Sum();

                int count = 0;
                foreach (Blade basisBlade in GenerateBasisBlades(subBasis.ToList()))
                {
                    Blade blade = basisBlade.Copy() as Blade;
                    string scalarName = string.Format($"__x{count++}__");
                    blade.scalar = new SymbolicScalarTerm(scalarName);
                    multivectorInverse.operandList.Add(blade);
                }

                GeometricProduct geometricProduct = new GeometricProduct();
                geometricProduct.operandList.Add(this.Copy());
                geometricProduct.operandList.Add(multivectorInverse.Copy());

                // I'm hoping the operand cache speeds up this calculation.
                Operand result = ExhaustEvaluation(geometricProduct, context);

                Sum resultSum = CanonicalizeMultivector(result);

                foreach (Blade blade in from operand in resultSum.operandList where operand is Blade select operand as Blade)
                    if(!(blade.scalar is Sum))
                        blade.scalar = new Sum(new List<Operand>() { blade.scalar });

                Matrix matrix = new Matrix(count, count);
                Regex rx = new Regex("__x([0-9]+)__");

                for (int i = 0; i < resultSum.operandList.Count; i++)
                {
                    Blade bladeA = resultSum.operandList[i] as Blade;
                    int row;
                    for(row = 0; row < multivectorInverse.operandList.Count; row++)
                    {
                        Blade bladeB = multivectorInverse.operandList[row] as Blade;
                        if(bladeB.IsLike(bladeA))
                            break;
                    }

                    if(row == multivectorInverse.operandList.Count)
                        throw new MathException("Failed to find row for matrix element.");

                    Sum sum = bladeA.scalar as Sum;
                    foreach (SymbolicScalarTerm term in from operand in sum.operandList where operand is SymbolicScalarTerm select operand as SymbolicScalarTerm)
                    {
                        int col = -1;
                        foreach(SymbolicScalarTerm.Symbol symbol in from factor in term.factorList where factor is SymbolicScalarTerm.Factor select factor as SymbolicScalarTerm.Factor)
                        {
                            MatchCollection collection = rx.Matches(symbol.name);
                            if(collection != null && collection.Count > 0)
                            {
                                Match match = collection[0];
                                col = Convert.ToInt32(match.Groups[1].Value);
                                term.factorList.Remove(symbol);
                                break;
                            }
                        }

                        if(col == -1)
                            throw new MathException("Failed to find column for matrix element.");

                        matrix.SetElement(row, col, term);
                    }
                }

                for(int i = 0; i < matrix.Rows; i++)
                    for(int j = 0; j < matrix.Cols; j++)
                        if(matrix.GetElement(i, j) == null)
                            matrix.SetElement(i, j, new NumericScalar(0.0));

                Matrix inverseMatrix = Operand.ExhaustEvaluation(new Inverse(new List<Operand>() { matrix }), context) as Matrix;

                multivectorInverse = new Sum();

                count = 0;
                foreach (Blade basisBlade in GenerateBasisBlades(subBasis.ToList()))
                {
                    Blade blade = basisBlade.Copy() as Blade;
                    blade.scalar = inverseMatrix.GetElement(count++, 0);
                    multivectorInverse.operandList.Add(blade);
                }

                return multivectorInverse;
            }
            catch(Exception exc)
            {
                throw new MathException(string.Format("Failed to calculate multivector inverse: {0}", exc.Message));
            }
        }
    }
}