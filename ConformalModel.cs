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

            // Add formulas for the geometric primitives of the conformal model in 3D space.
            operandStorage.Add("point", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center) * ni)", this));
            operandStorage.Add("sphere", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni)", this));
            operandStorage.Add("isphere", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni)", this));
            operandStorage.Add("circle", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this));
            operandStorage.Add("icircle", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this));
            operandStorage.Add("pointpair", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this));
            operandStorage.Add("ipointpair", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this));
            operandStorage.Add("plane", Operand.FullyEvaluate("@weight * (@normal + (@center . @normal) * ni)", this));
            operandStorage.Add("line", Operand.FullyEvaluate("@weight * (@normal + (@center ^ @normal) * ni) * @i", this));
            operandStorage.Add("flatpoint", Operand.FullyEvaluate("@weight * (1 - @center ^ ni) * @i", this));
            operandStorage.Add("tangentpoint", Operand.FullyEvaluate("@weight * (no + @center + 0.5 * (@center . @center) * ni) ^ (@normal + (@center . @normal) * ni)", this));

            //funcList.Add(new Decompose());    // TODO: Write a function that can identify and decompose elements of the conformal model.
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