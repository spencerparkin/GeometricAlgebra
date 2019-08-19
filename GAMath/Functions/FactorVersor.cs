using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class FactorVersor : Function
    {
        public FactorVersor() : base()
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{factor\_versor}";

            return "factor_versor";
        }

        public override Operand New()
        {
            return new FactorVersor();
        }

        public override string ShortDescription
        {
            get { return "Factor the given versor as a geometric product of vectors."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("An attempt is made to factor the given multivector as a geometric product of vectors.");
            context.Log("If it is a versor, the factorization procedure should succeed.");
            context.Log("If it is not a versor, an error is generated.");
            context.Log("The computed factorization, when expanded, should always recover the originally given multivector.");
        }

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Versor factor function expected exactly one argument; got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            GeometricProduct factorization = Factor(operandList[0], context);
            factorization.freezeFlags |= FreezeFlag.DISTRIBUTION;

            // Provide a way to get at the individual factors.
            for (int i = 0; i < factorization.operandList.Count; i++)
                context.operandStorage.SetStorage($"factor{i}", factorization.operandList[i].Copy());

            return operand;
        }

        // The algorithm implemented here comes straight out of Christian Perwass' book.
        // TODO: This is almost correct.  There is a sign bug.
        public static GeometricProduct Factor(Operand operand, Context context)
        {
            Operand currentVersor = operand.Copy();
            GeometricProduct versorFactorization = new GeometricProduct();

            while(true)
            {
                HashSet<int> gradeSet = DiscoverGrades(currentVersor, context);
                int maxGrade = gradeSet.ToList().Aggregate(0, (currentMaxGrade, grade) => grade > currentMaxGrade ? grade : currentMaxGrade);
                if(maxGrade <= 0)
                    break;

                Operand blade = Operand.ExhaustEvaluation(new GradePart(new List<Operand>() { currentVersor.Copy(), new NumericScalar(maxGrade) }), context);
                OuterProduct bladeFactorization = null;
            
                try
                {
                    bladeFactorization = FactorBlade.Factor(blade, context);
                }
                catch(MathException exc)
                {
                    throw new MathException("Highest-grade-part of multivector does not factor as a blade.", exc);
                }

                Operand nonNullVector = null, squareLength = null;

                foreach(Operand vector in bladeFactorization.operandList)
                {
                    if(vector.Grade == 1)
                    {
                        squareLength = Operand.ExhaustEvaluation(new Trim(new List<Operand>() { new InnerProduct(new List<Operand>() { vector.Copy(), vector.Copy() }) }), context);
                        if(!squareLength.IsAdditiveIdentity)
                        {
                            nonNullVector = vector;
                            break;
                        }
                    }
                }

                if(nonNullVector == null)
                    throw new MathException("Failed to find non-null vector in blade factorization.");

                Operand unitVector = Operand.ExhaustEvaluation(new GeometricProduct(new List<Operand>() { nonNullVector, new Power(new List<Operand>() { squareLength, new NumericScalar(-0.5) }) }), context);

                versorFactorization.operandList.Add(unitVector);

                currentVersor = Operand.ExhaustEvaluation(new Trim(new List<Operand>() { new GeometricProduct(new List<Operand>() { currentVersor, unitVector.Copy() }) }), context);
            }

            versorFactorization.operandList.Add(currentVersor);

            return versorFactorization;
        }
    }
}
