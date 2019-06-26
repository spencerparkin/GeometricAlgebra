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
            base.Evaluate();

            // TODO: Combine like terms.

            return null;
        }
    }

    public abstract class Product : Operation
    {
        public Product() : base()
        {
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
            base.Evaluate();

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
            base.Evaluate();

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
            base.Evaluate();

            // TODO: Take blades in the outer product.

            return null;
        }
    }

    public class Blade<T> : Operand
    {
        public T scalar;
        public List<string> vectorList;

        public Blade()
        {
            vectorList = new List<string>();
            dynamic thisScalar = this.scalar;
            thisScalar = 1.0;
        }

        public Blade(double scalar)
        {
            vectorList = new List<string>();
            dynamic thisScalar = this.scalar;
            thisScalar = scalar;
        }

        public Blade(string vectorName)
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            dynamic thisScalar = this.scalar;
            thisScalar = 1.0;
        }

        public Blade(double scalar, string vectorName)
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            dynamic thisScalar = this.scalar;
            thisScalar = scalar;
        }

        public override Operand New()
        {
            return new Blade<T>();
        }

        public override Operand Copy()
        {
            Blade<T> clone = new Blade<T>();
            clone.scalar = this.scalar;
            clone.vectorList = (from vectorName in this.vectorList select vectorName).ToList();
            return clone;
        }

        public override Operand Evaluate()
        {
            // TODO: First see if the blade goes to zero.  Second, sort the vectors by name, accounting for sign change, if any.
            return null;
        }
    }
}
