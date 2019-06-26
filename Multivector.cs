using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Multivector<T>
    {
        public class Scalar
        {
            public Scalar(double value)
            {
                dynamic this_value = this.value;
                this_value = value;
            }

            public Scalar(T value)
            {
                this.value = value;
            }
            
            public T value;

            public Scalar Copy()
            {
                return new Scalar(value);
            }

            public void Add(Scalar scalarA, Scalar scalarB)
            {
                dynamic valueA = scalarA.value;
                dynamic valueB = scalarB.value;
                this.value = valueA + valueB;
            }

            public void Subtract(Scalar scalarA, Scalar scalarB)
            {
                dynamic valueA = scalarA.value;
                dynamic valueB = scalarB.value;
                this.value = valueA - valueB;
            }

            public void Multiply(Scalar scalarA, Scalar scalarB)
            {
                dynamic valueA = scalarA.value;
                dynamic valueB = scalarB.value;
                this.value = valueA * valueB;
            }

            public bool Divide(Scalar scalarA, Scalar scalarB)
            {
                dynamic valueA = scalarA.value;
                dynamic valueB = scalarB.value;
                try
                {
                    this.value = valueA / valueB;
                    return true;
                }
                catch (DivideByZeroException)
                {
                    return false;
                }
            }

            public void Negate(Scalar scalar)
            {
                dynamic value = scalar.value;
                this.value = -value;
            }

            public bool Invert(Scalar scalar)
            {
                dynamic value = scalar.value;
                try
                {
                    this.value = 1.0 / value;
                    return true;
                }
                catch (DivideByZeroException)
                {
                    return false;
                }
            }
        }

        public class Vector
        {
            public string name;

            public Vector(string name)
            {
                this.name = name;
            }

            public Vector Copy()
            {
                return new Vector(name);
            }
        }

        public class Term
        {
            public enum Type
            {
                BLADE,
                VERSOR
            };

            public Type type;
            public Scalar scalar;
            public List<Vector> productOfVectors;

            public Term()
            {
                type = Type.BLADE;
                scalar = new Scalar(1.0);
                productOfVectors = new List<Vector>();
            }

            Term Copy()
            {
                Term copy = new Term();
                copy.type = this.type;
                copy.scalar = this.scalar.Copy();
                copy.productOfVectors = (from vector in productOfVectors select vector.Copy()).ToList();
                return copy;
            }

            public Multivector<T> Transform()
            {
                var result = new Multivector<T>();

                if (productOfVectors.Count() < 2)
                {
                    Term term = this.Copy();

                    if (type == Type.BLADE)
                        term.type = Type.VERSOR;
                    else if (type == Type.VERSOR)
                        term.type = Type.BLADE;

                    result.sumOfTerms.Add(term);
                }
                else
                {
                    if (type == Type.BLADE)
                    {
                        // TODO: v1^...^vN = v1(v2^...vN) - v1.(v2^...vN)
                    }
                    else if (type == Type.VERSOR)
                    {
                        // TODO: v1...vN = v1.(v2...vN) + v1^(v2...vN) - <v1...vN>
                    }
                }

                return result;
            }
        }

        public List<Term> sumOfTerms;

        public Multivector()
        {
            sumOfTerms = new List<Term>();
        }

        public void Add(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
            sumOfTerms = multivectorA.sumOfTerms.Concat(multivectorB.sumOfTerms).ToList();

            // TODO: Combine like terms.
        }

        public void Subtract(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
            var negatedB = new Multivector<T>();
            negatedB.Negate(multivectorB);
            Add(multivectorA, negatedB);
        }

        public void Multiply(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
            sumOfTerms = new List<Term>();

            foreach (Term termA in multivectorA.sumOfTerms)
            {
                foreach (Term termB in multivectorB.sumOfTerms)
                {
                    var product = new Multivector<T>();
                    product.Multiply(termA, termB);
                    Add(this, product);
                }
            }
        }

        private void Multiply(Term termA, Term termB)
        {
            sumOfTerms = new List<Term>();

            if (termA.type == Term.Type.VERSOR && termB.type == Term.Type.VERSOR)
            {
                var term = new Term();
                term.productOfVectors = termA.productOfVectors.Concat(termB.productOfVectors).ToList();
                term.scalar.Multiply(termA.scalar, termB.scalar);
                sumOfTerms.Add(term);
            }
            else if (termA.type == Term.Type.VERSOR && termB.type == Term.Type.BLADE)
            {
                var multivectorB = termB.Transform();
                foreach (Term versorB in multivectorB.sumOfTerms)
                {
                    var product = new Multivector<T>();
                    product.Multiply(termA, versorB);
                    Add(this, product);
                }
            }
            else if (termA.type == Term.Type.BLADE && termB.type == Term.Type.VERSOR)
            {
                var multivectorA = termA.Transform();
                foreach (Term versorA in multivectorA.sumOfTerms)
                {
                    var product = new Multivector<T>();
                    product.Multiply(versorA, termB);
                    Add(this, product);
                }
            }
            else if (termA.type == Term.Type.BLADE && termB.type == Term.Type.BLADE)
            {
                var multivectorA = termA.Transform();
                var multivectorB = termB.Transform();
                foreach (Term versorA in multivectorA.sumOfTerms)
                {
                    foreach (Term versorB in multivectorB.sumOfTerms)
                    {
                        var product = new Multivector<T>();
                        product.Multiply(versorA, versorB);
                        Add(this, product);
                    }
                }
            }
        }

        public bool Divide(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
            var invertedB = new Multivector<T>();
            if (!invertedB.Invert(multivectorB))
                return false;

            Multiply(multivectorA, invertedB);
            return true;
        }

        public void InnerProduct(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
        }

        public void OuterProduct(Multivector<T> multivectorA, Multivector<T> multivectorB)
        {
        }

        public void HomogenizeTermType(Term.Type type)
        {
        }

        public void Negate(Multivector<T> multivector)
        {
        }

        public bool Invert(Multivector<T> multivector)
        {
            // TODO: In practice, this amounts to solving a matrix equation, but coming up with that equation isn't immediately obvious to me.
            //       If we reduced the entire GA to a matrix algebra, then it would be clear, but I don't know how to do that either.
            return false;
        }

        public string ToString()
        {
            return "";
        }

        public bool FromString(string text)
        {
            return false;
        }
    }
}
