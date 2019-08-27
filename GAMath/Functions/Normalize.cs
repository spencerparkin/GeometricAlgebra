using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace GeometricAlgebra
{
    public class Normalize : Function
    {
        public Normalize() : base()
        {
        }

        public Normalize(List<Operand> operandList) : base(operandList)
        {
        }

        public override Operand New()
        {
            return new Normalize();
        }

        public override string Name(Format format)
        {
            if(format == Format.LATEX)
                return @"\mbox{normalize}";

            return "normalize";
        }

        public override string ShortDescription => "Calculate a scalar-multiple of the given multivector such that it has unit magnitude.";

        public override Operand EvaluationStep(Context context)
        {
            if(operandList.Count != 1)
                throw new MathException($"Normalize function expected exactly 1 operand; got {operandList.Count}.");

            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            operand = operandList[0];

            return new GeometricProduct(new List<Operand>() { operand.Copy(), new Inverse(new List<Operand>() { new Magnitude(new List<Operand>() { operand.Copy() }) }) });
        }
    }
}
