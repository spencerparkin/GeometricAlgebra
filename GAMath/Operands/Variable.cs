using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Variable : Operand
    {
        public string name;

        public Variable(string name = "") : base()
        {
            this.name = name;
        }

        public override Operand Copy()
        {
            return new Variable(this.name);
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = null;
            if (context.LookupVariableByName(this.name, ref operand))
            {
                ThawTree(operand);
                return operand;
            }

            return null;
        }

        public override string Print(Format format, Context context)
        {
            switch (format)
            {
                case Format.LATEX:
                {
                    return context.TranslateVariableNameForLatex(this.name);
                }
                case Format.PARSEABLE:
                {
                    return "@" + this.name;
                }
            }

            return "?";
        }

        public override string LexicographicSortKey()
        {
            return this.name;
        }
    }
}