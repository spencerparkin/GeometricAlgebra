﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class Meet : Function
    {
        public Meet() : base()
        {
        }

        public Meet(List<Operand> operandList) : base(operandList)
        {
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{meet}";

            return "meet";
        }

        public override Operand New()
        {
            return new Meet();
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count <= 0)
                throw new MathException(string.Format("Meet operation expects one or more operands, got {0}.", operandList.Count));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            return CalculateMeet(this.operandList, context);
        }

        public static Operand CalculateMeet(List<Operand> operandList, Context context)
        {
            Operand psuedoScalar = context.MakePsuedoScalar();
            Operand join = new Join(operandList.Select(operand => (Operand)new InnerProduct(new List<Operand>() { operand.Copy(), psuedoScalar.Copy() })).ToList());
            Operand meet = ExhaustEvaluation(new InnerProduct(new List<Operand>() { join, psuedoScalar }), context);
            return meet;
        }
    }
}