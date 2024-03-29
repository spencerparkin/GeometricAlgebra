﻿using System;
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
        }

        public override List<Function> GenerateFunctionList()
        {
            List<Function> funcList = base.GenerateFunctionList();

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

            // TODO: Add convenience functions for rotors, motors, inversions, dilations, transversions, etc.

            return funcList;
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

            BladeDecomposition bladeDecomposition = AnalyzeBlade(operand, context);
            context.Log("Blade analysis:");
            context.Log(bladeDecomposition.analysisList);

            VersorDecomposition versorDecomposition = AnalyzeVersor(operand, context);
            context.Log("Versor analysis:");
            context.Log(versorDecomposition.analysisList);

            if(bladeDecomposition.weight != null)
                context.operandStorage.SetStorage("weight", bladeDecomposition.weight);

            if(bladeDecomposition.center != null)
                context.operandStorage.SetStorage("center", bladeDecomposition.center);

            if (bladeDecomposition.radius != null)
                context.operandStorage.SetStorage("radius", bladeDecomposition.radius);

            if (bladeDecomposition.normal != null)
                context.operandStorage.SetStorage("normal", bladeDecomposition.normal);

            return operand;
        }

        public struct VersorDecomposition
        {
            public List<string> analysisList;
        }

        public VersorDecomposition AnalyzeVersor(Operand operand, Context context)
        {
            VersorDecomposition decomposition;
            decomposition.analysisList = new List<string>();

            using (var pushPopper = new ContextPushPopper(context))
            {
                int grade = operand.Grade;
                if(grade == 0)
                {
                    if(operand.IsAdditiveIdentity)
                        decomposition.analysisList.Add("Zero is not a versor because it is not invertible.");
                    else
                        decomposition.analysisList.Add("Non-zero scalars act as the identity transformation.");
                }
                else
                {
                    GeometricProduct versor = null;

                    try
                    {
                        versor = FactorVersor.Factor(operand, context);
                    }
                    catch(MathException)
                    {
                    }

                    if(versor == null)
                        decomposition.analysisList.Add("The given element was not a versor.");
                    else
                    {
                        // The reflections and rotations of conformal space give us the conformal transformations in the embedded 3D euclidean sub-space.
                        int i = versor.operandList.Count - 1;
                        while(i >= 0)
                        {
                            if(i - 1 >= 0 && AnalyzeRotation(versor.operandList[i - 1], versor.operandList[i], ref decomposition, context))
                                i -= 2;
                            else if(AnalyzeReflection(versor.operandList[i], ref decomposition, context))
                                i--;
                            else
                            {
                                decomposition.analysisList.Add("Failed to recognize transformation performed by vector.");
                                i--;
                            }
                        }
                    }
                }
            }

            return decomposition;
        }

        private bool AnalyzeRotation(Operand vectorA, Operand vectorB, ref VersorDecomposition versorDecomposition, Context context)
        {
            BladeDecomposition decompA = this.AnalyzeBlade(vectorA, context);
            BladeDecomposition decompB = this.AnalyzeBlade(vectorB, context);

            if(decompA.analysisList[0] == "The blade is a plane." && decompB.analysisList[0] == "The blade is a plane.")
            {
                Operand wedgeProduct = ExhaustEvaluation(new Trim(new List<Operand>() { new OuterProduct(new List<Operand>() { decompA.normal, decompB.normal }) }), context);
                if(wedgeProduct.IsAdditiveIdentity)
                {
                    versorDecomposition.analysisList.Add("Translation:");
                    //...calculate translation vector...
                }
                else
                {
                    versorDecomposition.analysisList.Add("Rotation:");
                    //...calculate center of rotation, axis of rotation, and rotation angle...
                }

                return true;
            }
            else if(decompA.analysisList[0] == "The blade is a real sphere." && decompB.analysisList[0] == "The blade is a real sphere.")
            {
                Operand difference = ExhaustEvaluation(new Trim(new List<Operand>() { new Sum(new List<Operand>() { decompA.center, new GeometricProduct(new List<Operand>() { new NumericScalar(-1.0), decompB.center }) }) }), context);
                if (difference.IsAdditiveIdentity)
                {
                    versorDecomposition.analysisList.Add("Dilation:");
                    //...calculate center of dilation and dilation scale...
                }
                else
                {
                    versorDecomposition.analysisList.Add("Transversion:");
                    //...
                }

                return true;
            }

            return false;
        }

        private bool AnalyzeReflection(Operand vector, ref VersorDecomposition versorDecomposition, Context context)
        {
            BladeDecomposition decomp = this.AnalyzeBlade(vector, context);

            if(decomp.analysisList[0] == "The blade is a real plane.")
            {
                versorDecomposition.analysisList.Add("Reflection:");
                //...
                return true;
            }
            else if(decomp.analysisList[0] == "The blade is a real sphere.")
            {
                versorDecomposition.analysisList.Add("Inversion:");
                //...
                return true;
            }
            else if(decomp.analysisList[0] == "The blade is an imaginary sphere.")
            {
                versorDecomposition.analysisList.Add("Imaginary inversion?");
                //...
                return true;
            }

            return false;
        }

        public struct BladeDecomposition
        {
            public Operand weight;
            public Operand center;
            public Operand radius;
            public Operand normal;
            public List<string> analysisList;
        }

        public BladeDecomposition AnalyzeBlade(Operand operand, Context context)
        {
            BladeDecomposition decomposition;
            decomposition.weight = null;
            decomposition.center = null;
            decomposition.radius = null;
            decomposition.normal = null;
            decomposition.analysisList = new List<string>();

            using(var pushPopper = new ContextPushPopper(context))
            {
                int grade = operand.Grade;
                if (grade < 0)
                    decomposition.analysisList.Add("Could not identify grade of given element.");
                else
                {
                    OuterProduct blade = null;

                    try
                    {
                        blade = FactorBlade.Factor(operand, context);
                    }
                    catch (MathException)
                    {
                    }

                    if(blade == null)
                        decomposition.analysisList.Add("The given element was not a blade.");
                    else
                    {
                        context.operandStorage.SetStorage("__blade__", operand);
                        Evaluate("del(@weight, @center, @radius, @normal)", context);

                        switch (grade)
                        {
                            case 0:
                            {
                                if (operand.IsAdditiveIdentity)
                                    decomposition.analysisList.Add("The blade is all of space.");
                                else
                                    decomposition.analysisList.Add("The blade is the empty set.");
                                break;
                            }
                            case 1:
                            {
                                decomposition.weight = Evaluate("@weight = mag(ni . @__blade__)", context).output;
                                if (decomposition.weight.IsAdditiveIdentity)
                                {
                                    decomposition.analysisList.Add("The blade is a plane.");
                                    decomposition.weight = Evaluate("@weight = mag(no . ni ^ @__blade__)", context).output;
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.normal = Evaluate("@normal = -no . ni ^ @__blade__", context).output;
                                    decomposition.center = Evaluate("@center = (-no . @__blade__) * @normal", context).output;
                                }
                                else
                                {
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.center = Evaluate("@center = ni^no . ni^no ^ @__blade__", context).output;
                                    Operand squareRadius = Evaluate("@__square_radius__ = @__blade__ . @__blade__", context).output;
                                    if (squareRadius.IsAdditiveIdentity)
                                        decomposition.analysisList.Add("The blade is a point.");
                                    else if (!(squareRadius is NumericScalar numericScalar))
                                    {
                                        decomposition.analysisList.Add("The blade is a sphere.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else if (numericScalar.value > 0.0)
                                    {
                                        decomposition.analysisList.Add("The blade is a real sphere.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else
                                    {
                                        decomposition.analysisList.Add("The blade is an imaginary sphere.");
                                        decomposition.radius = Evaluate("@radius = sqrt(-@__square_radius__)", context).output;
                                    }
                                }

                                break;
                            }
                            case 2:
                            {
                                decomposition.weight = Evaluate("@weight = mag(ni^no . ni ^ @__blade__)", context).output;
                                if (decomposition.weight.IsAdditiveIdentity)
                                {
                                    decomposition.analysisList.Add("The blade is a line.");
                                    Evaluate("@normal = (no . ni ^ @__blade__) * @i", context);
                                    decomposition.weight = Evaluate("@weight = mag(@normal)", context).output;
                                    decomposition.normal = Evaluate("@normal = @normal / @weight", context).output;
                                    decomposition.center = Evaluate("@center = ((no . @__blade__ / @weight) * @normal) * @i", context).output;
                                }
                                else
                                {
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.normal = Evaluate("@normal = ni^no . ni ^ @__blade__", context).output;
                                    decomposition.center = Evaluate("@center = @normal * (ni^no . @__blade__ ^ ni*no)", context).output;
                                    Operand squareRadius = Evaluate("@__square_radius__ = -@__blade__ . @__blade__", context).output;
                                    if (squareRadius.IsAdditiveIdentity)
                                        decomposition.analysisList.Add("The blade is a tangent point (degenerate circle.)");
                                    else if (!(squareRadius is NumericScalar numericScalar))
                                    {
                                        decomposition.analysisList.Add("The blade is a circle.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else if (numericScalar.value > 0.0)
                                    {
                                        decomposition.analysisList.Add("The blade is a real circle.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else
                                    {
                                        decomposition.analysisList.Add("The blade is an imaginary circle.");
                                        decomposition.radius = Evaluate("@radius = sqrt(-@__square_radius__)", context).output;
                                    }
                                }

                                break;
                            }
                            case 3:
                            {
                                decomposition.weight = Evaluate("@weight = mag((ni^no . ni ^ @__blade__) * @i)", context).output;
                                if (decomposition.weight.IsAdditiveIdentity)
                                {
                                    decomposition.analysisList.Add("The blade is a flat-point.");
                                    decomposition.weight = Evaluate("@weight = (no . ni ^ @__blade__) * @i", context).output;
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.center = Evaluate("@center = (no . @__blade__) * @i", context).output;
                                }
                                else
                                {
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.normal = Evaluate("@normal = (ni^no . ni ^ @__blade__) * -@i", context).output;
                                    decomposition.center = Evaluate("@center = @normal * (ni^no . @__blade__ ^ ni*no) * @i", context).output;
                                    Operand squareRadius = Evaluate("@__square_radius__ = -@__blade__ . @__blade__", context).output;
                                    if (squareRadius.IsAdditiveIdentity)
                                        decomposition.analysisList.Add("The blade is a tangent point (degenerate point-pair.)");
                                    else if (!(squareRadius is NumericScalar numericScalar))
                                    {
                                        decomposition.analysisList.Add("The blade is a point-pair.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else if (numericScalar.value > 0.0)
                                    {
                                        decomposition.analysisList.Add("The blade is a real point-pair.");
                                        decomposition.radius = Evaluate("@radius = sqrt(@__square_radius__)", context).output;
                                    }
                                    else
                                    {
                                        decomposition.analysisList.Add("The blade is an imaginary point-pair.");
                                        decomposition.radius = Evaluate("@radius = sqrt(-@__square_radius__)", context).output;
                                    }
                                }

                                break;
                            }
                            case 4:
                            {
                                Evaluate("@__blade__ = @__blade__ * @I", context);
                                decomposition.weight = Evaluate("@weight = mag(ni . @__blade__)", context).output;
                                if (decomposition.weight.IsAdditiveIdentity)
                                {
                                    Evaluate("del(@weight)", context);
                                    decomposition.weight = null;
                                    decomposition.analysisList.Add("The blade is the empty set.");
                                }
                                else
                                {
                                    Evaluate("@__blade__ = @__blade__ / @weight", context);
                                    decomposition.center = Evaluate("@center = ni^no . ni^no ^ @__blade__", context).output;
                                    Operand squareRadius = Evaluate("@__square_radius__ = trim(@__blade__ . @__blade__)", context).output;
                                    if (squareRadius.IsAdditiveIdentity)
                                        decomposition.analysisList.Add("The blade is a point.");
                                    else
                                    {
                                        Evaluate("del(@weight, @center)", context);
                                        decomposition.weight = null;
                                        decomposition.center = null;
                                        decomposition.analysisList.Add("The blade is the empty set.");
                                    }
                                }

                                break;
                            }
                            case 5:
                            {
                                decomposition.analysisList.Add("The blade is the empty set.");
                                break;
                            }
                        }

                        if (decomposition.center != null)
                            decomposition.analysisList.Add($"The center is {this.MakeCoordinatesString("center", context)}.");

                        if (decomposition.radius != null)
                            decomposition.analysisList.Add($"The radius is {decomposition.radius.Print(Format.PARSEABLE, context)}.");

                        if (decomposition.normal != null)
                            decomposition.analysisList.Add($"The normal is {this.MakeCoordinatesString("normal", context)}.");

                        if (decomposition.weight != null)
                            decomposition.analysisList.Add($"The weight is {decomposition.weight.Print(Format.PARSEABLE, context)}.");
                    }
                }
            }

            return decomposition;
        }
    }

    public class Geometry : Function
    {
        private string geometry;

        public Geometry() : base()
        {
            this.geometry = "";
        }

        public Geometry(string geometry = "") : base()
        {
            this.geometry = geometry;
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