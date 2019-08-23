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
            funcList.Add(new Geometry("point"));
            funcList.Add(new Geometry("sphere"));
            funcList.Add(new Geometry("isphere"));
            funcList.Add(new Geometry("circle"));
            funcList.Add(new Geometry("icircle"));
            funcList.Add(new Geometry("pointpair"));
            funcList.Add(new Geometry("ipointpair"));
            funcList.Add(new Geometry("plane"));
            funcList.Add(new Geometry("line"));
            funcList.Add(new Geometry("flatpoint"));
            funcList.Add(new Geometry("tangentpoint"));
        }

        public override void GenerateDefaultStorage()
        {
            base.GenerateDefaultStorage();

            operandStorage.SetStorage("i", Operand.Evaluate("e1^e2^e3", this).output);

            // Add formulas for the geometric primitives of the conformal model in 3D space.
            Operand.Evaluate("@point := @weight * (no + @center + 0.5 * (@center . @center) * ni)", this);
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
                        if (vectorNameList.Any(vectorName => !ReturnBasisVectors().Any(basisName => basisName == vectorName)))
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
            return new List<string>() { "e1", "e2", "e3", "ni", "no" };
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

        public override string ShortDescription
        {
            get { return "Identify the given element of the conformal model in terms of what it reprsents as a geometry or transform."; }
        }

        public override void LogDetailedHelp(Context context)
        {
            context.Log("Identify the given element of the conformal model in terms of what it reprsents as a geometry or transform.");
            context.Log("If the element is a blade, what it represents is determined in terms of the inner product.");
            context.Log("If the element is a versor, what transformation it performs is determined in terms of conjugation.");
            context.Log("To determine what a blade represents in terms of the outer product, pass its dual as an argument to this function.");
        }

        private string MakeCoordinatesString(string variableName, Context context)
        {
            return "(" + string.Join(",", Enumerable.Range(1, 3).Select(i => Operand.Evaluate($"@{variableName} . e{i}", context).output).Select(operand => operand.Print(Format.PARSEABLE, context))) + ")";
        }

        public override Operand EvaluationStep(Context context)
        {
            if (operandList.Count != 1)
                throw new MathException(string.Format($"Expected exactly 1 argument, got {operandList.Count}."));

            Operand operand = base.EvaluationStep(context);
            if (operand != null)
                return operand;

            operand = operandList[0];
            int grade = operand.Grade;
            if (grade == -1)
                throw new MathException("Could not identify grade of given element.");

            OuterProduct blade = null;
            GeometricProduct versor = null;

            try
            {
                blade = FactorBlade.Factor(operand, context);
            }
            catch(MathException)
            {
            }
            
            try
            {
                versor = FactorVersor.Factor(operand, context);
            }
            catch(MathException)
            {
            }

            if(blade != null)
            {
                context.operandStorage.SetStorage("__blade__", operand);

                Evaluate("del(weight, center, radius, normal)", context);

                Operand weight = null;
                Operand center = null;
                Operand radius = null;
                Operand normal = null;

                switch(grade)
                {
                    case 0:
                    {
                        if(operand.IsAdditiveIdentity)
                            context.Log("The blade is all of space.");
                        else
                            context.Log("The blade is the empty set.");
                        break;
                    }
                    case 1:
                    {
                        weight = Evaluate("@weight = mag(ni . @__blade__)", context).output;
                        if(weight.IsAdditiveIdentity)
                        {
                            context.Log("The blade is a plane.");
                            weight = Evaluate("@weight = mag(no . ni ^ @__blade__)", context).output;
                            Evaluate("@__blade__ = @__blade__ / @weight", context);
                            normal = Evaluate("@normal = no . ni ^ @__blade__", context).output;
                            center = Evaluate("@center = (-no . @__blade__) * @normal", context).output;
                        }
                        else
                        {
                            Evaluate("@__blade__ = @__blade__ / @weight", context);
                            center = Evaluate("@center = ni^no . ni^no ^ @__blade__", context).output;
                            Operand squareRadius = Evaluate("@__square_radius__ = @__blade__ . @__blade__", context).output;
                            if(squareRadius.IsAdditiveIdentity)
                                context.Log("The blade is a point.");
                            else if(!(squareRadius is NumericScalar numericScalar))
                            {
                                context.Log("The blade is a sphere.");
                                radius = Evaluate("sqrt(@__square_radius__)", context).output;
                            }
                            else if(numericScalar.value > 0.0)
                            {
                                context.Log("The blade is a real sphere.");
                                radius = Evaluate("sqrt(@__square_radius__)", context).output;
                            }
                            else
                            {
                                context.Log("The blade is an imaginary sphere.");
                                radius = Evaluate("sqrt(-@__square_radius__)", context).output;
                            }
                        }

                        break;
                    }
                    case 2:
                    {
                        weight = Evaluate("mag(ni^no . ni ^ @__blade__)", context).output;
                        if(weight.IsAdditiveIdentity)
                        {
                            context.Log("The blade is a line.");
                            Evaluate("@normal = grade((no . ni ^ @__blade__) * @i, 1)", context);
                            weight = Evaluate("@weight = mag(@normal)", context).output;
                            normal = Evaluate("@normal = @normal / @weight", context).output;
                            center = Evaluate("@center = grade(((no . @__blade__ / @weight) * @normal) * @i, 1)", context).output;
                        }
                        else
                        {
                            Evaluate("@__blade__ = @__blade__ / @weight", context);
                            normal = Evaluate("@normal = ni^no . ni ^ @__blade__", context).output;
                            center = Evaluate("@center = @normal * grade(ni^no . @__blade__ ^ ni*no, 0)", context).output;
                            Operand squareRadius = Evaluate("@__square_radius__ = -@__blade__ . @__blade__", context).output;
                            if(squareRadius.IsAdditiveIdentity)
                                context.Log("The blade is a tangent point (degenerate circle.)");
                            else if (!(squareRadius is NumericScalar numericScalar))
                            {
                                context.Log("The blade is a circle.");
                                radius = Evaluate("sqrt(@__square_radius__)", context).output;
                            }
                            else if (numericScalar.value > 0.0)
                            {
                                context.Log("The blade is a real circle.");
                                radius = Evaluate("sqrt(@__square_radius__)", context).output;
                            }
                            else
                            {
                                context.Log("The blade is an imaginary circle.");
                                radius = Evaluate("sqrt(-@__square_radius__)", context).output;
                            }
                        }

                        break;
                    }
                    case 3:
                    {
                        break;
                    }
                    case 4:
                    {
                        break;
                    }
                    case 5:
                    {
                        break;
                    }
                }

                if (center != null)
                    context.Log($"The center is {this.MakeCoordinatesString("center", context)}.");

                if (radius != null)
                    context.Log($"The radius is {radius.Print(Format.PARSEABLE, context)}.");

                if (normal != null)
                    context.Log($"The normal is {this.MakeCoordinatesString("normal", context)}.");

                if (weight != null)
                    context.Log($"The weight is {weight.Print(Format.PARSEABLE, context)}.");
            }

            if(versor != null)
            {
                //...
            }

            if(blade == null && versor == null)
                context.Log("The given element was not a blade nor a versor.  I'm not sure what it represents.");

            return operand;
        }
    }

    public class Geometry : Function
    {
        private string geometry;

        public Geometry(string geometry = "") : base()
        {
            this.geometry = geometry;
        }

        public override Operand New()
        {
            return new Geometry();
        }

        public override Operand Copy()
        {
            Geometry copy = base.Copy() as Geometry;
            copy.geometry = this.geometry;
            return copy;
        }

        public override string Name(Format format)
        {
            return format == Format.LATEX ? @"\mbox{" + geometry + "}" : geometry;
        }

        public override string ShortDescription => $"Make a blade representative of a {this.geometry} of the conformal model in terms of the inner product.";

        public override void LogDetailedHelp(Context context)
        {
            context.Log($"Make a blade representative of a {this.geometry} in the conformal model in terms of the inner product.");
            context.Log("The required arguments are as follows...");
            this.GrabAllArguments(context, true);
            context.Log("Note that 3 scalars may be passed in place of any required euclidean vector.");
        }

        private void GrabArgument(string variableName, ref int i, Context context, bool justLog)
        {
            string expectedType = "";
            if(new List<string>() { "weight", "radius", "__x__", "__y__", "__z__" }.Any(value => value == variableName))
                expectedType = "scalar";
            else if(new List<string>() { "center", "normal" }.Any(value => value == variableName))
                expectedType = "vector";

            if(justLog)
            {
                context.Log(expectedType + ": " + variableName);
                return;
            }

            if(i >= operandList.Count)
                throw new MathException("Ran out of arguments.");

            if(expectedType == "vector")
            {
                if(operandList[i].Grade == 1)
                {
                    context.operandStorage.SetStorage(variableName, operandList[i++]);
                    return;
                }
                else if(operandList[i].Grade == 0)
                {
                    GrabArgument("__x__", ref i, context, justLog);
                    GrabArgument("__y__", ref i, context, justLog);
                    GrabArgument("__z__", ref i, context, justLog);
                    Operand.Evaluate("@" + variableName + " = @__x__*e1 + @__y__*e2 + @__z__*e3", context);
                    if(variableName == "normal")
                        Operand.Evaluate("@normal = normalize(@normal)", context);
                    return;
                }
            }
            else if(expectedType == "scalar")
            {
                if(operandList[i].Grade == 0)
                {
                    context.operandStorage.SetStorage(variableName, operandList[i++]);
                    return;
                }
            }
            
            throw new MathException("Could not make use of argument.");
        }

        private void GrabAllArguments(Context context, bool justLog)
        {
            int i = 0;

            if (this.geometry == "point" || this.geometry == "flatpoint")
            {
                GrabArgument("center", ref i, context, justLog);
            }
            else if (this.geometry == "sphere" || this.geometry == "isphere")
            {
                GrabArgument("center", ref i, context, justLog);
                GrabArgument("radius", ref i, context, justLog);
            }
            else if (new List<string>() { "sphere", "isphere", "circle", "icircle", "pointpair", "ipointpair" }.Any(value => value == this.geometry))
            {
                GrabArgument("center", ref i, context, justLog);
                GrabArgument("radius", ref i, context, justLog);
                GrabArgument("normal", ref i, context, justLog);
            }
            else if (this.geometry == "tangentpoint" || this.geometry == "plane" || this.geometry == "line")
            {
                GrabArgument("center", ref i, context, justLog);
                GrabArgument("normal", ref i, context, justLog);
            }
        }

        public override Operand EvaluationStep(Context context)
        {
            Operand operand = base.EvaluationStep(context);
            if(operand != null)
                return operand;

            context.operandStorage.SetStorage("weight", new NumericScalar(1.0));

            this.GrabAllArguments(context, false);

            return Operand.Evaluate("@" + this.geometry, context).output;
        }
    }
}