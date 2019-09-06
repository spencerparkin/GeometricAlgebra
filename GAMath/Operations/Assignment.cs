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

        public Assignment() : base()
        {
            this.storeEvaluation = true;
        }

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

            Variable storageVariable = operandList[0] as Variable;
            if (storageVariable == null)
                throw new MathException("Assignment operation expects an l-value of type variable.");

            if(!storeEvaluation)
            {
                List<Operand> operandQueue = new List<Operand>();
                operandList[1].CollectAllOperands(operandQueue);

                while(operandQueue.Count > 0)
                {
                    Operand operand = operandQueue[0];
                    operandQueue.RemoveAt(0);

                    if(operand is Variable variable)
                    {
                        if(variable.name == storageVariable.name)
                            throw new MathException("Cannot assign recursive formula to variable.");
                        else if(context.operandStorage.GetStorage(variable.name, ref operand))
                            operand.CollectAllOperands(operandQueue);
                    }
                }
            }
            else
            {
                Operand operand = operandList[1].EvaluationStep(context);
                if(operand != null)
                {
                    operandList[1] = operand;
                    return this;
                }
            }

            context.operandStorage.SetStorage(storageVariable.name, operandList[1].Copy());

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