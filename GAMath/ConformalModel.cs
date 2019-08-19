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

            // TODO: I think we need convenience functions for making points, lines, spheres, etc.
        }

        public override void GenerateDefaultStorage()
        {
            base.GenerateDefaultStorage();

            operandStorage.SetStorage("i", Operand.Evaluate("e1^e2^e3", this).output);

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
            context.Log("If the element factors as a blade, what it directly and indirectly represents is determined.");
            context.Log("If the element factors as a versor, what transformation it performs is determined.");
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

            // TODO: If our argument is a blade, determine what geometry it dually and directly represents.
            //       If our argument is a versor, determine what transformation it performs.

            context.Log("Hello!");
            context.Log("Hello?");
            context.Log("Yes, hello there!");

            return null;
        }
    }
}