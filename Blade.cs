using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Blade : Collectable
    {
        public List<string> vectorList;

        public override int Grade
        {
            get
            {
                if (scalar.IsAdditiveIdentity)
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

        public override string Print(Format format, Context context)
        {
            if (Grade == 0)
                return scalar.Print(format, context);

            string printedBlade = "?";

            if (format == Format.LATEX)
                printedBlade = string.Join(@"\wedge", from vectorName in vectorList select (context == null ? vectorName : context.TranslateVectorNameForLatex(vectorName)));
            else if (format == Format.PARSEABLE)
                printedBlade = string.Join("^", vectorList);

            if (!scalar.IsMultiplicativeIdentity)
            {
                if (format == Format.LATEX)
                    printedBlade = @"\left(" + scalar.Print(format, context) + @"\right)" + printedBlade;
                else if (format == Format.PARSEABLE)
                    printedBlade = "(" + scalar.Print(format, context) + ")*" + printedBlade;
            }

            return printedBlade;
        }

        public override Operand EvaluationStep(Context context)
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

        public override Operand Inverse(Context context)
        {
            GeometricProduct geometricProduct = new GeometricProduct();

            // Without loss of generality, we can always write a blade in terms of an orthogonal basis.
            // It's then easy to realizing that a blade times its reverse is always a scalar.
            geometricProduct.operandList.Add(new Reverse(new List<Operand>() { this.Copy() }));
            geometricProduct.operandList.Add(new Inverse(new List<Operand>() { new GeometricProduct(new List<Operand>() { this.Copy(), new Reverse(new List<Operand>() { this.Copy() }) }) }));

            return geometricProduct;
        }

        public override Operand Explode(ITranslator translator, Context context)
        {
            OuterProduct outerProduct = new OuterProduct();

            outerProduct.operandList.Add(translator.Translate(scalar.Copy(), context));

            foreach (string vectorName in vectorList)
            {
                outerProduct.operandList.Add(translator.Translate(new Blade(vectorName), context));
            }

            return outerProduct;
        }
    }
}