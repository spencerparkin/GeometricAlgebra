using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class NumericScalar : Operand
    {
        public double value;

        public override int Grade
        {
            get
            {
                return 0;
            }
        }

        public override bool IsAdditiveIdentity
        {
            get
            {
                return this.value == 0.0;
            }
        }

        public override bool IsMultiplicativeIdentity
        {
            get
            {
                return this.value == 1.0;
            }
        }

        public NumericScalar(double value = 0.0) : base()
        {
            this.value = value;
        }

        public override Operand Copy()
        {
            return new NumericScalar(this.value);
        }

        public override Operand New()
        {
            return new NumericScalar();
        }

        public override Operand EvaluationStep(Context context)
        {
            return null;
        }

        public override string Print(Format format, Context context)
        {
            switch (format)
            {
                case Format.LATEX:
                    return string.Format("{0:F2}", this.value);
                case Format.PARSEABLE:
                    return string.Format("{0}", this.value);
            }

            return "?";
        }

        public override Operand Inverse(Context context)
        {
            try
            {
                return new NumericScalar(1.0 / this.value);
            }
            catch (DivideByZeroException)
            {
                throw new MathException(string.Format("Attempted to invert scalar ({0}), but got divid-by-zero exception.", this.value));
            }
        }

        public override Operand Reverse()
        {
            return this;
        }
    }
}