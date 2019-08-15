using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra
{
    public class Factor : Function
    {
        public Factor() : base()
        {
        }

        public Factor(List<Operand> operandList) : base(operandList)
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
            return new Factor();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{factor}";

            return "factor";
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new MathException(string.Format("Factor operation expects exactly one operand, got {0}.", operandList.Count));

            operand = operandList[0];
            
            if(operand.Grade < 0)
                throw new MathException("Unable to determine grade of argument.");
            else if(operand.Grade == 0)
                throw new MathException("Polynomial factorization not yet implemented.");
            else
            {
                Sum multivector = CanonicalizeMultivector(operand);
                OuterProduct factorization = FactorMultivectorAsBlade(multivector, context);
                operand = Operand.ExhaustEvaluation(factorization.Copy(), context);
                Sum expansion = CanonicalizeMultivector(operand);

                if(expansion.operandList.Count != multivector.operandList.Count)
                    throw new MathException("The multivector is not a blade.");

                // Note that this should work by the sorting performed by the sum operation.
                double commonRatio = 0.0;
                for(int i = 0; i < expansion.operandList.Count; i++)
                {
                    Blade bladeA = multivector.operandList[i] as Blade;
                    Blade bladeB = expansion.operandList[i] as Blade;

                    if(!bladeA.IsLike(bladeB))
                        throw new MathException("The multivector is not a blade.");
                
                    double ratio = (bladeA.scalar as NumericScalar).value / (bladeB.scalar as NumericScalar).value;
                    if(commonRatio == 0.0)
                        commonRatio = ratio;
                    else if(Math.Abs(ratio - commonRatio) >= context.epsilon)
                        throw new MathException("The multivector is not a blade.");
                }

                factorization.operandList.Insert(0, new NumericScalar(commonRatio));
                factorization.freezeFlags |= FreezeFlag.DISTRIBUTION;
                
                operand = Operand.ExhaustEvaluation(factorization, context);
                factorization = operand as OuterProduct;
                if(factorization == null)
                    factorization = new OuterProduct(new List<Operand>() { operand });

                // Provide a way to get at the individual factors.
                for(int i = 0; i < factorization.operandList.Count; i++)
                    context.operandStorage.SetStorage($"factor{i}", factorization.operandList[i].Copy());

                return factorization;
            }
        }

        // Note that factorizations of blades are not generally unique.
        // Here I'm just going to see if I can find any factorization.
        // My method here is the obvious one, and probably quite naive.
        // Lastly, the returned factorization, if any, will be correct up to scale.
        // It is up to the caller to determine the correct scale.
        public static OuterProduct FactorMultivectorAsBlade(Sum multivector, Context context)
        {
            if(!multivector.operandList.All(operand => operand is Blade))
                throw new MathException("Can only factor elements in multivector form.");

            if(!multivector.operandList.All(blade => (blade as Blade).scalar is NumericScalar))
                throw new MathException("Cannot yet perform symbolic factorization of blades.");

            OuterProduct factorization = new OuterProduct();

            int grade = multivector.Grade;
            if (grade == -1)
                throw new MathException("Could not determine grade of given element.  It might not be homogeneous of a single grade.");
            else if (grade == 0 || grade == 1)
                factorization.operandList.Add(multivector.Copy());
            else
            {
                // Given a blade A of grade n>1 and any vector v such that v.A != 0,
                // our method here is based on the identity L*A = (v.A) ^ ((v.A).A),
                // where L is a non-zero scalar.  Here, v.A is of grade n-1, and
                // (v.A).A is of grade 1.  This suggests a recursive algorithm.

                // TODO: I've just discovered that my algorithm here doesn't always
                //       work when null vectors are involved.  The identity above is
                //       easy to prove in a purely euclidean GA, but I did not bother
                //       to try and verify it more generally.  Interestingly, I've
                //       found that the algorithm can still work in some cases involving
                //       null vectors, provided the right probing vector is used, but it
                //       isn't clear to me how to differentiate between probing vectors
                //       that will work and those that wont.  I need to revisit all this.

                Operand result = null;
                List<string> basisVectorList = context.ReturnBasisVectors();
                foreach(Sum vector in GenerateProbingVectors(basisVectorList))
                {
                    result = Operand.ExhaustEvaluation(new InnerProduct(new List<Operand>() { vector, multivector.Copy() }), context);
                    if(!result.IsAdditiveIdentity)
                        break;
                    else
                        result = null;
                }

                if(result == null)
                    throw new MathException("Failed to find a vector with non-zero projection down onto the blade.  This does not necessarily mean that the multivector doesn't factor as a blade.");

                Sum reducedMultivector = CanonicalizeMultivector(result);

                OuterProduct subFactorization = FactorMultivectorAsBlade(reducedMultivector, context);
                if(subFactorization.Grade != grade - 1)
                    throw new MathException($"Expected sub-factorization to be of grade {grade - 1}.");

                Operand vectorFactor = Operand.ExhaustEvaluation(new InnerProduct(new List<Operand>() { reducedMultivector, multivector }), context);
                if(vectorFactor.Grade != 1)
                    throw new MathException("Expected vector factor to be of grade one.");

                factorization.operandList = subFactorization.operandList;
                factorization.operandList.Add(vectorFactor);
            }   

            return factorization;
        }

        public static IEnumerable<Sum> GenerateProbingVectors(List<string> basisVectorList)
        {
            // Start with the vectors of the simplest form first.
            for (int i = 1; i <= basisVectorList.Count; i++)
            {
                List<string> vectorList = new List<string>();
                foreach (List<string> vectorComboList in GenerateVectorCombinations(basisVectorList, vectorList, 0, i, 0))
                {
                    vectorComboList.Sort();
                    Sum vector = new Sum(vectorComboList.Select(vectorName => (Operand)new Blade(1.0, vectorName)).ToList());
                    yield return vector;
                }
            }

            // Okay, now let's try some vectors in more random directions.
            Random random = new Random();
            for(int pass = 0; pass < 2; pass++)
            {
                for (int i = 2; i <= basisVectorList.Count; i++)
                {
                    List<string> vectorList = new List<string>();
                    foreach (List<string> vectorComboList in GenerateVectorCombinations(basisVectorList, vectorList, 0, i, 0))
                    {
                        vectorComboList.Sort();
                        Sum vector = new Sum(vectorComboList.Select(vectorName => (Operand)new Blade(random.NextDouble(), vectorName)).ToList());
                        yield return vector;
                    }
                }
            }
        }
    }
}
