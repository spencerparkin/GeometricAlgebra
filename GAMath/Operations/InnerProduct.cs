using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public class InnerProduct : Product
    {
        public InnerProduct() : base()
        {
        }

        public InnerProduct(List<Operand> operandList)
            : base(operandList)
        {
        }

        public override bool IsAssociative()
        {
            return false;
        }

        public override string PrintJoiner(Format format)
        {
            switch (format)
            {
                case Format.LATEX:
                    return @"\cdot";
                case Format.PARSEABLE:
                    return ".";
            }

            return "?";
        }

        public override int Grade
        {
            get
            {
                if (operandList.Count == 2)
                    if (operandList[0].Grade >= 0 && operandList[1].Grade >= 0)
                        return Math.Abs(operandList[0].Grade - operandList[1].Grade);

                return -1;
            }
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count == 2)
            {
                Blade bladeA = operandList[0] as Blade;
                Blade bladeB = operandList[1] as Blade;

                if (bladeA != null && bladeB != null)
                {
                    if (bladeA.Grade == 1 && bladeB.Grade == 1)
                    {
                        return new GeometricProduct(new List<Operand>() { bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[0]) });
                    }
                    else if (bladeA.Grade == 1 && bladeB.Grade > 1)
                    {
                        Sum sum = new Sum();

                        for (int i = 0; i < bladeB.vectorList.Count; i++)
                        {
                            Blade subBlade = bladeB.MakeSubBlade(i);
                            subBlade.scalar = new GeometricProduct(new List<Operand>() { new NumericScalar(i % 2 == 1 ? -1.0 : 1.0), bladeA.scalar, bladeB.scalar, context.BilinearForm(bladeA.vectorList[0], bladeB.vectorList[i]) });
                            sum.operandList.Add(subBlade);
                        }

                        return sum;
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade == 1)
                    {
                        operandList[0] = bladeB;
                        operandList[1] = bladeA;

                        if (bladeA.Grade % 2 == 0)
                            operandList.Add(new NumericScalar(-1.0));

                        return this;
                    }
                    else if (bladeA.Grade > 1 && bladeB.Grade > 1)
                    {
                        if (context.useOperandCache)
                        {
                            InnerProduct innerProduct = new InnerProduct(new List<Operand>() { new Blade(new NumericScalar(1.0), bladeA.vectorList.ToList()), new Blade(new NumericScalar(1.0), bladeB.vectorList.ToList()) });
                            string key = innerProduct.Print(Format.PARSEABLE, context);
                            Operand cachedResult = null;
                            if (!context.operandCache.GetStorage(key, ref cachedResult))
                            {
                                context.useOperandCache = false;
                                cachedResult = Operand.ExhaustEvaluation(innerProduct, context);
                                context.useOperandCache = true;
                                context.operandCache.SetStorage(key, cachedResult);
                            }

                            return new GeometricProduct(new List<Operand>() { bladeA.scalar, bladeB.scalar, cachedResult });
                        }

                        if (bladeA.Grade <= bladeB.Grade)
                        {
                            return new InnerProduct(new List<Operand>() { bladeA.MakeSubBlade(bladeA.Grade - 1), new InnerProduct(new List<Operand>() { new Blade(bladeA.vectorList[bladeA.Grade - 1]), bladeB }) });
                        }
                        else
                        {
                            return new InnerProduct(new List<Operand>() { new InnerProduct(new List<Operand>() { bladeA, new Blade(bladeB.vectorList[0]) }), bladeB.MakeSubBlade(0) });
                        }
                    }
                }
            }

            return null;
        }
    }
}