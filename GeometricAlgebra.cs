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

        public virtual int Grade { get { return -1; } }
        
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

    // TODO: Future derivatives of this class might be an invert, reverse or grade-part.
    public abstract class Operation : Operand
    {
        public List<Operand> operandList;

        public Operation() : base()
        {
            operandList = new List<Operand>();
        }

        public Operation(List<Operand> operandList)
        {
            this.operandList = operandList;
        }

        public override Operand Copy()
        {
            Operation clone = (Operation)New();
            clone.operandList = (from operand in this.operandList select operand.Copy()).ToList();
            return clone;
        }

        public abstract bool IsAssociative();
        public abstract bool IsDistributiveOver(Operation operation);

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

            for(int i = 0; i < operandList.Count; i++)
            {
                Operation operation = operandList[i] as Operation;
                if(operation == null)
                    continue;

                if(operation.GetType() == this.GetType() && operation.IsAssociative())
                {
                    operandList = operandList.Take(i).Concat(operation.operandList).Concat(operandList.Skip(i + 1).Take(operandList.Count - i - 1)).ToList();
                    return this;
                }

                if(this.IsDistributiveOver(operation))
                {
                    Operation newOperationA = (Operation)Activator.CreateInstance(operation.GetType());

                    for (int j = 0; j < operation.operandList.Count; j++)
                    {
                        Operation newOperationB = (Operation)Activator.CreateInstance(this.GetType());

                        newOperationB.operandList = (from operand in operandList.Take(i) select operand.Copy()).ToList();
                        newOperationB.operandList.Add(operation.operandList[j]);
                        newOperationB.operandList = newOperationB.operandList.Concat(from operand in operandList.Skip(i + 1).Take(operandList.Count - i - 1) select operand.Copy()).ToList();

                        newOperationA.operandList.Add(newOperationB);
                    }

                    return newOperationA;
                }
            }

            return null;
        }
    }

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

        public override int Grade
        {
            get
            {
                if(operandList.Count == 0)
                    return 0;

                int grade = operandList[0].Grade;
                if(operandList.All(operand => operand.Grade == grade))
                    return grade;

                return -1;
            }
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

        public Product(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return operation is Sum;
        }

        public override Operand Evaluate(Signature signature)
        {
            if (operandList.Count == 0)
                return new Blade(1.0);

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade blade = operandList[i] as Blade;
                if (blade != null && blade.scalar == 1.0 && blade.Grade == 0)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                if (bladeA == null || bladeA.Grade != 0)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Blade bladeB = operandList[j] as Blade;
                    if (bladeB == null || bladeB.Grade != 0)
                        continue;

                    operandList.RemoveAt(i);
                    operandList.RemoveAt(j);
                    operandList.Add(new Blade(bladeA.scalar * bladeB.scalar));
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                if (bladeA != null)
                    continue;

                for (int j = 0; j < operandList.Count; j++)
                {
                    if (i == j)
                        continue;

                    Blade bladeB = operandList[j] as Blade;
                    if (bladeB != null)
                        continue;

                    if (bladeA.Grade > 0 && bladeB.Grade == 0)
                    {
                        bladeA.scalar *= bladeB.scalar;
                        operandList.RemoveAt(j);
                        return this;
                    }
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

        public GeometricProduct(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override Operand New()
        {
 	        return new GeometricProduct();
        }

        public override bool IsAssociative()
        {
            return true;
        }

        public override Operand Evaluate(Signature signature)
        {
            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            // TODO: Take blades in the geometric product.
            
            // To avoid infinite evaluation looping, we must apply...
            //   1) vB = v.B + v^B, and
            //   2) v^B = vB - v.B,
            // ...according to rules that dictate when and where they're appropriate.
            // The second of these is used to break up any A*B product where grade(A) > 1
            // and grade(B) > 1.  The first of these should be applied in cases where
            // grade(A) = 1 and grade(B) = 1 unless there exists a case where grade(A) = 1
            // and grade(B) > 1.

            return null;
        }
    }

    public class InnerProduct : Product
    {
        public InnerProduct() : base()
        {
        }

        public InnerProduct(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override Operand New()
        {
 	        return new InnerProduct();
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override int Grade
        {
            get
            {
                if(operandList.Count == 2)
                    if(operandList[0].Grade >= 0 && operandList[1].Grade >= 0)
                        return Math.Abs(operandList[0].Grade - operandList[1].Grade);

                return -1;
            }
        }

        public override Operand Evaluate(Signature signature)
        {
            Operand operand = base.Evaluate(signature);
            if (operand != null)
                return operand;

            if (operandList.Count == 2)
            {
                Blade bladeA = operandList[0] as Blade;
                Blade bladeB = operandList[1] as Blade;

                if (bladeA != null && bladeB != null)
                {
                    if (bladeA.Grade == 1 && bladeB.Grade == 1)
                    {
                        return new Blade(bladeA.scalar * bladeB.scalar * signature.Evaluate(bladeA.vectorList[0], bladeB.vectorList[0]));
                    }
                    else if (bladeA.Grade == 1 && bladeB.Grade > 1)
                    {
                        return VectorDotBlade(bladeA, bladeB, signature, 1.0);
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade == 1)
                    {
                        return VectorDotBlade(bladeB, bladeA, signature, bladeA.Grade % 2 == 1 ? 1.0 : -1.0);
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade > 1)
                    {
                        if (bladeA.Grade <= bladeB.Grade)
                        {
                            return new InnerProduct(new List<Operand>() { bladeA.MakeSubBlade(bladeA.Grade - 1), new InnerProduct(new List<Operand>() { new Blade(bladeA.vectorList[bladeA.Grade - 1]), bladeB }) });
                        }
                        else
                        {
                            return new InnerProduct(new List<Operand>() { new InnerProduct(new List<Operand>() { bladeA, new Blade(bladeB.vectorList[0]) }), bladeB.MakeSubBlade(0) });
                        }
                    }
                }
            }

            return null;
        }

        private Sum VectorDotBlade(Blade vector, Blade blade, Signature signature, double scale)
        {
            Sum sum = new Sum();

            for (int i = 0; i < blade.vectorList.Count; i++)
            {
                Blade subBlade = blade.MakeSubBlade(i);
                subBlade.scalar *= scale * vector.scalar * signature.Evaluate(vector.vectorList[0], blade.vectorList[i]);
                if (i % 2 == 1)
                    subBlade.scalar = -subBlade.scalar;

                sum.operandList.Add(subBlade);
            }

            return sum;
        }
    }

    public class OuterProduct : Product
    {
        public OuterProduct() : base()
        {
        }

        public OuterProduct(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override Operand New()
        {
            return new OuterProduct();
        }

        public override bool IsAssociative()
        {
            return true;
        }

        public override int Grade
        {
            get
            {
                if(operandList.All(operand => operand.Grade >= 0))
                    return operandList.Sum(operand => operand.Grade);

                return -1;
            }
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

        public override int Grade
        {
            get
            {
                return vectorList.Count;
            }
        }

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