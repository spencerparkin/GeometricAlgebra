using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace GeometricAlgebra
{
    public interface ITranslator
    {
        Operand Translate(Operand operand, Context context);
    }

    public abstract class Collectable : Operand
    {
        public Operand scalar;

        public abstract bool IsLike(Collectable collectable);
        public abstract bool CanAbsorb(Operand operand);
        public abstract Operand Explode(ITranslator translator, Context context);

        public Collectable()
        {
            scalar = null;
        }

        public override Operand Copy()
        {
            Collectable collectable = base.Copy() as Collectable;
            collectable.scalar = this.scalar.Copy();
            return collectable;
        }

        public override void CollectAllOperands(List<Operand> operandList)
        {
            base.CollectAllOperands(operandList);
            scalar.CollectAllOperands(operandList);
        }

        public override Operand EvaluationStep(Context context)
        {
            if (scalar != null)
            {
                if (scalar.IsAdditiveIdentity)
                    return scalar;

                Operand newScalar = scalar.EvaluationStep(context);
                if (newScalar != null)
                {
                    this.scalar = newScalar;
                    return this;
                }
            }

            return null;
        }
    }
}