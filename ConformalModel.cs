using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra.ConformalModel
{
    public class Conformal3D_EvaluationContext : EvaluationContext
    {
        public Conformal3D_EvaluationContext() : base()
        {
            operandStorage.Add("i", Operand.FullyEvaluate("e1^e2^e3", this));
            operandStorage.Add("I", Operand.FullyEvaluate("e1^e2^e3^no^ni", this));

            /*funcList.Add(new EuclidVector());
            funcList.Add(new Point());
            funcList.Add(new Sphere());
            funcList.Add(new Sphere(true));
            funcList.Add(new Plane());
            funcList.Add(new Line());
            funcList.Add(new Circle());
            funcList.Add(new Circle(true));
            funcList.Add(new PointPair());
            funcList.Add(new PointPair(true));
            funcList.Add(new FlatPoint());
            funcList.Add(new TangentPoint());
            funcList.Add(new Decompose());*/
        }

        public override Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            if (vectorNameA == "e1")
            {
                if (vectorNameB == "e1")
                    return new NumericScalar(1.0);
                else if (vectorNameB == "e2")
                    return new NumericScalar(0.0);
                else if (vectorNameB == "e3")
                    return new NumericScalar(0.0);
            }
            else if (vectorNameA == "e2")
            {
                if (vectorNameB == "e1")
                    return new NumericScalar(0.0);
                else if (vectorNameB == "e2")
                    return new NumericScalar(1.0);
                else if (vectorNameB == "e3")
                    return new NumericScalar(0.0);
            }
            else if (vectorNameA == "e3")
            {
                if (vectorNameB == "e1")
                    return new NumericScalar(0.0);
                else if (vectorNameB == "e2")
                    return new NumericScalar(0.0);
                else if (vectorNameB == "e3")
                    return new NumericScalar(1.0);
            }

            if (vectorNameA == "e1" || vectorNameA == "e2" || vectorNameA == "e3")
            {
                if (vectorNameB == "no" || vectorNameB == "ni")
                    return new NumericScalar(0.0);
            }

            if (vectorNameA == "no" || vectorNameA == "ni")
            {
                if (vectorNameB == "e1" || vectorNameB == "e2" || vectorNameB == "e3")
                    return new NumericScalar(0.0);

                if (vectorNameB == "no" || vectorNameB == "ni")
                {
                    if (vectorNameA == vectorNameB)
                        return new NumericScalar(0.0);
                    else
                        return new NumericScalar(-1.0);
                }
            }

            return base.BilinearForm(vectorNameA, vectorNameB);
        }
    }
}