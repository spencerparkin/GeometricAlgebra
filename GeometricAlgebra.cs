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
        private Dictionary<string, Operand> operandStorage;
        public List<Function> funcList;
        public List<string> logMessageList;

        public EvaluationContext()
        {
            funcList = new List<Function>();
            operandStorage = new Dictionary<string, Operand>();
            logMessageList = new List<string>();

            funcList.Add(new Inverse());
            funcList.Add(new Reverse());
            funcList.Add(new GradePart());
        }

        public void Log(string message)
        {
            logMessageList.Add(message);
        }

        public void ClearStorage(string variableName)
        {
            if (operandStorage.ContainsKey(variableName))
                operandStorage.Remove(variableName);
        }

        public void SetStorage(string variableName, Operand operand)
        {
            ClearStorage(variableName);

            operandStorage.Add(variableName, operand.Copy());
        }

        public bool GetStorage(string variableName, ref Operand operand)
        {
            if(!operandStorage.ContainsKey(variableName))
                return false;

            operand = operandStorage[variableName].Copy();
            return true;
        }

        // The operand returned here should have grade zero.
        public virtual Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            return new SymbolicScalarTerm(vectorNameA, vectorNameB);
        }

        public virtual bool IsLinearlyDependentSet(List<string> vectorNameList)
        {
            List<string> basisVectorList = ReturnBasisVectors();
            if(basisVectorList != null)
            {
                if (vectorNameList.Count > basisVectorList.Count)
                    return true;
            }

            return false;
        }

        public virtual List<string> ReturnBasisVectors()
        {
            return null;
        }

        public virtual Operation CreateFunction(string name)
        {
            IEnumerable<Function> enumerable = from func in funcList where func.Name(Operand.Format.PARSEABLE) == name select func;
            if (enumerable.Count() == 0)
                return null;

            return enumerable.ToArray()[0].Copy() as Operation;
        }

        public virtual string TranslateVectorNameForLatex(string vectorName)
        {
            return @"\vec{" + SubscriptNameForLatex(vectorName) + "}";
        }

        public virtual string TranslateScalarNameForLatex(string scalarName)
        {
            return SubscriptNameForLatex(scalarName);
        }

        public virtual string TranslateVariableNameForLatex(string varName)
        {
            return @"\textbf{" + SubscriptNameForLatex(varName) + "}";
        }

        public static string SubscriptNameForLatex(string name)
        {
            Regex rx = new Regex(@"^([a-zA-Z]+)([0-9]+)$");
            MatchCollection collection = rx.Matches(name);
            if (collection.Count != 1)
                return name;

            Match match = collection[0];
            return match.Groups[1].Value + "_{" + match.Groups[2].Value + "}";
        }
    }

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

        public abstract string Print(Format format, EvaluationContext context = null);

        // Derivatives overriding this virtual method are to return null
        // if no algebraic manipulation of the sub-tree rooted at this object
        // is performed.  On the other hand, if such a manipulation is performed,
        // then the new or existing root should be returned.
        public virtual Operand EvaluationStep(EvaluationContext context)
        {
            return null;
        }

        public static Operand ExhaustEvaluation(Operand operand, EvaluationContext context)
        {
            while (true)
            {
                Operand newOperand = operand.EvaluationStep(context);
                if (newOperand != null)
                    operand = newOperand;
                else
                    break;
            }

            return operand;
        }

        public static HashSet<int> DiscoverGrades(Operand operand, EvaluationContext context)
        {
            List<string> basisVectorList = context.ReturnBasisVectors();
            HashSet<int> gradeSet = new HashSet<int>();

            for(int i = 0; i <= basisVectorList.Count; i++)
            {
                Operand gradePart = new GradePart(new List<Operand>() { operand.Copy(), new NumericScalar(i) });
                gradePart = ExhaustEvaluation(gradePart, context);
                if(!gradePart.IsAdditiveIdentity)
                    gradeSet.Add(i);
            }

            return gradeSet;
        }

        public static (Operand input, Operand output, string error) Evaluate(string expression, EvaluationContext context)
        {
            Operand inputResult = null;
            Operand outputResult = null;
            string error = "";

            try
            {
                Parser parser = new Parser(context, false);
                inputResult = parser.Parse(expression);
                outputResult = ExhaustEvaluation(inputResult.Copy(), context);

                // Sadly, I've come across cases where it just takes way too long
                // to perform the following calculation, so I'm just going to have
                // to comment this out for now.  The solution did work, however, for
                // the case I encountered where it was needed.  Until I can think of
                // something else, it has to go.
#if false
                // If a symbolic vector was generated during parsing, then the
                // evaluation of the expression does not always reduce all
                // grade parts to zero that can be.  The only sure solution
                // I can think of is to redo the calculation, but only allow
                // basis vectors.  This will give us an expression that does
                // fully reduce in terms of grade cancellation.
                if(parser.generatedSymbolicVector)
                {
                    HashSet<int> gradeSetA = DiscoverGrades(outputResult, context);

                    parser = new Parser(context, true);
                    Operand basisResult = parser.Parse(expression);
                    basisResult = ExhaustEvaluation(basisResult, context);
                
                    HashSet<int> gradeSetB = DiscoverGrades(basisResult, context);

                    if(gradeSetB.IsProperSubsetOf(gradeSetA))
                    {
                        outputResult = new GradePart(new List<Operand>() { outputResult }.Concat(from i in gradeSetB select new NumericScalar(i)).ToList());
                        outputResult = ExhaustEvaluation(outputResult, context);
                    }
                }
#endif
            }
            catch(Exception exc)
            {
                error = exc.Message;
            }

            return (inputResult, outputResult, error);
        }

        public virtual string LexicographicSortKey()
        {
            return "";
        }
        
        // This is with respect to the geometric product.
        public virtual Operand Inverse(EvaluationContext context)
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

        public override string Print(Format format, EvaluationContext context)
        {
            List<string> printList = new List<string>();

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                string subPrint = operand.Print(format, context);
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

        public override Operand EvaluationStep(EvaluationContext context)
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
                Operand newOperand = oldOperand.EvaluationStep(context);

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

        public override string Print(Format format, EvaluationContext context)
        {
            string name = Name(format);
            string args = string.Join(", ", from operand in operandList select operand.Print(format, context));
            if (format == Format.PARSEABLE)
                return name + "(" + args + ")";
            else if (format == Format.LATEX)
                return name + @"\left(" + args + @"\right)";
            return "?";
        }
    }

    public interface ITranslator
    {
        Operand Translate(Operand operand, EvaluationContext context);
    }

    public abstract class Collectable : Operand
    {
        public Operand scalar;

        public abstract bool Like(Collectable collectable);
        public abstract Operand Collect(Collectable collectable);
        public abstract bool CanAbsorb(Operand operand);
        public abstract Operand Explode(ITranslator translator, EvaluationContext context);

        public Collectable()
        {
            scalar = null;
        }

        public override Operand Copy()
        {
            Collectable collectable = New() as Collectable;
            collectable.scalar = this.scalar.Copy();
            return collectable;
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (scalar != null)
            {
                if (scalar.IsAdditiveIdentity)
                    return scalar;

                Operand newScalar = scalar.EvaluationStep(context);
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (operandList.Count == 0)
                return new Blade(0.0);

            Operand operand = base.EvaluationStep(context);
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

        public override Operand Inverse(EvaluationContext context)
        {
            // This is a super hard problem, but maybe we can handle the following case.

            if(!operandList.All(operand => operand is Blade))
                return null;

            if(!operandList.All(operand => (operand as Blade).scalar is NumericScalar))
                return null;

            List<string> basisVectorList = context.ReturnBasisVectors();
            if(!operandList.All(operand => (operand as Blade).vectorList.All(vectorName => basisVectorList.Contains(vectorName))))
                return null;

            Sum multivectorA = this.Copy() as Sum;
            Sum multivectorB = this.Copy() as Sum;

            for(int i = 0; i < multivectorB.operandList.Count; i++)
            {
                Blade blade = multivectorB.operandList[i] as Blade;
                string scalarName = string.Format("__x{0}__", i);
                blade.scalar = new GeometricProduct(new List<Operand>() { new SymbolicScalarTerm(scalarName), blade.scalar });
            }

            GeometricProduct geometricProduct = new GeometricProduct();
            geometricProduct.operandList.Add(multivectorA);
            geometricProduct.operandList.Add(multivectorB);

            Operand result = ExhaustEvaluation(geometricProduct, context);

            // TODO: Now read the linear equations off of each part of the resulting multivector.
            //       The grade zero part should be one; all others zero.  Build a matrix; solve the system.
            //       I'll need to get my hands on a linear algebra library, or heaven forbid, write my own.

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

        public override Operand EvaluationStep(EvaluationContext context)
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

            return base.EvaluationStep(context);
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
                    return @"\times";
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = base.EvaluationStep(context);
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = base.EvaluationStep(context);
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = base.EvaluationStep(context);
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

    public class Reverse : Function
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

        public override string Name(Format format)
        {
            return "reverse";
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Reverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].Grade == 0)
                return operandList[0];

            Operand reverse = operandList[0].Reverse();
            return reverse;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            if (operandList.Count != 1)
                return "?";

            switch (format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format, context) + @"\right)^{\tilde}";
                case Format.PARSEABLE:
                    return base.Print(format, context);
            }

            return "?";
        }
    }

    public class Inverse : Function
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

        public override string Name(Format format)
        {
            return "inverse";
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (operandList.Count != 1)
                throw new EvaluationException(string.Format("Inverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].IsAdditiveIdentity)
                throw new EvaluationException("Cannot invert the additive identity.");

            if(operandList[0] is GeometricProduct oldGeometricProduct)
            {
                GeometricProduct newGeometricProduct = new GeometricProduct();

                for(int i = oldGeometricProduct.operandList.Count - 1; i >= 0; i--)
                {
                    newGeometricProduct.operandList.Add(new Inverse(new List<Operand>() { oldGeometricProduct.operandList[i] }));
                }

                return newGeometricProduct;
            }

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;            

            Operand inverse = operandList[0].Inverse(context);
            return inverse;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            if(operandList.Count != 1)
                return "?";

            switch(format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format, context) + @"\right)^{-1}";
                case Format.PARSEABLE:
                    return base.Print(format, context);
            }

            return "?";
        }
    }

    public class GradePart : Function
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

        public override string Name(Format format)
        {
            return "grade";
        }

        // Notice that we try to cull by grade first before evaluating our main argument.
        // This is what may be an optimization by early detection of grade.  We must evaluate
        // our main argument as long as its grade remains indeterminant.  How good the optimization
        // is depends on how well we can determine the grade of an arbitrary operand tree.
        // Of course, some trees well never have a grade, such as a sum of blades of non-homogeneous grade.
        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (operandList.Count < 2)
                throw new EvaluationException(string.Format("Grade-part operation expects two or more arguments, got {0}.", operandList.Count));

            int grade = operandList[0].Grade;
            if (grade == -1)
            {
                Operand operand = base.EvaluationStep(context);
                if(operand != null)
                    return operand;
            }
            else
            {
                var gradeSet = new HashSet<int>();
                for(int i = 1; i < operandList.Count; i++)
                {
                    NumericScalar scalar = operandList[i] as NumericScalar;
                    if(scalar == null)
                    {
                        Operand operand = operandList[i].EvaluationStep(context);
                        if(operand != null)
                        {
                            operandList[i] = operand;
                            return this;
                        }

                        throw new EvaluationException("Encountered non-numeric-scalar when looking for grade arguments.");
                    }

                    gradeSet.Add((int)scalar.value);
                }

                if(gradeSet.Contains(grade))
                    return operandList[0];

                return new NumericScalar(0.0);
            }

            return null;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            switch (format)
            {
                case Format.LATEX:
                {
                    return @"\left\langle" + operandList[0].Print(format, context) + @"\right\rangle_{" + string.Join(",", (from operand in operandList.Skip(1) select operand.Print(format, context)).ToList()) + "}";
                }
                case Format.PARSEABLE:
                {
                    return base.Print(format, context);
                }
            }

            return "?";
        }
    }

    public class Assignment : Operation
    {
        public bool storeEvaluation;

        public Assignment(bool storeEvaluation = true) : base()
        {
            this.storeEvaluation = storeEvaluation;
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

        public override Operand Copy()
        {
            var assignment = base.Copy() as Assignment;
            assignment.storeEvaluation = this.storeEvaluation;
            return assignment;
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (operandList.Count != 2)
                throw new EvaluationException(string.Format("Assignment operation expects exactly two operands, got {0}.", operandList.Count));

            Variable variable = operandList[0] as Variable;
            if(variable == null)
                throw new EvaluationException("Assignment operation expects an l-value of type variable.");

            // This is important so that our l-value doesn't evaluate as something other than a variable before we have a chance to assign it.
            context.ClearStorage(variable.name);

            // Sometimes we want a dependency-chain; sometimes we don't.
            if (storeEvaluation)
            {
                Operand operand = base.EvaluationStep(context);
                if (operand != null)
                    return operand;
            }

            // Perform the assignment.
            context.SetStorage(variable.name, operandList[1].Copy());

            return operandList[1];
        }

        public override string Print(Format format, EvaluationContext context)
        {
            if(operandList.Count == 2)
                return string.Format("{0} = {1}", operandList[0].Print(format, context), operandList[1].Print(format, context));

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
            Blade clone = base.Copy() as Blade;
            clone.vectorList = (from vectorName in this.vectorList select vectorName).ToList();
            return clone;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            if (Grade == 0)
                return scalar.Print(format, context);

            string printedBlade = "?";

            if(format == Format.LATEX)
                printedBlade = string.Join(@"\wedge", from vectorName in vectorList select (context == null ? vectorName : context.TranslateVectorNameForLatex(vectorName)));
            else if(format == Format.PARSEABLE)
                printedBlade = string.Join("^", vectorList);

            if(!scalar.IsMultiplicativeIdentity)
            {
                if(format == Format.LATEX)
                    printedBlade = @"\left(" + scalar.Print(format, context) + @"\right)" + printedBlade;
                else if(format == Format.PARSEABLE)
                    printedBlade = "(" + scalar.Print(format, context) + ")*" + printedBlade;
            }

            return printedBlade;
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (Grade == 0)
                return scalar;

            // Handle an easy case of a linearly dependent set of vectors.
            for (int i = 0; i < vectorList.Count; i++)
                for (int j = i + 1; j < vectorList.Count; j++)
                    if (vectorList[i] == vectorList[j])
                        return new NumericScalar(0.0);

            // The context may also have something to say about linear dependence.
            if (context.IsLinearlyDependentSet(vectorList))
                return new NumericScalar(0.0);

            Operand operand = base.EvaluationStep(context);
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

        // Note that this isn't terribly helpful, because an inverse isn't taken/considered until
        // the element to be inverted is fully expanded, at which point it is not easily recognized
        // as a blade (that is, unless a factorization algorithm was applied.)  Such an algorithm, however,
        // should not be needed at all, though, because the inverter should just consider the general
        // case of multivectors.
        public override Operand Inverse(EvaluationContext context)
        {
            GeometricProduct geometricProduct = new GeometricProduct();

            // This is correct up to sign.
            // TODO: Determine correct sign.
            geometricProduct.operandList.Add(new Reverse(new List<Operand>() { this.Copy() }));
            geometricProduct.operandList.Add(new Inverse(new List<Operand>(){ new InnerProduct(new List<Operand>() { this.Copy(), this.Copy() }) }));

            return geometricProduct;
        }

        public override Operand Explode(ITranslator translator, EvaluationContext context)
        {
            OuterProduct outerProduct = new OuterProduct();

            outerProduct.operandList.Add(translator.Translate(scalar.Copy(), context));

            foreach(string vectorName in vectorList)
            {
                outerProduct.operandList.Add(translator.Translate(new Blade(vectorName), context));
            }

            return outerProduct;
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            return null;
        }

        public override string Print(Format format, EvaluationContext context)
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

        public override Operand Inverse(EvaluationContext context)
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

            public Factor(int exponent)
            {
                this.exponent = exponent;
            }

            public abstract Factor New();
            public abstract string PrintSymbol(Format format, EvaluationContext context);
            public abstract bool Matches(Factor factor);
            public abstract string SortKey();
            public abstract Operand Explode(ITranslator translator, EvaluationContext context);

            public virtual Factor Copy()
            {
                Factor factor = New();
                factor.exponent = exponent;
                return factor;
            }

            public string Print(Format format, EvaluationContext context)
            {
                string symbol = PrintSymbol(format, context);

                if (exponent == 1)
                    return symbol;

                if (format == Format.PARSEABLE)
                {
                    return string.Format("pow({0},{1})", symbol, exponent);
                }
                else if (format == Format.LATEX)
                {
                    return symbol + "^{" + exponent.ToString() + "}";
                }

                return "?";
            }
        }

        public class Symbol : Factor
        {
            public string name;

            public Symbol() : base()
            {
            }

            public Symbol(string name, int exponent) : base(exponent)
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

            public override string PrintSymbol(Format format, EvaluationContext context)
            {
                if (format == Format.PARSEABLE)
                    return "$" + name;
                else if (format == Format.LATEX)
                    return context == null ? name : context.TranslateScalarNameForLatex(name);
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

            public override Operand Explode(ITranslator translator, EvaluationContext context)
            {
                GeometricProduct geometricProduct = new GeometricProduct();

                for(int i = 0; i < Math.Abs(exponent); i++)
                {
                    geometricProduct.operandList.Add(translator.Translate(new SymbolicScalarTerm(this.name), context));
                }

                if(exponent < 0)
                    return new Inverse(new List<Operand>() { geometricProduct });

                return geometricProduct;
            }
        }

        public class SymbolicDot : Factor
        {
            public string vectorNameA;
            public string vectorNameB;

            public SymbolicDot() : base()
            {
            }

            public SymbolicDot(string vectorNameA, string vectorNameB, int exponent) : base(exponent)
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

            public override string PrintSymbol(Format format, EvaluationContext context)
            {
                if (format == Format.PARSEABLE)
                    return "(" + vectorNameA + "." + vectorNameB + ")";
                else if (format == Format.LATEX)
                    return @"\left(" + (context == null ? vectorNameA : context.TranslateVectorNameForLatex(vectorNameA)) + @"\cdot " + (context == null ? vectorNameB : context.TranslateVectorNameForLatex(vectorNameB)) + @"\right)";
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

            public override Operand Explode(ITranslator translator, EvaluationContext context)
            {
                GeometricProduct geometricProduct = new GeometricProduct();

                for (int i = 0; i < Math.Abs(exponent); i++)
                {
                    InnerProduct innerProduct = new InnerProduct();
                    innerProduct.operandList.Add(translator.Translate(new Blade(this.vectorNameA), context));
                    innerProduct.operandList.Add(translator.Translate(new Blade(this.vectorNameB), context));

                    geometricProduct.operandList.Add(innerProduct);
                }

                if (exponent < 0)
                    return new Inverse(new List<Operand>() { geometricProduct });

                return geometricProduct;
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

        public SymbolicScalarTerm(string name, int exponent = 1) : base()
        {
            factorList = new List<Factor>() { new Symbol(name, exponent) };
            this.scalar = new NumericScalar(1.0);
        }

        public SymbolicScalarTerm(string vectorNameA, string vectorNameB, int exponent = 1) : base()
        {
            factorList = new List<Factor>() { new SymbolicDot(vectorNameA, vectorNameB, exponent) };
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
            SymbolicScalarTerm clone = base.Copy() as SymbolicScalarTerm;
            clone.factorList = (from factor in factorList select factor.Copy()).ToList();
            return clone;
        }

        public override Operand New()
        {
            return new SymbolicScalarTerm();
        }

        public override Operand EvaluationStep(EvaluationContext context)
        {
            if (factorList.Count == 0)
                return scalar;

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            for (int i = 0; i < factorList.Count; i++)
            {
                Factor factor = factorList[i];
                if (factor.exponent == 0)
                {
                    factorList.RemoveAt(i);
                    return this;
                }
            }

            for (int i = 0; i < factorList.Count; i++)
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

            for (int i = 0; i < factorList.Count; i++)
            {
                Factor factorA = factorList[i];

                for (int j = i + 1; j < factorList.Count; j++)
                {
                    Factor factorB = factorList[j];

                    if (factorA.Matches(factorB))
                    {
                        factorList.RemoveAt(j);
                        factorA.exponent += factorB.exponent;
                        return this;
                    }
                }
            }

            for (int i = 0; i < factorList.Count - 1; i++)
            {
                Factor factorA = factorList[i];
                Factor factorB = factorList[i + 1];

                if (string.Compare(factorA.SortKey(), factorB.SortKey()) > 0)
                {
                    factorList[i] = factorB;
                    factorList[i + 1] = factorA;
                    return this;
                }
            }

            return null;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            List<string> printedFactorList = (from factor in factorList select factor.Print(format, context)).ToList();

            if (!scalar.IsMultiplicativeIdentity)
            {
                if (format == Format.PARSEABLE)
                    printedFactorList.Insert(0, "(" + scalar.Print(format, context) + ")");
                else if (format == Format.LATEX)
                    printedFactorList.Insert(0, @"\left(" + scalar.Print(format, context) + @"\right)");
            }

            if (format == Format.PARSEABLE)
                return string.Join("*", printedFactorList);
            else if (format == Format.LATEX)
                return string.Join("", printedFactorList);

            return "?";
        }

        public override bool Like(Collectable collectable)
        {
            if (collectable is SymbolicScalarTerm term)
            {
                if (Enumerable.SequenceEqual<string>(from factor in factorList select factor.SortKey(), from factor in term.factorList select factor.SortKey()))
                {
                    if (Enumerable.SequenceEqual<int>(from factor in factorList select factor.exponent, from factor in term.factorList select factor.exponent))
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

        public override Operand Inverse(EvaluationContext context)
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

        public override Operand Explode(ITranslator translator, EvaluationContext context)
        {
            GeometricProduct geometricProduct = new GeometricProduct();

            geometricProduct.operandList.Add(translator.Translate(scalar.Copy(), context));

            foreach(Factor factor in factorList)
            {
                geometricProduct.operandList.Add(factor.Explode(translator, context));
            }

            return geometricProduct;
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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = null;
            if(context.GetStorage(this.name, ref operand))
                return operand;

            return null;
        }

        public override string Print(Format format, EvaluationContext context)
        {
            switch(format)
            {
                case Format.LATEX:
                {
                    return context.TranslateVariableNameForLatex(this.name);
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