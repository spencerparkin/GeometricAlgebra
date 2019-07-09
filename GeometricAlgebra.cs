using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public List<Function> funcList;

        public EvaluationContext()
        {
            funcList = new List<Function>();
            operandStorage = new Dictionary<string, Operand>();
        }

        // The operand returned here should have grade zero.
        public virtual Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            return new SymbolicScalarTerm(vectorNameA, vectorNameB);
        }

        public virtual Operation CreateFunction(string name)
        {
            IEnumerable<Function> enumerable = from func in funcList where func.Name(Operand.Format.PARSEABLE) == name select func;
            if (enumerable.Count() == 0)
                return null;

            return enumerable.ToArray()[0].Copy() as Operation;
        }
    }

    // TODO: We might add a virtual method taking a map that can be used to verify there are no cycles in the operand tree.
    // Note that eliminating redundancy in code or representation (data-structures) is often a programmer's goal,
    // and it certainly is here, because it reduces the number of cases that need to be considered and maintained.
    // There is, however, some redundancy going on here in our data-structure.  For example, there is more than one
    // way to represent an outer product of vectors (a blade).  This really shouldn't bother us too badly, though,
    // because redundancy is inherant in what we're trying to do here anyway.  Specifically, if one operand tree evaluates as
    // another, and both trees are different in any way, then you could argue that one is a redundant representation of
    // the other, or vice versa, but then to eliminate that redundancy defeats the purpose of a symbolic calculator.
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
        
        // Derivatives overriding this virtual method are to return null
        // if no algebraic manipulation of the sub-tree rooted at this object
        // is performed.  On the other hand, if such a manipulation is performed,
        // then the new or existing root should be returned.
        public virtual Operand Evaluate(EvaluationContext context)
        {
            return null;
        }

        public static Operand FullyEvaluate(Operand operand, EvaluationContext context, bool debug = false)
        {
            while (true)
            {
                Operand newOperand = operand.Evaluate(context);
                if (newOperand != null)
                    operand = newOperand;
                else
                    break;

                if(debug)
                {
                    string expression = operand.Print(Format.PARSEABLE);
                    Console.WriteLine(expression + "\n");
                }
            }

            return operand;
        }

        public static Operand FullyEvaluate(string expression, EvaluationContext context, bool debug = false)
        {
            Parser parser = new Parser(context);
            Operand operand = parser.Parse(expression);
            return FullyEvaluate(operand, context, debug);
        }

        public virtual string LexicographicSortKey()
        {
            return "";
        }
        
        // This is with respect to the geometric product.
        public virtual Operand Inverse()
        {
            return null;
        }

        public virtual Operand Reverse()
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

        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count == 1 && (this is Sum || this is Product))
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

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand oldOperand = operandList[i];
                Operand newOperand = oldOperand.Evaluate(context);

                if (newOperand != null)
                {
                    operandList[i] = newOperand;
                    return this;
                }
            }

            return null;
        }

        public override string LexicographicSortKey()
        {
            return string.Join("", from operand in operandList select operand.LexicographicSortKey());
        }
    }

    public abstract class Function : Operation
    {
        public Function() : base()
        {
        }

        public Function(List<Operand> operandList) : base(operandList)
        {

        }

        public abstract string Name(Format format);

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override bool IsAssociative()
        {
            return false;
        }

        // This is really clunky/hacky.  A variation of this might look for an operand in our list by the given name.
        public Operand GrabArg(string name, EvaluationContext context, Operand defaultOperand)
        {
            if (operandList.Count != 1 || !(operandList[0] is Variable variable))
                throw new EvaluationException("Expected variable argument.");

            string key = variable.name + name;
            if (!context.operandStorage.ContainsKey(key))
                return defaultOperand;

            return context.operandStorage[key];
        }

        public override string Print(Format format)
        {
            string name = Name(format);
            string args = string.Join(", ", from operand in operandList select operand.Print(format));
            if (format == Format.PARSEABLE)
                return name + "(" + args + ")";
            else if (format == Format.LATEX)
                return name + @"\left(" + args + @"\right)";
            return "?";
        }
    }

    public abstract class Collectable : Operand
    {
        public Operand scalar;

        public abstract bool Like(Collectable collectable);
        public abstract Operand Collect(Collectable collectable);
        public abstract bool CanAbsorb(Operand operand);

        public Collectable()
        {
            scalar = null;
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            if (scalar != null)
            {
                if (scalar.IsAdditiveIdentity)
                    return scalar;

                Operand newScalar = scalar.Evaluate(context);
                if (newScalar != null)
                {
                    this.scalar = newScalar;
                    return this;
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

        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.Evaluate(context);
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

                    if(collectableA.Like(collectableB))
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

        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count == 0)
                return new NumericScalar(1.0);

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                if(operand.IsAdditiveIdentity)
                    return new NumericScalar(0.0);

                if(operand.IsMultiplicativeIdentity)
                {
                    operandList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                NumericScalar scalarA = operandList[i] as NumericScalar;
                if(scalarA == null)
                    continue;

                for (int j = i + 1; j < operandList.Count; j++)
                {
                    NumericScalar scalarB = operandList[j] as NumericScalar;
                    if(scalarB == null)
                        continue;

                    operandList.RemoveAt(j);
                    operandList.RemoveAt(i);
                    operandList.Add(new NumericScalar(scalarA.value * scalarB.value));
                    return this;
                }
            }

            for(int i = 0; i < operandList.Count; i++)
            {
                if (!(operandList[i] is SymbolicScalarTerm scalarA))
                    continue;

                for(int j = i + 1; j < operandList.Count; j++)
                {
                    if (!(operandList[j] is SymbolicScalarTerm scalarB))
                        continue;

                    operandList.RemoveAt(j);
                    operandList.RemoveAt(i);
                    SymbolicScalarTerm scalar = new SymbolicScalarTerm(new GeometricProduct(new List<Operand>() { scalarA.scalar, scalarB.scalar }));
                    scalar.factorList = (from factor in scalarA.factorList.Concat(scalarB.factorList) select factor.Copy()).ToList();
                    operandList.Add(scalar);
                    return this;
                }
            }

            for(int i = 0; i < operandList.Count; i++)
            {
                Collectable collectable = operandList[i] as Collectable;
                if (collectable == null)
                    continue;

                for(int j = 0; j < operandList.Count; j++)
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

                    if(string.Compare(keyA, keyB) > 0)
                    {
                        operandList[i] = operandB;
                        operandList[j] = operandA;
                        return this;
                    }
                }
            }

            for(int i = 0; i < operandList.Count; i++)
            {
                Operand operandA = operandList[i];
                if (operandA.Grade <= 0)
                    continue;

                for(int j = i + 1; j < operandList.Count; j++)
                {
                    Operand operandB = operandList[j];
                    if (operandB.Grade != 0)
                        continue;

                    operandList.RemoveAt(j);
                    operandList.Insert(0, operandB);
                    return this;
                }
            }

            return base.Evaluate(context);
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

        public override int Grade
        {
            get
            {
                if (operandList.All(operand => operand.Grade == 0))
                    return 0;

                return -1;
            }
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            // To avoid infinite evaluation looping, we must apply...
            //   1) vB = v.B + v^B, and
            //   2) v^B = vB - v.B,
            // ...according to rules that dictate when and where they're appropriate.
            // Also to avoid infinite looping, the distributive property must take
            // precedence over anything we do here.

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
                    operandList[j] = new Sum(new List<Operand>() { geometricProduct, new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), innerProduct }) });
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

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
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
                        Sum sum = new Sum();

                        for (int i = 0; i < bladeB.vectorList.Count; i++)
                        {
                            Blade subBlade = bladeB.MakeSubBlade(i);
                            subBlade.scalar = new GeometricProduct(new List<Operand>() { new NumericScalar(i % 2 == 1 ? -1.0 : 1.0), bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[i]) });
                            sum.operandList.Add(subBlade);
                        }

                        return sum;
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade == 1)
                    {
                        operandList[0] = bladeB;
                        operandList[1] = bladeA;

                        if (bladeA.Grade % 2 == 0)
                            operandList.Add(new NumericScalar(-1.0));

                        return this;
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

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
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
                    operandList.RemoveAt(i);
                    operandList[i] = blade;
                    return this;
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

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if(operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Reverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].Grade == 0)
                return operandList[0];

            Operand reverse = operandList[0].Reverse();
            return reverse;
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

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand = base.Evaluate(context);
            if(operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Inverse operation expects exactly one operand, got {0}.", operandList.Count));

            if(operandList[0].IsAdditiveIdentity)
                throw new EvaluationException("Cannot invert the additive identity.");

            Operand inverse = operandList[0].Inverse();
            return inverse;
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

        // Notice that we try to cull by grade first before evaluating our main argument.
        // This is what may be an optimization by early detection of grade.  We must evaluate
        // our main argument as long as its grade remains indeterminant.  How good the optimization
        // is depends on how well we can determine the grade of an arbitrary operand tree.
        // Of course, some trees well never have a grade, such as a sum of blades of non-homogeneous grade.
        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count != 2)
                throw new EvaluationException(string.Format("Grade-part operation expects exactly two arguments, got {0}.", operandList.Count));

            int determinedGrade = operandList[0].Grade;
            if (determinedGrade != -1)
            {
                NumericScalar scalar = operandList[1] as NumericScalar;
                if (scalar != null)
                {
                    int desiredGrade = (int)scalar.value;
                    return determinedGrade == desiredGrade ? operandList[0] : new NumericScalar(0.0);
                }
            }

            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            return null;
        }

        public override string Print(Format format)
        {
            if(operandList.Count != 2)
                return "?";

            switch(format)
            {
                case Format.LATEX:
                {
                    NumericScalar scalar = operandList[1] as NumericScalar;
                    if (scalar == null)
                        return "?";

                    int desiredGrade = (int)scalar.value;
                    if (desiredGrade == 0)
                        return @"\left\langle" + operandList[0].Print(format) + @"\right\rangle";

                    return @"\left\langle" + operandList[0].Print(format) + @"\right\rangle_{" + desiredGrade.ToString() + "}";
                }
                case Format.PARSEABLE:
                {
                    return string.Format("grade({0},{1})", operandList[0].Print(format), operandList[1].Print(format));
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

        // Notice that we store the un-evaluated r-value in the l-value.  This is necessary if
        // we want to be able to create a dependency-chain between variables.
        public override Operand Evaluate(EvaluationContext context)
        {
            if (operandList.Count != 2)
                throw new EvaluationException(string.Format("Assignment operation expects exactly two operands, got {0}.", operandList.Count));

            Variable variable = operandList[0] as Variable;
            if(variable == null)
                throw new EvaluationException("Assignment operation expects an l-value of type variable.");

            if (context.operandStorage.ContainsKey(variable.name))
                context.operandStorage.Remove(variable.name);

            context.operandStorage.Add(variable.name, operandList[1].Copy());

            return operandList[1];
        }

        public override string Print(Format format)
        {
            if(operandList.Count == 2)
                return string.Format("{0} = {1}", operandList[0].Print(format), operandList[1].Print(format));

            return "?";
        }
    }

    public class Blade : Collectable
    {
        public List<string> vectorList;

        public override int Grade
        {
            get
            {
                if(scalar.IsAdditiveIdentity)
                    return 0;

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
            this.scalar = new NumericScalar(1.0);
        }

        public Blade(double scalar) : base()
        {
            vectorList = new List<string>();
            this.scalar = new NumericScalar(scalar);
        }

        public Blade(string vectorName) : base()
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = new NumericScalar(1.0);
        }

        public Blade(double scalar, string vectorName) : base()
        {
            vectorList = new List<string>();
            vectorList.Add(vectorName);
            this.scalar = new NumericScalar(scalar);
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

        public Blade(Operand scalar, List<string> vectorList) : base()
        {
            this.vectorList = vectorList;
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

        public override Operand Evaluate(EvaluationContext context)
        {
            if (Grade == 0)
                return scalar;

            // This is the only way we detect a linearly dependent set of vectors.
            for (int i = 0; i < vectorList.Count; i++)
                for (int j = i + 1; j < vectorList.Count; j++)
                    if (vectorList[i] == vectorList[j])
                        return new NumericScalar(0.0);

            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

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
                    scalar = new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), scalar });

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

        public override bool Like(Collectable collectable)
        {
            Blade blade = collectable as Blade;
            if (blade == null)
                return false;

            // This works because blades are sorted as part of their evaluation.
            return Enumerable.SequenceEqual<string>(vectorList, blade.vectorList);
        }

        public override Operand Collect(Collectable collectable)
        {
            Blade blade = collectable as Blade;
            return new Blade(new Sum(new List<Operand>() { scalar, blade.scalar }), vectorList);
        }

        public override bool CanAbsorb(Operand operand)
        {
            return operand is NumericScalar || operand is SymbolicScalarTerm;
        }

        public override string LexicographicSortKey()
        {
            return string.Join("", vectorList);
        }

        public override Operand Reverse()
        {
            int i = this.Grade;
            int j = i * (i - 1) / 2;
            if (j % 2 == 0)
                return this;

            return new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), this });
        }

        public override Operand Inverse()
        {
            // TODO: Write this.
            return null;
        }
    }

    public class NumericScalar : Operand
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

        public NumericScalar(double value = 0.0) : base()
        {
            this.value = value;
        }

        public override Operand Copy()
        {
            return new NumericScalar(this.value);
        }

        public override Operand New()
        {
            return new NumericScalar();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            return null;
        }

        public override string Print(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return string.Format("{0:F2}", this.value);
                case Format.PARSEABLE:
                    return string.Format("{0}", this.value);
            }

            return "?";
        }

        public override Operand Inverse()
        {
            try
            {
                return new NumericScalar(1.0 / this.value);
            }
            catch (DivideByZeroException)
            {
                throw new EvaluationException(string.Format("Attempted to invert scalar ({0}), but got divid-by-zero exception.", this.value));
            }
        }

        public override Operand Reverse()
        {
            return this;
        }
    }

    // These are monomials.
    public class SymbolicScalarTerm : Collectable
    {
        public abstract class Factor
        {
            public int exponent;

            public Factor()
            {
                exponent = 1;
            }

            public abstract Factor New();
            public abstract string PrintSymbol(Format format);
            public abstract bool Matches(Factor factor);
            public abstract string SortKey();

            public virtual Factor Copy()
            {
                Factor factor = New();
                factor.exponent = exponent;
                return factor;
            }

            public string Print(Format format)
            {
                string symbol = PrintSymbol(format);

                if (exponent == 1)
                    return symbol;

                if(format == Format.PARSEABLE)
                {
                    return string.Format("pow({0},{1})", symbol, exponent);
                }
                else if(format == Format.LATEX)
                {
                    return symbol + "^{" + exponent.ToString() + "}";
                }

                return "?";
            }

            public static string SubscriptNameForLatex(string name)
            {
                Regex rx = new Regex(@"^([a-zA-Z]+)([0-9]+)$");
                MatchCollection collection = rx.Matches(name);
                if (collection.Count != 1)
                    return name;

                Match match = collection[0];
                return match.Groups[0].Value + "_{" + match.Groups[1].Value + "}";
            }
        }

        public class Symbol : Factor
        {
            public string name;

            public Symbol() : base()
            {
            }

            public Symbol(string name) : base()
            {
                this.name = name;
            }

            public override Factor New()
            {
                return new Symbol();
            }

            public override Factor Copy()
            {
                Symbol symbol = base.Copy() as Symbol;
                symbol.name = this.name;
                return symbol;
            }

            public override string PrintSymbol(Format format)
            {
                if (format == Format.PARSEABLE)
                    return "$" + name;
                else if (format == Format.LATEX)
                    return SubscriptNameForLatex(name);
                return "?";
            }

            public override bool Matches(Factor factor)
            {
                return factor is Symbol symbol && symbol.name == name;
            }

            public override string SortKey()
            {
                return this.name;
            }
        }

        public class SymbolicDot : Factor
        {
            public string vectorNameA;
            public string vectorNameB;

            public SymbolicDot() : base()
            {
            }

            public SymbolicDot(string vectorNameA, string vectorNameB) : base()
            {
                this.vectorNameA = vectorNameA;
                this.vectorNameB = vectorNameB;
            }

            public override Factor New()
            {
                return new SymbolicDot();
            }

            public override Factor Copy()
            {
                SymbolicDot dot = base.Copy() as SymbolicDot;
                dot.vectorNameA = this.vectorNameA;
                dot.vectorNameB = this.vectorNameB;
                return dot;
            }

            public override string PrintSymbol(Format format)
            {
                if (format == Format.PARSEABLE)
                    return "(" + vectorNameA + "." + vectorNameB + ")";
                else if (format == Format.LATEX)
                    return @"\left(\vec{" + SubscriptNameForLatex(vectorNameA) + @"}\cdot\vec{" + SubscriptNameForLatex(vectorNameB) + @"}\right)";
                return "?";
            }

            public override bool Matches(Factor factor)
            {
                // Not that this works because of our sorting.
                return factor is SymbolicDot dot && dot.vectorNameA == vectorNameA && dot.vectorNameB == vectorNameB;
            }

            public override string SortKey()
            {
                return this.vectorNameA + this.vectorNameB;
            }
        }

        public List<Factor> factorList;

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
                return this.scalar.IsAdditiveIdentity;
            }
        }

        public override bool IsMultiplicativeIdentity
        {
            get
            {
                return this.scalar.IsMultiplicativeIdentity && factorList.Count == 0;
            }
        }

        public SymbolicScalarTerm() : base()
        {
            factorList = new List<Factor>();
            this.scalar = new NumericScalar(1.0);
        }

        public SymbolicScalarTerm(string name) : base()
        {
            factorList = new List<Factor>() { new Symbol(name) };
            this.scalar = new NumericScalar(1.0);
        }

        public SymbolicScalarTerm(string vectorNameA, string vectorNameB) : base()
        {
            factorList = new List<Factor>() { new SymbolicDot(vectorNameA, vectorNameB) };
            this.scalar = new NumericScalar(1.0);
        }

        public SymbolicScalarTerm(List<Factor> factorList) : base()
        {
            this.factorList = factorList;
            this.scalar = new NumericScalar(1.0);
        }

        public SymbolicScalarTerm(Operand scalar) : base()
        {
            factorList = new List<Factor>();
            this.scalar = scalar;
        }

        public override Operand Copy()
        {
            return new SymbolicScalarTerm((from factor in factorList select factor.Copy()).ToList());
        }

        public override Operand New()
        {
            return new SymbolicScalarTerm();
        }

        public override Operand Evaluate(EvaluationContext context)
        {
            if (factorList.Count == 0)
                return scalar;

            Operand operand = base.Evaluate(context);
            if (operand != null)
                return operand;

            for(int i = 0; i < factorList.Count; i++)
            {
                Factor factor = factorList[i];
                if(factor.exponent == 0)
                {
                    factorList.RemoveAt(i);
                    return this;
                }
            }

            for(int i = 0; i < factorList.Count; i++)
            {
                SymbolicDot dot = factorList[i] as SymbolicDot;
                if (dot != null && string.Compare(dot.vectorNameA, dot.vectorNameB) > 0)
                {
                    string name = dot.vectorNameA;
                    dot.vectorNameA = dot.vectorNameB;
                    dot.vectorNameB = name;
                    return this;
                }
            }

            for(int i = 0; i < factorList.Count; i++)
            {
                Factor factorA = factorList[i];

                for (int j = i + 1; j < factorList.Count; j++)
                {
                    Factor factorB = factorList[j];

                    if(factorA.Matches(factorB))
                    {
                        factorList.RemoveAt(j);
                        factorA.exponent += factorB.exponent;
                        return this;
                    }
                }
            }

            for(int i = 0; i < factorList.Count - 1; i++)
            {
                Factor factorA = factorList[i];
                Factor factorB = factorList[i + 1];

                if(string.Compare(factorA.SortKey(), factorB.SortKey()) > 0)
                {
                    factorList[i] = factorB;
                    factorList[i + 1] = factorA;
                    return this;
                }
            }

            return null;
        }

        public override string Print(Format format)
        {
            List<string> printedFactorList = (from factor in factorList select factor.Print(format)).ToList();

            if (!scalar.IsMultiplicativeIdentity)
            {
                if (format == Format.PARSEABLE)
                    printedFactorList.Insert(0, "(" + scalar.Print(format) + ")");
                else if (format == Format.LATEX)
                    printedFactorList.Insert(0, @"\left(" + scalar.Print(format) + @"\right)");
            }

            if (format == Format.PARSEABLE)
                return string.Join("*", printedFactorList);
            else if (format == Format.LATEX)
                return string.Join("", printedFactorList);

            return "?";
        }

        public override bool Like(Collectable collectable)
        {
            if(collectable is SymbolicScalarTerm term)
            {
                if(Enumerable.SequenceEqual<string>(from factor in factorList select factor.SortKey(), from factor in term.factorList select factor.SortKey()))
                {
                    if(Enumerable.SequenceEqual<int>(from factor in factorList select factor.exponent, from factor in term.factorList select factor.exponent))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override Operand Collect(Collectable collectable)
        {
            SymbolicScalarTerm term = collectable as SymbolicScalarTerm;
            term.scalar = new Sum(new List<Operand>() { scalar, term.scalar });
            return term;
        }

        public override bool CanAbsorb(Operand operand)
        {
            return operand is NumericScalar;
        }

        public override string LexicographicSortKey()
        {
            // What about sorting by the degree of the monomial?
            return string.Join("", (from factor in factorList select factor.SortKey()).ToList());
        }

        public override Operand Inverse()
        {
            SymbolicScalarTerm term = this.Copy() as SymbolicScalarTerm;
            term.scalar = new Inverse(new List<Operand>() { term.scalar });
            for (int i = 0; i < term.factorList.Count; i++)
                term.factorList[i].exponent *= -1;
            return term;
        }

        public override Operand Reverse()
        {
            return this;
        }
    }

    public class Variable : Operand
    {
        public string name;

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

        public override Operand Evaluate(EvaluationContext context)
        {
            Operand operand;
            if(context.operandStorage.TryGetValue(this.name, out operand))
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
                    return "@" + this.name;
                }
            }

            return "?";
        }

        public override string LexicographicSortKey()
        {
            return this.name;
        }
    }
}