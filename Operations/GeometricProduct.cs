using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class GeometricProduct : Product
    {
        public GeometricProduct() : base()
        {
        }

        public GeometricProduct(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override Operand New()
        {
            return new GeometricProduct();
        }

        public override bool IsAssociative()
        {
            return true;
        }

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return @"\times";
                case Format.PARSEABLE:
                    return "*";
            }

            return "?";
        }

        public override int Grade
        {
            get
            {
                if (operandList.All(operand => operand.Grade == 0))
                    return 0;

                return -1;
            }
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            // To avoid infinite evaluation looping, we must apply...
            //   1) vB = v.B + v^B, and
            //   2) v^B = vB - v.B,
            // ...according to rules that dictate when and where they're appropriate.
            // Also to avoid infinite looping, the distributive property must take
            // precedence over anything we do here.

            // All reduction cases must be eliminated before it is safe to handle the expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA != null && bladeB != null && bladeA.Grade > 1 && bladeB.Grade > 1)
                {
                    // TODO: This would be a great place to provide a cache-based optimization.  I don't have
                    //       a proof (I should look for one), but I believe the geometric product of any two
                    //       blades will be a blade.  What we can do here is look in a cache for the answer
                    //       to the product.  On cache miss, do the work as usual, then cache the answer for
                    //       future use.  This could speed up the inverse calculation considerably.

                    // Here our choice of which blade to reduce is arbitrary from a stand-point of correctness.
                    // However, we might converge faster by choosing the blade with smaller grade.
                    // Note there is also something arbitrary about how we're reducing the blades.
                    int j = bladeA.Grade <= bladeB.Grade ? i : i + 1;
                    Blade blade = operandList[j] as Blade;
                    Blade subBlade = blade.MakeSubBlade(0);
                    Blade vector = new Blade(blade.vectorList[0]);
                    GeometricProduct geometricProduct = new GeometricProduct(new List<Operand>() { vector, subBlade });
                    InnerProduct innerProduct = new InnerProduct(new List<Operand>() { vector.Copy(), subBlade.Copy() });
                    operandList[j] = new Sum(new List<Operand>() { geometricProduct, new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), innerProduct }) });
                    return this;
                }
            }

            // All reduction cases eliminated, it is now safe to handle some expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA == null || bladeB == null)
                    continue;

                if ((bladeA.Grade == 1 && bladeB.Grade > 1) || (bladeA.Grade > 1 && bladeB.Grade == 1))
                {
                    InnerProduct innerProduct = new InnerProduct(new List<Operand>() { bladeA, bladeB });
                    OuterProduct outerProduct = new OuterProduct(new List<Operand>() { bladeA.Copy(), bladeB.Copy() });
                    operandList[i] = new Sum(new List<Operand>() { innerProduct, outerProduct });
                    operandList.RemoveAt(i + 1);
                    return this;
                }
            }

            // It is now safe to handle the remaining expansion cases.
            for (int i = 0; i < operandList.Count - 1; i++)
            {
                Blade bladeA = operandList[i] as Blade;
                Blade bladeB = operandList[i + 1] as Blade;

                if (bladeA == null || bladeB == null)
                    continue;

                if (bladeA.Grade == 1 && bladeB.Grade == 1)
                {
                    operandList.RemoveAt(i + 1);
                    GeometricProduct innerProduct = new GeometricProduct(new List<Operand>() { bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[0]) });
                    Blade outerProduct = new Blade(new GeometricProduct(new List<Operand>() { bladeA.scalar.Copy(), bladeB.scalar.Copy() }));
                    outerProduct.vectorList.Add(bladeA.vectorList[0]);
                    outerProduct.vectorList.Add(bladeB.vectorList[0]);
                    operandList[i] = new Sum(new List<Operand>() { innerProduct, outerProduct });
                    return this;
                }
            }

            return null;
        }
    }
}