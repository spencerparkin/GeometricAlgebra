using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace GeometricAlgebra
{
    public class MathException : Exception
    {
        public MathException(string error) : base(error)
        {
        }
    }

    // This is the base class for all expression tree nodes.
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

        public abstract string Print(Format format, Context context = null);

        // Derivatives overriding this virtual method are to return null
        // if no algebraic manipulation of the sub-tree rooted at this object
        // is performed.  On the other hand, if such a manipulation is performed,
        // then the new or existing root should be returned.
        public virtual Operand EvaluationStep(Context context)
        {
            return null;
        }

        public static Operand ExhaustEvaluation(Operand operand, Context context)
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

        public static HashSet<int> DiscoverGrades(Operand operand, Context context)
        {
            List<string> basisVectorList = context.ReturnBasisVectors();
            HashSet<int> gradeSet = new HashSet<int>();

            for (int i = 0; i <= basisVectorList.Count; i++)
            {
                Operand gradePart = new GradePart(new List<Operand>() { operand.Copy(), new NumericScalar(i) });
                gradePart = ExhaustEvaluation(gradePart, context);
                if (!gradePart.IsAdditiveIdentity)
                    gradeSet.Add(i);
            }

            return gradeSet;
        }

        public static (Operand input, Operand output, string error) Evaluate(string expression, Context context)
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
            catch (Exception exc)
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
        public virtual Operand Inverse(Context context)
        {
            return null;
        }

        public virtual Operand Reverse()
        {
            return null;
        }
    }
}