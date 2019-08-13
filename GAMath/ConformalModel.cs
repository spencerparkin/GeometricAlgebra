using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GeometricAlgebra.ConformalModel
{
    public class Conformal3D_Context : Context
    {
        public Conformal3D_Context() : base()
        {
            funcList.Add(new Identify());
        }

        public override void GenerateDefaultStorage()
        {
            base.GenerateDefaultStorage();

            operandStorage.SetStorage("i", Operand.Evaluate("e1^e2^e3", this).output);
            operandStorage.SetStorage("I", Operand.Evaluate("e1^e2^e3^ni^no", this).output);

            // Add formulas for the geometric primitives of the conformal model in 3D space.
            Operand.Evaluate("@point := weight * (no + @center + 0.5 * (@center . @center) * ni)", this);
            Operand.Evaluate("@sphere := @weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni)", this);
            Operand.Evaluate("@isphere := @weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni)", this);
            Operand.Evaluate("@circle := @weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this);
            Operand.Evaluate("@icircle := @weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this);
            Operand.Evaluate("@pointpair := @weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this);
            Operand.Evaluate("@ipointpair := @weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this);
            Operand.Evaluate("@plane := @weight * (@normal + (@center . @normal) * ni)", this);
            Operand.Evaluate("@line := @weight * (@normal + (@center ^ @normal) * ni) * @i", this);
            Operand.Evaluate("@flatpoint := @weight * (1 - @center ^ ni) * @i", this);
            Operand.Evaluate("@tangentpoint := @weight * (no + @center + 0.5 * (@center . @center) * ni) ^ (@normal + (@center . @normal) * ni)", this);
        }

        public override string TranslateVectorNameForLatex(string vectorName)
        {
            if(vectorName == "no")
                return @"\vec{o}";
            else if(vectorName == "ni")
                return @"\vec{\infty}";

            return base.TranslateVectorNameForLatex(vectorName);
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

            // All symbolic vectors are assumed to be an unknown
            // linear combination of e1, e2 and e3.
            if (vectorNameA == "no" || vectorNameA == "ni")
                if (vectorNameB != "no" && vectorNameB != "ni")
                    return new NumericScalar(0.0);
            if (vectorNameB == "no" || vectorNameB == "ni")
                if (vectorNameA != "no" && vectorNameA != "ni")
                    return new NumericScalar(0.0);

            return base.BilinearForm(vectorNameA, vectorNameB);
        }

        public override bool IsLinearlyDependentSet(List<string> vectorNameList)
        {
            if (base.IsLinearlyDependentSet(vectorNameList))
                return true;

            // Since we're treating all symbolic vectors as being taken from
            // the euclidean sub-space of our geometric algebra, we can identify
            // the following case.
            if(vectorNameList.Any(vectorName => vectorName == "e1"))
            {
                if(vectorNameList.Any(vectorName => vectorName == "e2"))
                {
                    if(vectorNameList.Any(vectorName => vectorName == "e3"))
                    {
                        List<string> basisList = new List<string>() { "e1", "e2", "e3", "ni", "no" };
                        if (vectorNameList.Any(vectorName => !basisList.Any(basisName => basisName == vectorName)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override List<string> ReturnBasisVectors()
        {
            return new List<string>() { "e1", "e2", "e3", "no", "ni" };
        }
    }

    public class Identify : Function
    {
        public Identify() : base()
        {
        }

        public override Operand New()
        {
            return new Identify();
        }

        public override string Name(Format format)
        {
            if (format == Format.LATEX)
                return @"\mbox{id}";
            return "id";
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new MathException(string.Format($"Expected exactly 1 argument, got {operandList.Count}."));

            operand = operandList[0];
            int grade = operand.Grade;
            if (grade == -1)
                throw new MathException("Cannot identify an element non-homogenous in terms of grade; specifically, only blades are identified.");

            context.operandStorage.SetStorage("__blade__", operand);

            Operand center = null;
            Operand normal = null;
            Operand radius = null;
            Operand weight = null;

            switch (grade)
            {
                case 0:
                    if (operand.IsAdditiveIdentity)
                    {
                        context.Log("The zero set of the blade consumes all of space.");
                    }
                    else
                    {
                        context.Log("The zero set of the blade is empty.");
                    }
                    break;
                case 1:
                    Operand.Evaluate("@weight = -@__blade__ . ni", context);
                    context.operandStorage.GetStorage("weight", ref weight);
                    if (weight.IsAdditiveIdentity)
                    {
                        Operand.Evaluate("@normal = no . @__blade__ ^ ni", context);
                        weight = Operand.Evaluate("@weight = pow(@normal . @normal, 0.5)", context).output;
                        normal = Operand.Evaluate("@normal = @normal / @weight", context).output;
                        center = Operand.Evaluate("@center = -(no . @__blade__ / @weight) * @normal", context).output;
                        context.Log("The blade represents a plane.");
                    }
                    else
                    {
                        center = Operand.Evaluate("@center = (no ^ ni . @__blade__ ^ no ^ ni) / @weight", context).output;
                        Operand radiusSquared = Operand.Evaluate("@__square_radius__ = grade(@center . @center + 2 * no . @__blade__ / @weight, 0)", context).output;
                        if(radiusSquared is NumericScalar scalar)
                        {
                            if(scalar.value >= 0.0)
                            {
                                radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5)", context).output;
                                if (scalar.value == 0.0)
                                    context.Log("The blade represents a point.");
                                else
                                    context.Log("The blade represents a sphere.");
                            }
                            else
                            {
                                context.Log("The blade represents an imaginary sphere.");
                                radius = Operand.Evaluate("@radius = pow(-@__square_radius__, 0.5)", context).output;
                            }
                        }
                        else
                        {
                            context.Log("The blade represents a sphere.");
                            radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5)", context).output;
                        }
                    }
                    break;
                case 2:
                    normal = Operand.Evaluate("no ^ ni . @__blade__ ^ ni", context).output;
                    if(normal.IsAdditiveIdentity)
                    {

                    }
                    else
                    {
                        weight = Operand.Evaluate("@weight = pow(@normal . @normal, 0.5)", context).output;
                        normal = Operand.Evaluate("@normal = @normal / @weight", context).output;
                        center = Operand.Evaluate("@center = -@normal * (no ^ ni . @__blade__ ^ no * ni) / @weight", context).output;
                        Operand radiusSquared = Operand.Evaluate("@__square_radius__ = grade(@center . @center + 2 * ((no ^ ni . no ^ @__blade__) / @weight + (@center . @normal) * @normal, 0)", context).output;
                        if(radiusSquared is NumericScalar scalar)
                        {
                            if (scalar.value >= 0.0)
                            {
                                radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5", context).output;
                                if (scalar.value == 0.0)
                                    context.Log("The blade represents a tangent point.");
                                else
                                    context.Log("The blade represents a circle.");
                            }
                            else
                            {
                                context.Log("The blade represents an imaginary circle.");
                                radius = Operand.Evaluate("@radius = pow(-@__square_radius__, 0.5", context).output;
                            }
                        }
                        else
                        {
                            context.Log("The blade represents a circle.");
                            radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5", context).output;
                        }
                    }
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
            }

            if (weight != null)
                context.Log("Weight: " + weight.Print(Format.PARSEABLE));
            if (center != null)
                context.Log("Center: " + center.Print(Format.PARSEABLE));
            if (normal != null)
                context.Log("Normal: " + normal.Print(Format.PARSEABLE));
            if (radius != null)
                context.Log("Radius: " + radius.Print(Format.PARSEABLE));

            return null;
        }
    }
}