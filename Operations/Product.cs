﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public abstract class Product : Operation
    {
        public Product() : base()
        {
        }

        public Product(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return operation is Sum;
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count == 0)
                return new NumericScalar(1.0);

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                if (operand.IsAdditiveIdentity)
                    return new NumericScalar(0.0);

                if (operand.IsMultiplicativeIdentity)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for(int i = 0; i < operandList.Count; i++)
            {
                Operand operandA = operandList[i];

                for(int j = i + 1; j < operandList.Count; j++)
                {
                    Operand operandB = operandList[j];
                    Operand product = null;

                    if(operandA is NumericScalar scalarA && operandB is NumericScalar scalarB)
                    {
                        product = new NumericScalar(scalarA.value * scalarB.value);
                    }
                    else if(operandA is SymbolicScalarTerm termA && operandB is SymbolicScalarTerm termB)
                    {
                        product = new SymbolicScalarTerm(new GeometricProduct(new List<Operand>() { termA.scalar, termB.scalar }));
                        (product as SymbolicScalarTerm).factorList = (from factor in termA.factorList.Concat(termB.factorList) select factor.Copy()).ToList();
                    }
                    else if(operandA is Matrix matrixA && operandB is Matrix matrixB)
                    {
                        product = Matrix.Multiply(matrixA, matrixB, GetType());
                    }
                    else if(operandA.Grade == 0 && operandB is Matrix matrixC)
                    {
                        product = matrixC.Scale(operandA);
                    }
                    else if(operandA is Matrix matrixD && operandB.Grade == 0)
                    {
                        product = matrixD.Scale(operandB);
                    }

                    if(product != null)
                    {
                        operandList.RemoveAt(j);    // Remove j before i so as not to invalidate i.
                        operandList.RemoveAt(i);
                        operandList.Add(product);
                        return this;
                    }
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Collectable collectable = operandList[i] as Collectable;
                if (collectable == null)
                    continue;

                for (int j = 0; j < operandList.Count; j++)
                {
                    if (i == j)
                        continue;

                    Operand scalar = operandList[j];
                    if (scalar.Grade != 0)
                        continue;

                    if (collectable.CanAbsorb(scalar))
                    {
                        operandList.RemoveAt(j);
                        collectable.scalar = new GeometricProduct(new List<Operand>() { scalar, collectable.scalar });
                        return this;
                    }
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operandA = operandList[i];
                if (operandA.Grade != 0)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Operand operandB = operandList[j];
                    if (operandB.Grade != 0)
                        continue;

                    string keyA = operandA.LexicographicSortKey();
                    string keyB = operandB.LexicographicSortKey();

                    if (string.Compare(keyA, keyB) > 0)
                    {
                        operandList[i] = operandB;
                        operandList[j] = operandA;
                        return this;
                    }
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operandA = operandList[i];
                if (operandA.Grade <= 0)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Operand operandB = operandList[j];
                    if (operandB.Grade != 0)
                        continue;

                    operandList.RemoveAt(j);
                    operandList.Insert(0, operandB);
                    return this;
                }
            }

            return base.EvaluationStep(context);
        }
    }
}