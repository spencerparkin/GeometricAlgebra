using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
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

        public override string ShortDescription
        {
            get { return "Select the parts of the given multivector of the given grade."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Select the parts of the given multivector of the given grade.");
            context.Log("Give the multivector first, then the desired grade.");
            context.Log("This function is the identity for multivectors homogeneous of a certain grade.");
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{grade}";

            return "grade";
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count < 2)
                throw new MathException(string.Format("Grade-part operation expects two or more arguments, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            int grade = operandList[0].Grade;
            if (grade == -1)
                throw new MathException("Could not determine grade of operand.");
            
            var gradeSet = new HashSet<int>();
            for (int i = 1; i < operandList.Count; i++)
            {
                NumericScalar scalar = operandList[i] as NumericScalar;
                if (scalar == null)
                {
                    Operand gradeOperand = operandList[i].EvaluationStep(context);
                    if (gradeOperand != null)
                    {
                        operandList[i] = gradeOperand;
                        return this;
                    }

                    throw new MathException("Encountered non-numeric-scalar when looking for grade arguments.");
                }

                gradeSet.Add((int)scalar.value);
            }

            if (gradeSet.Contains(grade))
                return operandList[0];

            return new NumericScalar(0.0);
        }

        public override string Print(Format format, Context context)
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
}