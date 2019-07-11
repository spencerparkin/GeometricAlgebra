﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeometricAlgebra.ConformalModel
{
    public class Conformal3D_EvaluationContext : EvaluationContext
    {
        public Conformal3D_EvaluationContext() : base()
        {
            SetStorage("i", Operand.Evaluate("e1^e2^e3", this));
            SetStorage("I", Operand.Evaluate("e1^e2^e3^no^ni", this));

            // Add formulas for the geometric primitives of the conformal model in 3D space.
            SetStorage("point", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center) * ni)", this));
            SetStorage("sphere", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni)", this));
            SetStorage("isphere", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni)", this));
            SetStorage("circle", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this));
            SetStorage("icircle", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center . @normal) * ni)", this));
            SetStorage("pointpair", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center - @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this));
            SetStorage("ipointpair", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center + @radius * @radius) * ni) ^ (@normal + (@center ^ @normal) * ni) * @i", this));
            SetStorage("plane", Operand.Evaluate("@weight * (@normal + (@center . @normal) * ni)", this));
            SetStorage("line", Operand.Evaluate("@weight * (@normal + (@center ^ @normal) * ni) * @i", this));
            SetStorage("flatpoint", Operand.Evaluate("@weight * (1 - @center ^ ni) * @i", this));
            SetStorage("tangentpoint", Operand.Evaluate("@weight * (no + @center + 0.5 * (@center . @center) * ni) ^ (@normal + (@center . @normal) * ni)", this));

            funcList.Add(new Identify());
        }

        public override string TranslateVectorNameForLatex(string vectorName)
        {
            if(vectorName == "no")
                return @"\vec{o}";
            else if(vectorName == "ni")
                return @"\rev{\infty}";

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

            // For now, I'm going to treat all symbolic vectors as being
            // taken from the euclidean sub-space of the conformal space.
            // That is, until I see the limitation of doing so.
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
            List<string> basisList = new List<string>() { "e1", "e2", "e3", "no", "ni" };
            bool has_e1 = vectorNameList.Any(vectorName => vectorName == "e1");
            bool has_e2 = vectorNameList.Any(vectorName => vectorName == "e2");
            bool has_e3 = vectorNameList.Any(vectorName => vectorName == "e3");
            bool has_symb = vectorNameList.Any(vectorName => !basisList.Any(basisName => basisName == vectorName));
            if (has_e1 && has_e2 && has_e3 && has_symb)
                return true;

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

        public override Operand EvaluationStep(EvaluationContext context)
        {
            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            if (operandList.Count != 1)
                throw new EvaluationException(string.Format($"Expected exactly 1 argument, got {operandList.Count}."));

            operand = operandList[0];
            int grade = operand.Grade;
            if (grade == -1)
                throw new EvaluationException("Cannot identify an element non-homogenous in terms of grade; specifically, only blades are identified.");

            context.SetStorage("__blade__", operand);

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
                    context.GetStorage("weight", ref weight);
                    if (weight.IsAdditiveIdentity)
                    {
                        Operand.Evaluate("@normal = no . @__blade__ ^ ni", context);
                        weight = Operand.Evaluate("@weight = pow(@normal . @normal, 0.5)", context);
                        normal = Operand.Evaluate("@normal = @normal / @weight", context);
                        center = Operand.Evaluate("@center = -(no . @__blade__ / @weight) * @normal", context);
                        context.Log("The blade represents a plane.");
                    }
                    else
                    {
                        center = Operand.Evaluate("@center = (no ^ ni . @__blade__ ^ no ^ ni) / @weight", context);
                        Operand radiusSquared = Operand.Evaluate("@__square_radius__ = grade(@center . @center + 2 * no . @__blade__ / @weight, 0)", context);
                        if(radiusSquared is NumericScalar scalar)
                        {
                            if(scalar.value >= 0.0)
                            {
                                radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5)", context);
                                if (scalar.value == 0.0)
                                    context.Log("The blade represents a point.");
                                else
                                    context.Log("The blade represents a sphere.");
                            }
                            else
                            {
                                context.Log("The blade represents an imaginary sphere.");
                                radius = Operand.Evaluate("@radius = pow(-@__square_radius__, 0.5)", context);
                            }
                        }
                        else
                        {
                            context.Log("The blade represents a sphere.");
                            radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5)", context);
                        }
                    }
                    break;
                case 2:
                    normal = Operand.Evaluate("no ^ ni . @__blade__ ^ ni", context);
                    if(normal.IsAdditiveIdentity)
                    {

                    }
                    else
                    {
                        weight = Operand.Evaluate("@weight = pow(@normal . @normal, 0.5)", context);
                        normal = Operand.Evaluate("@normal = @normal / @weight", context);
                        center = Operand.Evaluate("@center = -@normal * (no ^ ni . @__blade__ ^ no * ni) / @weight", context);
                        Operand radiusSquared = Operand.Evaluate("@__square_radius__ = grade(@center . @center + 2 * ((no ^ ni . no ^ @__blade__) / @weight + (@center . @normal) * @normal, 0)", context);
                        if(radiusSquared is NumericScalar scalar)
                        {
                            if (scalar.value >= 0.0)
                            {
                                radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5", context);
                                if (scalar.value == 0.0)
                                    context.Log("The blade represents a tangent point.");
                                else
                                    context.Log("The blade represents a circle.");
                            }
                            else
                            {
                                context.Log("The blade represents an imaginary circle.");
                                radius = Operand.Evaluate("@radius = pow(-@__square_radius__, 0.5", context);
                            }
                        }
                        else
                        {
                            context.Log("The blade represents a circle.");
                            radius = Operand.Evaluate("@radius = pow(@__square_radius__, 0.5", context);
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