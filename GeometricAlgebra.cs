using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public abstract class Operand
    {
        public Operand()
        {
        }

        public abstract Operand Copy();
        public abstract Operand New();
        
        public virtual Operand Evaluate()
        {
            return null;
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

        public override Operand Evaluate()
        {
            int count;
            
            do
            {
                count = 0;

                for (int i = 0; i < operandList.Count; i++)
                {
                    Operand oldOperand = operandList[i];
                    Operand newOperand = oldOperand.Evaluate();

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

        public override Operand Evaluate()
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.Evaluate();
            if (operand != null)
                return operand;

            // TODO: Combine like terms.  At this point, we should be a collection of sorted blades.

            return null;
        }
    }

    public abstract class Product : Operation
    {
        public Product() : base()
        {
        }

        public override Operand Evaluate()
        {
            if (operandList.Count == 0)
                return new Blade(1.0);

            return base.Evaluate();
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

        public override Operand Evaluate()
        {
            Operand operand = base.Evaluate();
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

        public override Operand Evaluate()
        {
            Operand operand = base.Evaluate();
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

        public override Operand Evaluate()
        {
            Operand operand = base.Evaluate();
            if (operand != null)
                return operand;

            // TODO: Take blades in the outer product.

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

        public override Operand Evaluate()
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
