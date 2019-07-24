using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

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

        public override Operand New()
        {
            return new Sum();
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
                NumericScalar scalarA = operandList[i] as NumericScalar;
                if (scalarA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    NumericScalar scalarB = operandList[j] as NumericScalar;
                    if (scalarB == null)
                        continue;

                    operandList.RemoveAt(j);
                    operandList.RemoveAt(i);
                    operandList.Add(new NumericScalar(scalarA.value + scalarB.value));
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Collectable collectableA = operandList[i] as Collectable;
                if (collectableA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Collectable collectableB = operandList[j] as Collectable;
                    if (collectableB == null)
                        continue;

                    if (collectableA.Like(collectableB))
                    {
                        operandList.RemoveAt(j);
                        operandList.RemoveAt(i);
                        operandList.Add(collectableA.Collect(collectableB));
                        return this;
                    }
                }
            }

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

            return null;
        }

        private static IEnumerable<Blade> GenerateBasisBladesOfGrade(List<string> basisVectorList, Blade blade, int depth, int maxDepth, int j)
        {
            if (depth < maxDepth)
            {
                for (int i = j; i < basisVectorList.Count; i++)
                {
                    blade.vectorList.Add(basisVectorList[i]);

                    foreach (Blade basisBlade in GenerateBasisBladesOfGrade(basisVectorList, blade, depth + 1, maxDepth, i + 1))
                        yield return basisBlade;

                    blade.vectorList.RemoveAt(blade.vectorList.Count - 1);
                }
            }
            else
            {
                Blade basisBlade = blade.Copy() as Blade;
                basisBlade.vectorList.Sort();
                yield return basisBlade;
            }
        }

        private static IEnumerable<Blade> GenerateBasisBlades(List<string> basisVectorList)
        {
            Blade blade = new Blade();
            for (int i = 0; i <= basisVectorList.Count; i++)
            {
                foreach (Blade basisBlade in GenerateBasisBladesOfGrade(basisVectorList, blade, 0, i, 0))
                    yield return basisBlade;
            }
        }

        private static void PopulateMatrixArray(double[,] matrixArray, int row, Sum sum)
        {
            Regex rx = new Regex("x([0-9]+)");

            foreach(SymbolicScalarTerm term in from operand in sum.operandList where operand is SymbolicScalarTerm select operand as SymbolicScalarTerm)
            {
                var symbol = term.factorList[0] as SymbolicScalarTerm.Symbol;
                MatchCollection collection = rx.Matches(symbol.name);
                Match match = collection[0];
                int col = Convert.ToInt32(match.Groups[1].Value);
                matrixArray[row, col] = (term.scalar as NumericScalar).value;
            }
        }

        public override Operand Inverse(Context context)
        {
            // This is a super hard problem, but maybe we can handle the following case.

            if (!operandList.All(operand => operand is Blade || operand is NumericScalar))
                return null;

            if (!(from operand in operandList where operand is Blade select operand as Blade).All(blade => blade.scalar is NumericScalar))
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
                    string scalarName = string.Format("x{0}", count++);
                    blade.scalar = new SymbolicScalarTerm(scalarName);
                    multivectorInverse.operandList.Add(blade);
                }

                GeometricProduct geometricProduct = new GeometricProduct();
                geometricProduct.operandList.Add(this.Copy());
                geometricProduct.operandList.Add(multivectorInverse.Copy());

                Operand result = ExhaustEvaluation(geometricProduct, context);

                double[,] matrixArray = new double[count, count];
                for(int i = 0; i < count; i++)
                    for(int j = 0; j < count; j++)
                        matrixArray[i,j] = 0.0;

                PopulateMatrixArray(matrixArray, 0, result as Sum);

                foreach(Blade bladeA in from operand in (result as Operation).operandList where operand is Blade select operand as Blade)
                {
                    for(int i = 0; i < multivectorInverse.operandList.Count; i++)
                    {
                        Blade bladeB = multivectorInverse.operandList[i] as Blade;
                        if(bladeB != null && bladeB.Like(bladeA))
                        {
                            if(!(bladeA.scalar is Sum))
                                bladeA.scalar = new Sum(new List<Operand>() { bladeA.scalar });

                            PopulateMatrixArray(matrixArray, i, bladeA.scalar as Sum);
                        }
                    }
                }

                Matrix<double> matrix = DenseMatrix.OfArray(matrixArray);

                double epsilon = 1e-12;
                double det = matrix.Determinant();
                if (Math.Abs(det) < epsilon)
                    throw new MathException("Cannot invert singular matrix.");

                double[] vectorArray = new double[count];
                for (int i = 0; i < count; i++)
                    vectorArray[i] = i > 0 ? 0.0 : 1.0;

                Vector<double> vectorA = DenseVector.OfArray(vectorArray);
                Vector<double> vectorB = Vector<double>.Build.Dense(count);

                matrix.Solve(vectorA, vectorB);

                multivectorInverse = new Sum();

                count = 0;
                foreach (Blade basisBlade in GenerateBasisBlades(subBasis.ToList()))
                {
                    double value = vectorB[count++];
                    if(Math.Abs(value) >= epsilon)
                    {
                        double roundedValue = Math.Round(value);
                        if(Math.Abs(value - roundedValue) < epsilon)
                            value = roundedValue;

                        Blade blade = basisBlade.Copy() as Blade;
                        blade.scalar = new NumericScalar(value);
                        multivectorInverse.operandList.Add(blade);
                    }
                }

                return multivectorInverse;
            }
            catch(Exception exc)
            {
                throw new MathException(string.Format("Failed to calculate inverse: {0}", exc.Message));
            }
        }
    }
}