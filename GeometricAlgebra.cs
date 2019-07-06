using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class EvaluationException : Exception
    {
        public EvaluationException(string error) : base(error)
        {
        }
    }

    public class EvaluationContext
    {
        public Dictionary<string, Operand> operandStorage;

        public EvaluationContext()
        {
        }

        // The operand returned here should have grade zero.
        public virtual Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            // TODO: Is there a better way to handle this?  Maybe just return an inner product here?
            //       For now, this should work pretty well except for how it formats.
            return new Variable(vectorNameA + "|" + vectorNameB);
        }
    }

    public class Euclidean3D_EvaluationContext : EvaluationContext
    {
        public Euclidean3D_EvaluationContext() : base()
        {
        }

        public override Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            if(vectorNameA == "e1")
            {
                if(vectorNameB == "e1")
                    return new Scalar(1.0);
                else if(vectorNameB == "e2")
                    return new Scalar(0.0);
                else if(vectorNameB == "e3")
                    return new Scalar(0.0);
            }
            else if(vectorNameA == "e2")
            {
                if (vectorNameB == "e1")
                    return new Scalar(0.0);
                else if (vectorNameB == "e2")
                    return new Scalar(1.0);
                else if (vectorNameB == "e3")
                    return new Scalar(0.0);
            }
            else if(vectorNameA == "e3")
            {
                if (vectorNameB == "e1")
                    return new Scalar(0.0);
                else if (vectorNameB == "e2")
                    return new Scalar(0.0);
                else if (vectorNameB == "e3")
                    return new Scalar(1.0);
            }

            return base.BilinearForm(vectorNameA, vectorNameB);
        }
    }

    public class Conformal3D_EvaluationContext : Euclidean3D_EvaluationContext
    {
        public Conformal3D_EvaluationContext() : base()
        {
        }

        public override Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            if(vectorNameA == "e1" || vectorNameA == "e2" || vectorNameA == "e3")
            {
                if(vectorNameB == "no" || vectorNameB == "ni")
                    return new Scalar(0.0);
            }

            if(vectorNameA == "no" || vectorNameA == "ni")
            {
                if(vectorNameB == "e1" || vectorNameB == "e2" || vectorNameB == "e3")
                    return new Scalar(0.0);

                if (vectorNameB == "no" || vectorNameB == "ni")
                {
                    if(vectorNameA == vectorNameB)
                        return new Scalar(0.0);
                    else
                        return new Scalar(-1.0);
                }
            }
            
            return base.BilinearForm(vectorNameA, vectorNameB);
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
        public virtual bool IsAdditiveIdentity { get { return false; } }
        public virtual bool IsMultiplicativeIdentity { get { return false; } }  // This is with respect to the geometric product.

        public enum Format
        {
            PARSEABLE,
            LATEX
        }

        public abstract string Print(Format format);
        
        public virtual Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            return null;
        }

        public static Operand FullyEvaluate(Operand operand, EvaluationContext context)
        {
            while (true)
            {
                bool bail = false;
                Operand newOperand = operand.Evaluate(context, ref bail);
                if (newOperand != null)
                    operand = newOperand;
                else if (bail)
                    continue;
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
        
        // This is used by infix operators.
        public virtual string PrintJoiner(Format format)
        {
            return "?";
        }

        public override string Print(Format format)
        {
            List<string> printList = new List<string>();

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                string subPrint = operand.Print(format);
                if(operand is Operation)
                {
                    if(format == Format.PARSEABLE)
                        subPrint = "(" + subPrint + ")";
                    else if(format == Format.LATEX)
                        subPrint = @"\left(" + subPrint + @"\right)";
                }

                printList.Add(subPrint);
            }

            return string.Join(PrintJoiner(format), printList);
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            if (operandList.Count == 1)
                return operandList[0];

            for(int i = 0; i < operandList.Count; i++)
            {
                Operation operation = operandList[i] as Operation;
                if(operation == null)
                    continue;

                // Apply the associative property.
                if(operation.GetType() == this.GetType() && operation.IsAssociative())
                {
                    operandList = operandList.Take(i).Concat(operation.operandList).Concat(operandList.Skip(i + 1).Take(operandList.Count - i - 1)).ToList();
                    return this;
                }

                // Apply the distributive property.
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

            int count;

            do
            {
                count = 0;

                for (int i = 0; i < operandList.Count; i++)
                {
                    Operand oldOperand = operandList[i];
                    Operand newOperand = oldOperand.Evaluate(context, ref bail);

                    if (newOperand != null)
                    {
                        operandList[i] = newOperand;
                        count++;
                    }

                    if (bail)
                        return null;
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
                if(operandList.Count == 0)
                    return 0;

                int grade = operandList[0].Grade;
                if(operandList.All(operand => operand.Grade == grade))
                    return grade;

                return -1;
            }
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.Evaluate(context, ref bail);
            if (operand != null)
                return operand;

            for (int i = 0; i < operandList.Count; i++)
            {
                if(operandList[i].IsAdditiveIdentity)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Scalar scalarA = operandList[i] as Scalar;
                if (scalarA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Scalar scalarB = operandList[i] as Scalar;
                    if (scalarB == null)
                        continue;

                    operandList.RemoveAt(i);
                    operandList.RemoveAt(j);
                    operandList.Add(new Scalar(scalarA.value + scalarB.value));
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

                    // Note that as blades evaluate, their anti-commutative property is applied,
                    // so this is all we need to do in order to identify like-terms in a sum.
                    if (Enumerable.SequenceEqual<string>(bladeA.vectorList, bladeB.vectorList))
                    {
                        operandList.RemoveAt(i);
                        operandList.RemoveAt(j);
                        Blade blade = new Blade(new Sum(new List<Operand>() { bladeA.scalar, bladeB.scalar }));
                        blade.vectorList = bladeA.vectorList;
                        operandList.Add(blade);
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
                    // TODO: Do some sort of lexographic compare here.
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

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            if (operandList.Count == 0)
                return new Scalar(1.0);

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                if(operand.IsAdditiveIdentity)
                    return new Scalar(0.0);

                if(operand.IsMultiplicativeIdentity)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Scalar scalarA = operandList[i] as Scalar;
                if(scalarA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    Scalar scalarB = operandList[i] as Scalar;
                    if(scalarB == null)
                        continue;

                    operandList.RemoveAt(i);
                    operandList.RemoveAt(j);
                    operandList.Add(new Scalar(scalarA.value * scalarB.value));
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Blade blade = operandList[i] as Blade;
                if (blade == null || blade.Grade == 0)
                    continue;

                for (int j = 0; j < operandList.Count; j++)
                {
                    Operand scalar = operandList[i];
                    if(scalar.Grade != 0)
                        continue;

                    operandList.RemoveAt(j);
                    blade.scalar = new GeometricProduct(new List<Operand>() { blade.scalar, scalar });
                    return this;
                }
            }

            return base.Evaluate(context, ref bail);
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

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return "";      // Juxtaposition.
                case Format.PARSEABLE:
                    return "*";
            }

            return "?";
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if (operand != null)
                return operand;

            // To avoid infinite evaluation looping, we must apply...
            //   1) vB = v.B + v^B, and
            //   2) v^B = vB - v.B,
            // ...according to rules that dictate when and where they're appropriate.
            // Also to avoid infinite looping, the distributive property must take
            // precedence over anything we do here.  This is accomplished using the bail flag.

            // All reduction cases must be eliminated before it is safe to handle the expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA != null && bladeB != null && bladeA.Grade > 1 && bladeB.Grade > 1)
                {
                    // Here our choice of which blade to reduce is arbitrary from a stand-point of correctness.
                    // However, we might converge faster by choosing the blade with smaller grade.
                    // Note there is also something arbitrary about how we're reducing the blades.
                    int j = bladeA.Grade <= bladeB.Grade ? i : i + 1;
                    Blade blade = operandList[j] as Blade;
                    Blade subBlade = blade.MakeSubBlade(0);
                    Blade vector = new Blade(blade.vectorList[0]);
                    GeometricProduct geometricProduct = new GeometricProduct(new List<Operand>() { vector, subBlade });
                    InnerProduct innerProduct = new InnerProduct(new List<Operand>() { vector.Copy(), subBlade.Copy() });
                    operandList[j] = new Sum(new List<Operand>() { geometricProduct, new GeometricProduct(new List<Operand>() { new Scalar(-1.0), innerProduct }) });

                    bail = true;
                    return this;
                }
            }

            // All reduction cases eliminated, it is now safe to handle some expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA == null || bladeB == null)
                    continue;

                if ((bladeA.Grade == 1 && bladeB.Grade > 1) || (bladeA.Grade > 1 && bladeB.Grade == 1))
                {
                    InnerProduct innerProduct = new InnerProduct(new List<Operand>() { bladeA, bladeB });
                    OuterProduct outerProduct = new OuterProduct(new List<Operand>() { bladeA.Copy(), bladeB.Copy() });
                    operandList[i] = new Sum(new List<Operand>() { innerProduct, outerProduct });
                    operandList.RemoveAt(i + 1);

                    bail = true;
                    return this;
                }
            }

            // It is now safe to handle the remaining expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA == null || bladeB == null)
                    continue;

                if(bladeA.Grade == 1 && bladeB.Grade == 1)
                {
                    operandList.RemoveAt(i + 1);
                    GeometricProduct innerProduct = new GeometricProduct(new List<Operand>(){ bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[0]) });
                    Blade outerProduct = new Blade(new GeometricProduct(new List<Operand>(){ bladeA.scalar.Copy(), bladeB.scalar.Copy() }));
                    outerProduct.vectorList.Add(bladeA.vectorList[0]);
                    outerProduct.vectorList.Add(bladeB.vectorList[0]);
                    operandList[i] = new Sum(new List<Operand>() { innerProduct, outerProduct });

                    bail = true;
                    return this;
                }
            }

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

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return @"\cdot";
                case Format.PARSEABLE:
                    return ".";
            }

            return "?";
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

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
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
                        return new GeometricProduct(new List<Operand>() { bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[0]) });
                    }
                    else if (bladeA.Grade == 1 && bladeB.Grade > 1)
                    {
                        return VectorDotBlade(bladeA, bladeB, context, 1.0);
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade == 1)
                    {
                        return VectorDotBlade(bladeB, bladeA, context, bladeA.Grade % 2 == 1 ? 1.0 : -1.0);
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

        private Sum VectorDotBlade(Blade vector, Blade blade, EvaluationContext context, double scale)
        {
            Sum sum = new Sum();

            for (int i = 0; i < blade.vectorList.Count; i++)
            {
                Blade subBlade = blade.MakeSubBlade(i);
                subBlade.scalar = new GeometricProduct(new List<Operand>() { new Scalar(i % 2 == 1 ? -scale : scale), vector.scalar, context.BilinearForm(vector.vectorList[0], blade.vectorList[i]) });
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

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return @"\wedge";
                case Format.PARSEABLE:
                    return "^";
            }

            return "?";
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

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if (operand != null)
                return operand;

            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA != null && bladeB != null)
                {
                    Blade blade = new Blade(new GeometricProduct(new List<Operand>() { bladeA.scalar, bladeB.scalar }));
                    blade.vectorList = bladeA.vectorList.Concat(bladeB.vectorList).ToList();
                    return blade;
                }
            }

            return null;
        }
    }

    public class Reverse : Operation
    {
        public Reverse() : base()
        {
        }

        public Reverse(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            // The reverse of a sum is the sum of the reverses.
            return operation is Sum;
        }

        public override Operand New()
        {
            return new Reverse();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if(operand != null)
                return null;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Reverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].Grade == 0)
                return operandList[0];

            Blade blade = operandList[0] as Blade;
            if(blade != null)
            {
                // TODO: Apply identity for taking the reverse of a blade here.
            }

            return null;
        }

        public override string Print(Format format)
        {
            if (operandList.Count != 1)
                return "?";

            switch (format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format) + @"\right)^{\tilde}";
                case Format.PARSEABLE:
                    return string.Format("reverse({0})", operandList[0].Print(format));
            }

            return "?";
        }
    }

    public class Inverse : Operation
    {
        public Inverse() : base()
        {
        }

        public Inverse(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override Operand New()
        {
            return new Inverse();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if(operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Inverse operation expects exactly one operand, got {0}.", operandList.Count));

            if(operandList[0].IsAdditiveIdentity)
                throw new EvaluationException("Cannot invert the additive identity.");

            Scalar scalar = operandList[0] as Scalar;
            if(scalar != null)
            {
                try
                {
                    return new Scalar(1.0 / scalar.value);
                }
                catch(DivideByZeroException)
                {
                    throw new EvaluationException(string.Format("Attempted to invert scalar ({0}), but got divid-by-zero exception.", scalar.value));
                }
            }

            // TODO: Look for easy cases we know how to invert, such as blades or maybe even rotors.
            //       Note that the general case is a fully evaluated multivector,
            //       and inverting it amounts to solving a system of linear equations,
            //       but coming up with this system isn't terribly easy.  Solving it symbolically would be very hard.
            return null;
        }

        public override string Print(Format format)
        {
            if(operandList.Count != 1)
                return "?";

            switch(format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format) + @"\right)^{-1}";
                case Format.PARSEABLE:
                    return string.Format("inverse({0})", operandList[0].Print(format));
            }

            return "?";
        }
    }

    public class GradePart : Operation
    {
        public GradePart() : base()
        {
        }

        public GradePart(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return operation is Sum;
        }

        public override Operand New()
        {
            return new GradePart();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if(operand != null)
                return operand;

            if (operandList.Count != 2)
                throw new EvaluationException(string.Format("Grade-part operation expects exactly two arguments, got {0}.", operandList.Count));

            int determinedGrade = operandList[0].Grade;
            if(determinedGrade == -1)
                return null;

            Scalar scalar = operandList[1] as Scalar;
            if(scalar == null)
                return null;

            int desiredGrade = (int)scalar.value;

            return determinedGrade == desiredGrade ? operand : new Scalar(0.0);
        }

        public override string Print(Format format)
        {
            if(operandList.Count != 2)
                return "?";

            switch(format)
            {
                case Format.LATEX:
                {
                    Scalar scalar = operandList[1] as Scalar;
                    if (scalar == null)
                        return "?";

                    int desiredGrade = (int)scalar.value;
                    if (desiredGrade == 0)
                        return @"\left\langle" + operandList[0].Print(format) + @"\right\rangle";

                    return @"\left\langle" + operandList[0].Print(format) + @"\right\rangle_{" + desiredGrade.ToString() + "}";
                }
                case Format.PARSEABLE:
                {
                    return string.Format("grade({0},{1})", operandList[0].ToString(), operandList[1].ToString());
                }
            }

            return "?";
        }
    }

    public class Assignment : Operation
    {
        public Assignment() : base()
        {
        }

        public Assignment(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override Operand New()
        {
            return new Assignment();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            Operand operand = base.Evaluate(context, ref bail);
            if(operand != null)
                return operand;

            if (operandList.Count != 2)
                throw new EvaluationException(string.Format("Assignment operation expects exactly two operands, got {0}.", operandList.Count));

            Variable variable = operandList[0] as Variable;
            if(variable == null)
                throw new EvaluationException("Assignment operation expects an l-value of type variable.");

            context.operandStorage.Add(variable.name, operandList[1]);
            return operandList[1];
        }

        public override string Print(Format format)
        {
            if(operandList.Count == 2)
                return string.Format("{0} = {1}", operandList[0].Print(format), operandList[1].Print(format));

            return "?";
        }
    }

    public class Blade : Operand
    {
        public Operand scalar;
        public List<string> vectorList;

        public override int Grade
        {
            get
            {
                return vectorList.Count;
            }
        }

        public override bool IsAdditiveIdentity
        {
            get
            {
                return this.scalar.IsAdditiveIdentity;
            }
        }

        public override bool IsMultiplicativeIdentity
        {
            get
            {
                return this.scalar.IsMultiplicativeIdentity && vectorList.Count == 0;
            }
        }

        public Blade() : base()
        {
            vectorList = new List<string>();
            this.scalar = new Scalar(1.0);
        }

        public Blade(double scalar) : base()
        {
            vectorList = new List<string>();
            this.scalar = new Scalar(scalar);
        }

        public Blade(string vectorName) : base()
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = new Scalar(1.0);
        }

        public Blade(double scalar, string vectorName) : base()
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = new Scalar(scalar);
        }

        public Blade(Operand scalar) : base()
        {
            vectorList = new List<string>();
            this.scalar = scalar;
        }

        public Blade(Operand scalar, string vectorName) : base()
        {
            vectorList = new List<string>() { vectorName };
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

        public override string Print(Format format)
        {
            if (Grade == 0)
                return scalar.Print(format);

            string printedBlade = "?";

            if(format == Format.LATEX)
                printedBlade = string.Join(@"\wedge", from vectorName in vectorList select @"\vec{" + vectorName + "}");
            else if(format == Format.PARSEABLE)
                printedBlade = string.Join("^", vectorList);

            if(!scalar.IsMultiplicativeIdentity)
            {
                if(format == Format.LATEX)
                    printedBlade = @"\left(" + scalar.Print(format) + @"\right)" + printedBlade;
                else if(format == Format.PARSEABLE)
                    printedBlade = "(" + scalar.Print(format) + ")*" + printedBlade;
            }

            return printedBlade;
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            if (Grade == 0)
                return scalar;

            // This is the only way we detect a linearly dependent set of vectors.
            for (int i = 0; i < vectorList.Count; i++)
                for (int j = i + 1; j < vectorList.Count; j++)
                    if (vectorList[i] == vectorList[j])
                        return new Scalar(0.0);

            Operand newScalar = scalar.Evaluate(context, ref bail);
            if (newScalar != null)
            {
                scalar = newScalar;
                return this;
            }

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
                    scalar = new GeometricProduct(new List<Operand>() { new Scalar(-1.0), scalar });

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

    public class Scalar : Operand
    {
        public double value;

        public override int Grade
        {
            get
            {
                return 0;
            }
        }

        public override bool IsAdditiveIdentity
        {
            get
            {
                return this.value == 0.0;
            }
        }

        public override bool IsMultiplicativeIdentity
        {
            get
            {
                return this.value == 1.0;
            }
        }

        public Scalar(double value = 0.0) : base()
        {
            this.value = value;
        }

        public override Operand Copy()
        {
            return new Scalar(this.value);
        }

        public override Operand New()
        {
            return new Scalar();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            return null;
        }

        public override string Print(Format format)
        {
            switch(format)
            {
                case Format.LATEX:
                {
                    return string.Format("{0:F2}", this.value);
                }
                case Format.PARSEABLE:
                {
                    return string.Format("{0}", this.value);
                }
            }

            return "?";
        }
    }

    public class Variable : Operand
    {
        public string name;

        public override int Grade
        {
            get
            {
                // This is kind-of a hack, but I'm okay with it for now.
                if(name[0] == '_')
                    return 0;

                return -1;
            }
        }

        public Variable(string name = "") : base()
        {
            this.name = name;
        }

        public override Operand Copy()
        {
            return new Variable(this.name);
        }

        public override Operand New()
        {
            return new Variable();
        }

        public override Operand Evaluate(EvaluationContext context, ref bool bail)
        {
            if(this.name[0] == '_')
                return null;

            Operand operand = context.operandStorage[this.name];
            if(operand != null && operand.Grade == 0)
                return operand;

            return null;
        }

        public override string Print(Format format)
        {
            switch(format)
            {
                case Format.LATEX:
                {
                    return this.name;
                }
                case Format.PARSEABLE:
                {
                    return "$" + this.name;
                }
            }

            return "?";
        }
    }
}