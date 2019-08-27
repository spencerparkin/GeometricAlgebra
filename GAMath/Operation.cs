using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public abstract class Operation : Operand
    {
        public List<Operand> operandList;

        public Operation() : base()
        {
            operandList = new List<Operand>();
        }

        public Operation(List<Operand> operandList)
        {
            this.operandList = operandList;
        }

        public override Operand Copy()
        {
            Operation clone = (Operation)base.Copy();
            clone.operandList = (from operand in this.operandList select operand.Copy()).ToList();
            return clone;
        }

        public override void CollectAllOperands(List<Operand> operandList)
        {
            base.CollectAllOperands(operandList);
            foreach(Operand operand in this.operandList)
                operand.CollectAllOperands(operandList);
        }

        public abstract bool IsAssociative();
        public abstract bool IsDistributiveOver(Operation operation);

        // This is used by infix operators.
        public virtual string PrintJoiner(Format format)
        {
            return "?";
        }

        public override string Print(Format format, Context context)
        {
            List<string> printList = new List<string>();

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand operand = operandList[i];
                string subPrint = operand.Print(format, context);
                if (operand is Operation)
                {
                    if (format == Format.PARSEABLE)
                        subPrint = "(" + subPrint + ")";
                    else if (format == Format.LATEX)
                        subPrint = @"\left(" + subPrint + @"\right)";
                }

                printList.Add(subPrint);
            }

            return string.Join(PrintJoiner(format), printList);
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count == 1 && (this is Sum || this is Product))
                return operandList[0];

            for (int i = 0; i < operandList.Count; i++)
            {
                Operation operation = operandList[i] as Operation;
                if (operation == null)
                    continue;

                // Apply the associative property.
                if ((this.freezeFlags & FreezeFlag.ASSOCIATION) == 0 && operation.GetType() == this.GetType() && operation.IsAssociative())
                {
                    operandList = operandList.Take(i).Concat(operation.operandList).Concat(operandList.Skip(i + 1).Take(operandList.Count - i - 1)).ToList();
                    return this;
                }

                // Apply the distributive property.
                if ((this.freezeFlags & FreezeFlag.DISTRIBUTION) == 0 && this.IsDistributiveOver(operation))
                {
                    Operation newOperationA = (Operation)Activator.CreateInstance(operation.GetType());

                    for (int j = 0; j < operation.operandList.Count; j++)
                    {
                        Operation newOperationB = (Operation)Activator.CreateInstance(this.GetType());

                        newOperationB.operandList = (from operand in operandList.Take(i) select operand.Copy()).ToList();
                        newOperationB.operandList.Add(operation.operandList[j]);
                        newOperationB.operandList = newOperationB.operandList.Concat(from operand in operandList.Skip(i + 1).Take(operandList.Count - i - 1) select operand.Copy()).ToList();

                        newOperationA.operandList.Add(newOperationB);
                    }

                    return newOperationA;
                }
            }

            for (int i = 0; i < operandList.Count; i++)
            {
                Operand oldOperand = operandList[i];

                // This if-statement is purely an optimization, and if it is suspected
                // that it is causing any problems, it can be commented out.
                if((oldOperand.freezeFlags & FreezeFlag.SUB_EVAL) != 0)
                    continue;

                Operand newOperand = oldOperand.EvaluationStep(context);
                if (newOperand == null)
                    oldOperand.freezeFlags |= FreezeFlag.SUB_EVAL;
                else
                {
                    operandList[i] = newOperand;
                    return this;
                }
            }

            return null;
        }

        public override string LexicographicSortKey()
        {
            return string.Join("", from operand in operandList select operand.LexicographicSortKey());
        }
    }
}