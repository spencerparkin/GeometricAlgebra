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

    public struct Result
    {
        public Operand input;
        public Operand output;
        public string error;
    }

    // This is the base class for all expression tree nodes.
    public abstract class Operand
    {
        [FlagsAttribute]
        public enum FreezeFlag
        {
            ALL             = 0x00000001,
            DISTRIBUTION    = 0x00000002,
            ASSOCIATION     = 0x00000004
        }

        public FreezeFlag freezeFlags;

        public Operand()
        {
            freezeFlags = 0;
        }

        public abstract Operand New();

        public virtual Operand Copy()
        {
            Operand operand = New();
            operand.freezeFlags = this.freezeFlags;
            return operand;
        }

        public virtual void CollectAllOperands(List<Operand> operandList)
        {
            operandList.Add(this);
        }

        public virtual int Grade { get { return -1; } }
        public virtual bool IsAdditiveIdentity { get { return false; } }
        public virtual bool IsMultiplicativeIdentity { get { return false; } }  // This is with respect to the geometric product.

        public enum Format
        {
            PARSEABLE,
            LATEX
        }

        public abstract string Print(Format format, Context context = null);

        public string debug
        {
            get
            {
                return Print(Format.PARSEABLE);
            }
        }

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
            double startTimeMilliseconds = (DateTime.Now - DateTime.MinValue).TotalMilliseconds;
            while (true)
            {
                Operand newOperand = operand.EvaluationStep(context);
                if (newOperand != null)
                    operand = newOperand;
                else
                    break;

                double currentTimeMilliseconds = (DateTime.Now - DateTime.MinValue).TotalMilliseconds;
                double elapsedTimeMilliseconds = currentTimeMilliseconds - startTimeMilliseconds;
                if(elapsedTimeMilliseconds >= context.evaluationTimeoutMilliseconds)
                    throw new MathException($"Evaluation loop timed-out (time-out was {context.evaluationTimeoutMilliseconds} milliseconds.)");
            }

            return operand;
        }

        public static void ThawTree(Operand root)
        {
            List<Operand> operandList = new List<Operand>();
            root.CollectAllOperands(operandList);
            foreach (Operand operand in operandList)
                operand.freezeFlags = 0;
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

        public static Result Evaluate(string expression, Context context)
        {
            Result result = new Result();
            result.input = null;
            result.output = null;
            result.error = "";

            try
            {
                Parser parser = new Parser(context, false);
                result.input = parser.Parse(expression);
                result.output = ExhaustEvaluation(result.input.Copy(), context);

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
                    HashSet<int> gradeSetA = DiscoverGrades(result.output, context);

                    parser = new Parser(context, true);
                    Operand basisResult = parser.Parse(expression);
                    basisResult = ExhaustEvaluation(basisResult, context);
                
                    HashSet<int> gradeSetB = DiscoverGrades(basisResult, context);

                    if(gradeSetB.IsProperSubsetOf(gradeSetA))
                    {
                        result.output = new GradePart(new List<Operand>() { result.output }.Concat(from i in gradeSetB select new NumericScalar(i)).ToList());
                        result.output = ExhaustEvaluation(result.output, context);
                    }
                }
#endif
            }
            catch (Exception exc)
            {
                result.error = exc.Message;
            }

            return result;
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

        public static IEnumerable<List<string>> GenerateVectorCombinations(List<string> vectorSampleList, List<string> vectorList, int depth, int maxDepth, int j)
        {
            if (depth < maxDepth)
            {
                for (int i = j; i < vectorSampleList.Count; i++)
                {
                    vectorList.Add(vectorSampleList[i]);

                    foreach (List<string> vectorComboList in GenerateVectorCombinations(vectorSampleList, vectorList, depth + 1, maxDepth, i + 1))
                        yield return vectorComboList;

                    vectorList.RemoveAt(vectorList.Count - 1);
                }
            }
            else
            {
                List<string> vectorComboList = vectorList.ToList();
                yield return vectorComboList;
            }
        }

        public static IEnumerable<Blade> GenerateBasisBlades(List<string> basisVectorList)
        {
            for (int i = 0; i <= basisVectorList.Count; i++)
            {
                List<string> vectorList = new List<string>();
                foreach (List<string> vectorComboList in GenerateVectorCombinations(basisVectorList, vectorList, 0, i, 0))
                {
                    vectorComboList.Sort();
                    Blade basisBlade = new Blade(new NumericScalar(1.0), vectorComboList);
                    yield return basisBlade;
                }
            }
        }

        public static Sum CanonicalizeMultivector(Operand operand)
        {
            Sum sum = operand as Sum;
            if(sum == null)
                sum = new Sum(new List<Operand>() { operand });

            for(int i = 0; i < sum.operandList.Count; i++)
            {
                Operand term = sum.operandList[i];
                if(term is Blade)
                    continue;
                else if(term.Grade == 0)
                    sum.operandList[i] = new Blade(term);
                else
                    throw new MathException("Element in given form cannot be put in canonical form.");
            }

            return sum;
        }
    }
}