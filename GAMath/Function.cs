using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public abstract class Function : Operation
    {
        public Function() : base()
        {
        }

        public Function(List<Operand> operandList) : base(operandList)
        {
        }

        public abstract string Name(Format format);

        public virtual string DetailedHelp
        {
            get { return "No help."; }
        }

        public virtual string ShortDescription
        {
            get { return "No description."; }
        }

        public override bool IsDistributiveOver(Operation operation)
        {
            return false;
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override string Print(Format format, Context context)
        {
            string name = Name(format);
            string args = string.Join(", ", from operand in operandList select operand.Print(format, context));
            if (format == Format.PARSEABLE)
                return name + "(" + args + ")";
            else if (format == Format.LATEX)
                return name + @"\left(" + args + @"\right)";
            return "?";
        }
    }
}