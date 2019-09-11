using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
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
            public abstract string PrintSymbol(Format format, Context context);
            public abstract bool Matches(Factor factor);
            public abstract string SortKey();
            public abstract Operand Explode(ITranslator translator, Context context);

            public virtual Factor Copy()
            {
                Factor factor = New();
                factor.exponent = exponent;
                return factor;
            }

            public string Print(Format format, Context context)
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

            public override string PrintSymbol(Format format, Context context)
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

            public override Operand Explode(ITranslator translator, Context context)
            {
                GeometricProduct geometricProduct = new GeometricProduct();

                for (int i = 0; i < Math.Abs(exponent); i++)
                {
                    geometricProduct.operandList.Add(translator.Translate(new SymbolicScalarTerm(this.name), context));
                }

                if (exponent < 0)
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

            public override string PrintSymbol(Format format, Context context)
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

            public bool InvolvesVectors(string vectorNameA, string vectorNameB)
            {
                if(this.vectorNameA == vectorNameA && this.vectorNameB == vectorNameB)
                    return true;

                if(this.vectorNameA == vectorNameB && this.vectorNameB == vectorNameA)
                    return true;

                return false;
            }

            public override string SortKey()
            {
                return this.vectorNameA + this.vectorNameB;
            }

            public override Operand Explode(ITranslator translator, Context context)
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

        public override Operand EvaluationStep(Context context)
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

        public override string Print(Format format, Context context)
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

        public override bool IsLike(Collectable collectable)
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

        public override bool CanAbsorb(Operand operand)
        {
            return operand is NumericScalar;
        }

        public override string LexicographicSortKey()
        {
            // What about sorting by the degree of the monomial?
            return string.Join("", (from factor in factorList select factor.SortKey()).ToList());
        }

        public override Operand Inverse(Context context)
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

        public override Operand Explode(ITranslator translator, Context context)
        {
            GeometricProduct geometricProduct = new GeometricProduct();

            geometricProduct.operandList.Add(translator.Translate(scalar.Copy(), context));

            foreach (Factor factor in factorList)
            {
                geometricProduct.operandList.Add(factor.Explode(translator, context));
            }

            return geometricProduct;
        }
    }
}