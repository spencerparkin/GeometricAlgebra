using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                yield return blade;
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

        public override Operand Inverse(Context context)
        {
            // This is a super hard problem, but maybe we can handle the following case.

            if (!operandList.All(operand => operand is Blade))
                return null;

            if (!operandList.All(operand => (operand as Blade).scalar is NumericScalar))
                return null;

            List<string> basisVectorList = context.ReturnBasisVectors();
            if (!operandList.All(operand => (operand as Blade).vectorList.All(vectorName => basisVectorList.Contains(vectorName))))
                return null;

            HashSet<string> subBasis = new HashSet<string>();
            foreach (Operand operand in operandList)
                foreach (string vectorName in (operand as Blade).vectorList)
                    if (!subBasis.Contains(vectorName))
                        subBasis.Add(vectorName);

            Sum multivectorA = this.Copy() as Sum;
            Sum multivectorB = new Sum();

            int i = 0;
            foreach (Blade basisBlade in GenerateBasisBlades(subBasis.ToList()))
            {
                Blade blade = basisBlade.Copy() as Blade;
                string scalarName = string.Format("__x{0}__", i++);
                blade.scalar = new SymbolicScalarTerm(scalarName);
                multivectorB.operandList.Add(blade);
            }

            GeometricProduct geometricProduct = new GeometricProduct();
            geometricProduct.operandList.Add(multivectorA);
            geometricProduct.operandList.Add(multivectorB);

            Operand result = ExhaustEvaluation(geometricProduct, context);

            double[,] matrixArray = new double[i, i];

            //...

            Matrix<double> matrix = DenseMatrix.OfArray(matrixArray);

            double[] vectorArray = new double[i];
            for (int j = 0; j < i; j++)
                vectorArray[j] = j > 0 ? 0.0 : 1.0;

            Vector<double> vectorA = DenseVector.OfArray(vectorArray);
            Vector<double> vectorB = Vector<double>.Build.Dense(i);

            double det = matrix.Determinant();
            if (det == 0.0)      // Just being close to zero can be a problem.
            {
            }

            matrix.Solve(vectorA, vectorB);   // Will this throw an exception if singular?

            //...

            return null;
        }
    }
}