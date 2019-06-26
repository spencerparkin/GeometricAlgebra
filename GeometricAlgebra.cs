using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public abstract class Signature
    {
        public Signature()
        {
        }

        // TODO: At some point I would like to be able to generalize from floating-point
        //       scalars to symbolic scalars.  This would allow us to support symbolic vectors.
        //       For now, all pairs of vectors taken in an inner product must give a numeric result.
        public abstract double Evaluate(string vectorNameA, string vectorNameB);
    }

    public class Euclidean3D_Signature : Signature
    {
        public Euclidean3D_Signature() : base()
        {
        }

        public override double Evaluate(string vectorNameA, string vectorNameB)
        {
            return 0.0;
        }
    }

    public class Conformal3D_Signature : Signature
    {
        public Conformal3D_Signature() : base()
        {
        }

        public override double Evaluate(string vectorNameA, string vectorNameB)
        {
            return 0.0;
        }
    }

    public abstract class Operand
    {
        public Operand()
        {
        }

        public abstract Operand Copy();
        public abstract Operand New();
        
        public virtual Operand Evaluate(Signature signature)
        {
            return null;
        }

        public static Operand FullyEvaluate(Operand operand, Signature signature)
        {
            while (true)
            {
                Operand newOperand = operand.Evaluate(signature);
                if (newOperand != null)
                    operand = newOperand;
                else
                    break;
            }

            return operand;
        }
    }

    public abstract class Operation : Operand
    {
        public List<Operand> operandList;

        public Operation() : base()
        {
            operandList = new List<Operand>();
        }

        public override Operand Copy()
        {
            Operation clone = (Operation)New();
            clone.operandList = (from operand in this.operandList select operand.Copy()).ToList();
            return clone;
        }

        public override Operand Evaluate(Signature signature)
        {
            int count;
            
            do
            {
                count = 0;

                for (int i = 0; i < operandList.Count; i++)
                {
                    Operand oldOperand = operandList[i];
                    Operand newOperand = oldOperand.Evaluate(signature);

                    if (newOperand != null)
                    {
                        operandList[i] = newOperand;
                        count++;
                    }
                }
            }
            while (count > 0);

            if (operandList.Count == 1)
                return operandList[0];

            return null;
        }
    }

    public class Sum : Operation
    {
        public Sum() : base()
        {
        }

        public override Operand New()
        {
 	        return new Sum();
        }

        public override Operand Evaluate(Signature signature)
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade blade = operandList[i] as Blade;
                if (blade != null && blade.scalar == 0.0)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                if (bladeA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; i++)
                {
                    Blade bladeB = operandList[j] as Blade;
                    if (bladeB == null)
                        continue;

                    if (Enumerable.SequenceEqual<string>(bladeA.vectorList, bladeB.vectorList))
                    {
                        operandList.RemoveAt(i);
                        operandList.RemoveAt(j);
                        Blade blade = new Blade(bladeA.scalar + bladeB.scalar);
                        blade.vectorList = bladeA.vectorList;
                        operandList.Add(blade);
                        return this;
                    }
                }
            }

            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA != null && bladeB != null)
                {
                    bool swapBlades = false;

                    if (bladeA.vectorList.Count > bladeB.vectorList.Count)
                        swapBlades = true;
                    else if (bladeA.vectorList.Count == bladeB.vectorList.Count)
                    {
                        if (string.Compare(string.Join("", bladeA.vectorList), string.Join("", bladeB.vectorList)) > 0)
                            swapBlades = true;
                    }

                    if (swapBlades)
                    {
                        operandList[i] = bladeB;
                        operandList[i + 1] = bladeA;
                        return this;
                    }
                }
            }

            return null;
        }
    }

    public abstract class Product : Operation
    {
        public Product() : base()
        {
        }

        public override Operand Evaluate(Signature signature)
        {
            if (operandList.Count == 0)
                return new Blade(1.0);

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade blade = operandList[i] as Blade;
                if (blade != null && blade.scalar == 1.0 && blade.vectorList.Count == 0)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            return base.Evaluate(signature);
        }
    }

    public class GeometricProduct : Product
    {
        public GeometricProduct() : base()
        {
        }

        public override Operand New()
        {
 	        return new GeometricProduct();
        }

        public override Operand Evaluate(Signature signature)
        {
            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            // TODO: Take blades in the geometric product.

            return null;
        }
    }

    public class InnerProduct : Product
    {
        public InnerProduct() : base()
        {
        }

        public override Operand New()
        {
 	        return new InnerProduct();
        }

        public override Operand Evaluate(Signature signature)
        {
            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            // TODO: Take blades in the inner product.

            return null;
        }
    }

    public class OuterProduct : Product
    {
        public OuterProduct() : base()
        {
        }

        public override Operand New()
        {
            return new OuterProduct();
        }

        public override Operand Evaluate(Signature signature)
        {
            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA != null && bladeB != null)
                {
                    Blade blade = new Blade(bladeA.scalar * bladeB.scalar);
                    blade.vectorList = bladeA.vectorList.Concat(bladeB.vectorList).ToList();
                    return blade;
                }
            }

            return null;
        }
    }

    public class Blade : Operand
    {
        public double scalar;
        public List<string> vectorList;

        public Blade()
        {
            vectorList = new List<string>();
            this.scalar = 1.0;
        }

        public Blade(double scalar)
        {
            vectorList = new List<string>();
            this.scalar = scalar;
        }

        public Blade(string vectorName)
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = 1.0;
        }

        public Blade(double scalar, string vectorName)
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = scalar;
        }

        public override Operand New()
        {
            return new Blade();
        }

        public override Operand Copy()
        {
            Blade clone = new Blade();
            clone.scalar = this.scalar;
            clone.vectorList = (from vectorName in this.vectorList select vectorName).ToList();
            return clone;
        }

        public override Operand Evaluate(Signature signature)
        {
            for (int i = 0; i < vectorList.Count; i++)
                for (int j = i + 1; j < vectorList.Count; j++)
                    if (vectorList[i] == vectorList[j])
                        return new Blade(0.0);

            int adjacentSwapCount = 0;
            bool keepGoing = true;
            while (keepGoing)
            {
                keepGoing = false;
                for (int i = 0; i < vectorList.Count - 1; i++)
                {
                    string vectorNameA = vectorList[i];
                    string vectorNameB = vectorList[i + 1];

                    if (string.Compare(vectorNameA, vectorNameB) > 0)
                    {
                        vectorList[i] = vectorNameB;
                        vectorList[i + 1] = vectorNameA;

                        adjacentSwapCount++;
                        keepGoing = true;
                    }
                }
            }

            if (adjacentSwapCount > 0)
            {
                if (adjacentSwapCount % 2 == 1)
                    scalar = -scalar;

                return this;
            }

            return null;
        }

        public Blade MakeSubBlade(int i)
        {
            Blade subBlade = new Blade();
            subBlade.scalar = this.scalar;
            string removedVectorName = this.vectorList[i];
            subBlade.vectorList = (from vectorName in this.vectorList where vectorName != removedVectorName select vectorName).ToList();
            return subBlade;
        }
    }
}
