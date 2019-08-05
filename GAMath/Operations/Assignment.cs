using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
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

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 2)
                throw new MathException(string.Format("Assignment operation expects exactly two operands, got {0}.", operandList.Count));

            Variable variable = operandList[0] as Variable;
            if (variable == null)
                throw new MathException("Assignment operation expects an l-value of type variable.");

            // This is important so that our l-value doesn't evaluate as something other than a variable before we have a chance to assign it.
            context.operandStorage.ClearStorage(variable.name);

            // Sometimes we want a dependency-chain; sometimes we don't.
            if (storeEvaluation)
            {
                Operand operand = base.EvaluationStep(context);
                if (operand != null)
                    return operand;
            }

            // Perform the assignment.
            context.operandStorage.SetStorage(variable.name, operandList[1].Copy());

            return operandList[1];
        }

        public override string Print(Format format, Context context)
        {
            if (operandList.Count == 2)
                return string.Format("{0} = {1}", operandList[0].Print(format, context), operandList[1].Print(format, context));

            return "?";
        }
    }
}