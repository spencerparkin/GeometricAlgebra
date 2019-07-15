using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Inverse : Function
    {
        public Inverse() : base()
        {
        }

        public Inverse(List<Operand> operandList)
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
            return new Inverse();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{inverse}";

            return "inverse";
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException(string.Format("Inverse operation expects exactly one operand, got {0}.", operandList.Count));

            if (operandList[0].IsAdditiveIdentity)
                throw new MathException("Cannot invert the additive identity.");

            if (operandList[0] is GeometricProduct oldGeometricProduct)
            {
                GeometricProduct newGeometricProduct = new GeometricProduct();

                for (int i = oldGeometricProduct.operandList.Count - 1; i >= 0; i--)
                {
                    newGeometricProduct.operandList.Add(new Inverse(new List<Operand>() { oldGeometricProduct.operandList[i] }));
                }

                return newGeometricProduct;
            }

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            Operand inverse = operandList[0].Inverse(context);
            return inverse;
        }

        public override string Print(Format format, Context context)
        {
            if (operandList.Count != 1)
                return "?";

            switch (format)
            {
                case Format.LATEX:
                    return @"\left(" + operandList[0].Print(format, context) + @"\right)^{-1}";
                case Format.PARSEABLE:
                    return base.Print(format, context);
            }

            return "?";
        }
    }
}