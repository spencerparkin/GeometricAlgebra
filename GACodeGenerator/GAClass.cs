using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GeometricAlgebra;

#nullable disable

namespace GACodeGenerator
{
    public class GAClass
    {
        private HashSet<string> basisSet;
        private string name;
        private Dictionary<string, string> basisElementToMemberMap;
        private Dictionary<string, Operand> basisElementToOperandMap;

        public GAClass(string givenName, HashSet<string> givenBasisSet)
        {
            name = givenName;
            basisSet = givenBasisSet;

            basisElementToMemberMap = new Dictionary<string, string>();
            foreach (string basisElement in basisSet)
            {
                string scalarName = this.MakeScalarNameFromBasisElement(basisElement);
                basisElementToMemberMap.Add(basisElement, scalarName);
            }

            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();

            basisElementToOperandMap = new Dictionary<string, Operand>();
            foreach (string basisElement in basisSet)
            {
                var result = Operand.Evaluate(basisElement, context);
                if (result.output == null)
                    throw new Exception($"Basis element \"{basisElement}\" could not be evaluated.");

                basisElementToOperandMap.Add(basisElement, result.output);
            }
        }

        private string MakeScalarNameFromBasisElement(string basisElement)
        {
            string scalarName = basisElement.Replace('^', '_');
            if (char.IsDigit(scalarName[0]))
                scalarName = "_" + scalarName;
            return scalarName;
        }

        private List<string> GetSortedListOfBasisElements()
        {
            var basisList = basisSet.ToList();
            basisList.Sort();
            return basisList;
        }

        private string GenerateExpression(string varName)
        {
            List<string> basisList = GetSortedListOfBasisElements();
            List<string> termList = new List<string>();
            for (int i = 0; i < basisList.Count; i++)
            {
                string basisElement = basisList[i];
                string term = $"${varName}{i} * {basisElement}";
                termList.Add(term);
            }

            string expression = string.Join(" + ", termList);
            return expression;
        }

        private Sum StandardizeResult(Operand result)
        {
            Sum sum = result as Sum;
            if (sum == null)
            {
                sum = new Sum();
                sum.operandList.Add(result);
                result = sum;
            }

            Sum standardizedSum = new Sum();

            for(int i = 0; i < sum.operandList.Count; i++)
            {
                if (!(sum.operandList[i] is Blade))
                {
                    Debug.Assert(sum.operandList[i].Grade == 0);
                    Blade blade = new Blade();
                    blade.scalar = sum.operandList[i];
                    standardizedSum.operandList.Add(blade);
                }
                else
                {
                    Blade blade = sum.operandList[i] as Blade;
                    if(blade.scalar is Sum)
                    {
                        foreach (Operand operand in (blade.scalar as Sum).operandList)
                        {
                            Blade bladeCopy = blade.Copy() as Blade;
                            bladeCopy.scalar = operand;
                            standardizedSum.operandList.Add(bladeCopy);
                        }
                    }
                    else
                    {
                        standardizedSum.operandList.Add(blade);
                    }
                }
            }

            return standardizedSum;
        }

        // Our job here is to determine whether the two given basis elements are a
        // positive or negative unit-multiple of one another.  That is, do we have A == B or A == -B?
        // Note that it's tempting to multiply one by the inverse of the other, but we can't
        // assume here that either basis element is invertible.
        private bool BasisElementsAreTheSame(Operand basisElementA, Operand basisElementB, out double sign, Context context)
        {
            Sum sum = new Sum();
            sum.operandList.Add(basisElementA.Copy());
            sum.operandList.Add(basisElementB.Copy());
            Operand result = Operand.ExhaustEvaluation(sum, context);
            if(result.IsAdditiveIdentity)
            {
                sign = -1.0;
                return true;
            }

            GeometricProduct product = new GeometricProduct();
            product.operandList.Add(new NumericScalar(-1.0));
            product.operandList.Add(basisElementA.Copy());

            sum = new Sum();
            sum.operandList.Add(product);
            sum.operandList.Add(basisElementB.Copy());
            result = Operand.ExhaustEvaluation(sum, context);
            if(result.IsAdditiveIdentity)
            {
                sign = 1.0;
                return true;
            }

            sign = 0.0;
            return false;
        }

        private bool CanTakeResult(Result result, Context context)
        {
            Sum resultSum = StandardizeResult(result.output.Copy());

            return resultSum.operandList.All(resultTerm =>
            {
                Operand resultBasisElement = resultTerm.Copy();
                Debug.Assert(resultBasisElement is Blade);
                (resultBasisElement as Blade).scalar = new NumericScalar(1.0);

                return basisElementToOperandMap.Values.Any(basisElement =>
                {
                    double sign = 0.0;
                    return BasisElementsAreTheSame(basisElement, resultBasisElement, out sign, context);
                });
            });
        }

        private string ReplaceScalarsInExpression(string expression, GAClass gaClass, string gaClassInstanceName, string varName)
        {
            string newExpression = expression;

            List<string> basisElementList = gaClass.GetSortedListOfBasisElements();

            // We count backwards so that we, for example, match "b11" before "b1".
            for (int i = basisElementList.Count - 1; i >= 0; i--)
            {
                string memberName = basisElementList[i];
                if (memberName == "1")
                    memberName = "_1";

                memberName = memberName.Replace("^", "_");

                if (gaClassInstanceName == "this")
                    memberName = $"this->{memberName}";
                else
                    memberName = $"{gaClassInstanceName}.{memberName}";

                newExpression = newExpression.Replace($"${varName}{i}", memberName);
            }

            return newExpression;
        }

        private string GenerateCodeForResult(Result result, Context context, GAClass gaClassA, GAClass gaClassB)
        {
            Sum resultSum = StandardizeResult(result.output.Copy());

            var componentMap = new Dictionary<string, List<string>>();

            foreach (Operand resultTerm in resultSum.operandList)
            {
                Operand resultBasisElement = resultTerm.Copy();
                Debug.Assert(resultBasisElement is Blade);
                (resultBasisElement as Blade).scalar = new NumericScalar(1.0);

                foreach (var keyValuePair in basisElementToOperandMap)
                {
                    Operand basisElement = keyValuePair.Value;
                    double sign = 0.0;
                    if (!BasisElementsAreTheSame(basisElement, resultBasisElement, out sign, context))
                        continue;

                    Operand resultScalar = (resultTerm.Copy() as Blade).scalar;

                    if (sign == -1.0)
                    {
                        GeometricProduct geometricProduct = new GeometricProduct();
                        geometricProduct.operandList.Add(new NumericScalar(sign));
                        geometricProduct.operandList.Add(resultScalar);

                        resultScalar = Operand.ExhaustEvaluation(geometricProduct, context);
                    }

                    string expression = resultScalar.Print(Operand.Format.PARSEABLE, context);

                    if (gaClassA != null)
                        expression = ReplaceScalarsInExpression(expression, gaClassA, $"{gaClassA.name.ToLower()}A", "a");

                    if (gaClassB != null)
                        expression = ReplaceScalarsInExpression(expression, gaClassB, $"{gaClassB.name.ToLower()}B", "b");

                    expression = expression.Replace("0", "0.0");
                    expression = expression.Replace("-1", "-1.0");
                    expression = expression.Replace("*", " * ");

                    List<string> termList = null;
                    if (componentMap.ContainsKey(keyValuePair.Key))
                        termList = componentMap[keyValuePair.Key];
                    else
                    {
                        termList = new List<string>();
                        componentMap[keyValuePair.Key] = termList;
                    }

                    termList.Add(expression);
                }
            }

            string code = "";

            foreach (var basisElement in basisSet)
            {
                string memberName = basisElement;
                if (memberName == "1")
                    memberName = "_1";

                memberName = memberName.Replace("^", "_");

                if (!componentMap.ContainsKey(basisElement))
                    code += $"\tthis->{memberName} = 0.0;\n";
                else
                {
                    List<string> termList = componentMap[basisElement];
                    string expression = string.Join(" + ", termList);
                    code += $"\tthis->{memberName} = {expression};\n";
                }
            }

            return code;
        }

        public void GenerateSourceCode(string outDir, string nameSpace, List<GAClass> gaClassList)
        {
            string cppFilePath = Path.Combine(outDir, name + ".cpp");
            string hFilePath = Path.Combine(outDir, name + ".h");

            Console.WriteLine($"Generating {name}.cpp/.h...");

            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();

            context.unrollPowers = true;

            //--------------------------------------------------------------------

            string hFileText = "";

            hFileText += "// NOTE: This is a generated source file!  Any edits you make will not be preserved.\n";
            hFileText += "\n";
            hFileText += "#pragma once\n";
            hFileText += "\n";
            hFileText += "#include <functional>\n";
            hFileText += "\n";
            hFileText += $"namespace {nameSpace}\n";
            hFileText += "{\n";

            foreach(GAClass gaClass in gaClassList)
                if (gaClass != this)
                    hFileText += $"\tclass {gaClass.name};\n";

            hFileText += "\n";
            hFileText += $"\tclass {name}\n";
            hFileText += "\t{\n";
            hFileText += "\tpublic:\n";

            //
            // Construction
            //

            hFileText += $"\t\t{name}();\n";

            List<string> memberList = new List<string>();
            foreach (var keyValuePair in basisElementToMemberMap)
                memberList.Add(keyValuePair.Value);

            List<string> paramList = memberList.Select(x => $"double {x}").ToList();
            hFileText += $"\t\t{name}({string.Join(", ", paramList)});\n";

            foreach(GAClass gaClass in gaClassList)
                if(gaClass.basisSet.IsSubsetOf(basisSet))
                    hFileText += $"\t\t{name}(const {gaClass.name}& {gaClass.name.ToLower()});\n";

            hFileText += "\n";

            //
            // Equality
            //

            hFileText += $"\t\tbool IsEqualTo(const {this.name}& {this.name.ToLower()}, double epsilon = 1e-5) const;\n";
            hFileText += "\n";

            //
            // Extraction
            //

            int numExtractions = 0;
            foreach (GAClass gaClass in gaClassList)
            {
                if (!gaClass.basisSet.IsProperSubsetOf(this.basisSet))
                    continue;

                numExtractions++;

                hFileText += $"\t\tvoid Get{gaClass.name}({gaClass.name}& {gaClass.name.ToLower()}) const;\n";
            }

            if (numExtractions > 0)
                hFileText += "\n";

            //
            // Addition/Subtraction
            //

            foreach (GAClass gaClassA in gaClassList)
                foreach (GAClass gaClassB in gaClassList)
                    if (gaClassA.basisSet.IsSubsetOf(basisSet) && gaClassB.basisSet.IsSubsetOf(basisSet))
                        hFileText += $"\t\tvoid Add(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B);\n";

            hFileText += "\n";

            foreach (GAClass gaClassA in gaClassList)
                foreach (GAClass gaClassB in gaClassList)
                    if (gaClassA.basisSet.IsSubsetOf(basisSet) && gaClassB.basisSet.IsSubsetOf(basisSet))
                        hFileText += $"\t\tvoid Subtract(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B);\n";

            hFileText += "\n";

            //
            // Inner/Outer/Geometric Products
            //

            for (int i = 0; i < 3; i++)
            {
                foreach (GAClass gaClassA in gaClassList)
                {
                    string expressionA = gaClassA.GenerateExpression("a");

                    foreach (GAClass gaClassB in gaClassList)
                    {
                        string expressionB = gaClassB.GenerateExpression("b");

                        string operation = "";
                        switch(i)
                        {
                            case 0: operation = "."; break;
                            case 1: operation = "^"; break;
                            case 2: operation = "*"; break;
                        }

                        string expression = $"({expressionA}) {operation} ({expressionB})";

                        Result result = Operand.Evaluate(expression, context);
                        if (!CanTakeResult(result, context))
                            continue;

                        string funcName = "";
                        switch(i)
                        {
                            case 0: funcName = "InnerProduct"; break;
                            case 1: funcName = "OuterProduct"; break;
                            case 2: funcName = "GeometricProduct"; break;
                        }

                        hFileText += $"\t\tvoid {funcName}(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B);\n";
                    }
                }

                hFileText += "\n";
            }

            //
            // Reverse & Square Magnitude
            //

            hFileText += $"\t\tvoid Reverse(const {name}& {name.ToLower()}A);\n";
            hFileText += "\n";
            hFileText += $"\t\tdouble SquareMagnitude() const;\n";
            hFileText += "\n";

            //
            // Matrix Conversion
            //

            hFileText += "\t\tvoid GetMatrixSize(int& numRows, int& numCols) const;\n";
            hFileText += "\n";
            hFileText += "\t\tvoid ToSquareMatrix(std::function<void(int, int, double)> elementCallback) const;\n";
            hFileText += "\t\tvoid ToColumnMatrix(std::function<void(int, double)> elementCallback) const;\n";
            hFileText += "\t\tvoid FromColumnMatrix(std::function<void(int, double&)> elementCallback);\n";
            hFileText += "\n";

            //
            // Members
            //

            hFileText += $"\t\tdouble {string.Join(", ", memberList)};\n";
            hFileText += "\t};\n";
            hFileText += "}";

            //--------------------------------------------------------------------

            string cppFileText = "";

            cppFileText += "// NOTE: This is a generated source file!  Any edits you make will not be preserved.\n";
            cppFileText += "\n";
            cppFileText += $"#include \"{name}.h\"\n";

            foreach (GAClass gaClass in gaClassList)
                if (gaClass != this)
                    cppFileText += $"#include \"{gaClass.name}.h\"\n";

            cppFileText += "\n";
            cppFileText += $"using namespace {nameSpace};\n";
            cppFileText += "\n";

            //
            // Construction
            //

            cppFileText += $"{name}::{name}()\n";
            cppFileText += "{\n";

            foreach (string member in memberList)
                cppFileText += $"\tthis->{member} = 0.0;\n";

            cppFileText += "}\n";
            cppFileText += "\n";

            cppFileText += $"{name}::{name}({string.Join(", ", paramList)})\n";
            cppFileText += "{\n";

            foreach(string member in memberList)
                cppFileText += $"\tthis->{member} = {member};\n";

            cppFileText += "}\n";
            cppFileText += "\n";

            foreach(GAClass gaClass in gaClassList)
            {
                if (!gaClass.basisSet.IsSubsetOf(basisSet))
                    continue;

                cppFileText += $"{name}::{name}(const {gaClass.name}& {gaClass.name.ToLower()})\n";
                cppFileText += "{\n";

                foreach(string basisElement in basisSet)
                {
                    string member = basisElementToMemberMap[basisElement];
                    if (!gaClass.basisSet.Contains(basisElement))
                        cppFileText += $"\tthis->{member} = 0.0;\n";
                    else
                    {
                        Debug.Assert(member == gaClass.basisElementToMemberMap[basisElement]);
                        cppFileText += $"\tthis->{member} = {gaClass.name.ToLower()}.{member};\n";
                    }
                }

                cppFileText += "}\n";
                cppFileText += "\n";
            }

            //
            // Equality
            //

            cppFileText += $"bool {this.name}::IsEqualTo(const {this.name}& {this.name.ToLower()}, double epsilon /*= 1e-5*/) const\n";
            cppFileText += "{\n";

            foreach(string basisElement in this.basisSet)
            {
                string scalarVarName = this.MakeScalarNameFromBasisElement(basisElement);
                cppFileText += $"\tif(::fabs(this->{scalarVarName} - {this.name.ToLower()}.{scalarVarName}) >= epsilon)\n";
                cppFileText += "\t\treturn false;\n";
                cppFileText += "\n";
            }

            cppFileText += "\treturn true;\n";
            cppFileText += "}\n";
            cppFileText += "\n";

            //
            // Extraction
            //

            if (numExtractions > 0)
            {
                foreach (GAClass gaClass in gaClassList)
                {
                    if (!gaClass.basisSet.IsProperSubsetOf(this.basisSet))
                        continue;

                    cppFileText += $"void {name}::Get{gaClass.name}({gaClass.name}& {gaClass.name.ToLower()}) const\n";
                    cppFileText += "{\n";

                    foreach (string basisElement in gaClass.basisSet)
                    {
                        string scalarName = MakeScalarNameFromBasisElement(basisElement);
                        cppFileText += $"\t{gaClass.name.ToLower()}.{scalarName} = this->{scalarName};\n";
                    }

                    cppFileText += "}\n\n";
                }
            }

            //
            // Addiction/Subtraction
            //

            for (int i = 0; i < 2; i++)
            {
                foreach (GAClass gaClassA in gaClassList)
                {
                    foreach (GAClass gaClassB in gaClassList)
                    {
                        if (!gaClassA.basisSet.IsSubsetOf(basisSet) || !gaClassB.basisSet.IsSubsetOf(basisSet))
                            continue;

                        cppFileText += $"void {name}::{((i == 0) ? "Add" : "Subtract")}(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B)\n";
                        cppFileText += "{\n";

                        foreach (string basisElement in basisSet)
                        {
                            string member = basisElementToMemberMap[basisElement];
                            if (gaClassA.basisSet.Contains(basisElement) && gaClassB.basisSet.Contains(basisElement))
                                cppFileText += $"\tthis->{member} = {gaClassA.name.ToLower()}A.{member} {((i == 0) ? '+' : '-')} {gaClassB.name.ToLower()}B.{member};\n";
                            else if (gaClassA.basisSet.Contains(basisElement))
                                cppFileText += $"\tthis->{member} = {gaClassA.name.ToLower()}A.{member};\n";
                            else if (gaClassB.basisSet.Contains(basisElement))
                                cppFileText += $"\tthis->{member} = {((i == 0) ? "" : "-")}{gaClassB.name.ToLower()}B.{member};\n";
                            else
                                cppFileText += $"\tthis->{member} = 0.0;\n";
                        }

                        cppFileText += "}\n";
                        cppFileText += "\n";
                    }
                }
            }

            //
            // Inner/Outer/Geometric Products
            //

            for (int i = 0; i < 3; i++)
            {
                foreach (GAClass gaClassA in gaClassList)
                {
                    string expressionA = gaClassA.GenerateExpression("a");

                    foreach (GAClass gaClassB in gaClassList)
                    {
                        string expressionB = gaClassB.GenerateExpression("b");

                        string operation = "";
                        switch (i)
                        {
                            case 0: operation = "."; break;
                            case 1: operation = "^"; break;
                            case 2: operation = "*"; break;
                        }

                        string expression = $"({expressionA}) {operation} ({expressionB})";

                        Result result = Operand.Evaluate(expression, context);
                        if (!CanTakeResult(result, context))
                            continue;

                        string funcName = "";
                        switch (i)
                        {
                            case 0: funcName = "InnerProduct"; break;
                            case 1: funcName = "OuterProduct"; break;
                            case 2: funcName = "GeometricProduct"; break;
                        }

                        cppFileText += $"void {name}::{funcName}(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B)\n";
                        cppFileText += "{\n";
                        cppFileText += GenerateCodeForResult(result, context, gaClassA, gaClassB);
                        cppFileText += "}\n\n";
                    }
                }
            }

            //
            // Reverse & Square Magnitude
            //

            string reverseExpression = $"rev({this.GenerateExpression("a")})";

            Result reverseResult = Operand.Evaluate(reverseExpression, context);
            if(!CanTakeResult(reverseResult, context))
                throw new Exception($"Could not take reverse result.");

            cppFileText += $"void {name}::Reverse(const {name}& {name.ToLower()}A)\n";
            cppFileText += "{\n";
            cppFileText += GenerateCodeForResult(reverseResult, context, this, null);
            cppFileText += "}\n";

            string squareMagnitudeExpression = $"grade(({this.GenerateExpression("a")}) . rev({this.GenerateExpression("a")}), 0)";

            Result squareMagnitudeResult = Operand.Evaluate(squareMagnitudeExpression, context);

            string squareMagExpResultSumStr = squareMagnitudeResult.output.Print(Operand.Format.PARSEABLE, context);

            string squareMagCodeStr = ReplaceScalarsInExpression(squareMagExpResultSumStr, this, "this", "a");

            cppFileText += "\n";
            cppFileText += $"double {name}::SquareMagnitude() const\n";
            cppFileText += "{\n";
            cppFileText += $"\treturn {squareMagCodeStr};\n";
            cppFileText += "}\n";

            //
            // Matrix Conversion
            //

            this.GenerateMatrixConversionCode(ref cppFileText, gaClassList, context);

            //
            // Finally, flush files...
            //

            File.WriteAllText(hFilePath, hFileText);
            File.WriteAllText(cppFilePath, cppFileText);
        }

        private void GenerateMatrixConversionCode(ref string cppFileText, List<GAClass> gaClassList, Context context)
        {
            GAClass gaMultivectorClass = null;
            int maxBasisSetCount = 0;
            for(int i = 0; i < gaClassList.Count; i++)
            {
                if (gaClassList[i].basisSet.Count > maxBasisSetCount)
                {
                    maxBasisSetCount = gaClassList[i].basisSet.Count;
                    gaMultivectorClass = gaClassList[i];
                }
            }

            Debug.Assert(gaMultivectorClass != null);

            List<string> basisList = GetSortedListOfBasisElements();

            string productExpression = $"({this.GenerateExpression("a")}) * ({this.GenerateExpression("b")})";
            Result productResult = Operand.Evaluate(productExpression, context);
            Sum resultSum = StandardizeResult(productResult.output);
            List<string> foundBasisList = new List<string>();
            HashSet<string> foundBasisSet = new HashSet<string>();

            // Note that for correctness, we assume here that our basis elements
            // will be found in order of ascending grade.  In particular, we need
            // the grade zero basis element found first.  That's because the left-most
            // column of the inverted matrix is assumed to contain our inverse result.
            foreach (Operand resultTerm in resultSum.operandList)
            {
                Operand resultBasisElement = resultTerm.Copy();
                Debug.Assert(resultBasisElement is Blade);
                (resultBasisElement as Blade).scalar = new NumericScalar(1.0);

                foreach (var keyValuePair in gaMultivectorClass.basisElementToOperandMap)
                {
                    Operand basisElement = keyValuePair.Value;
                    double sign = 0.0;
                    if (BasisElementsAreTheSame(basisElement, resultBasisElement, out sign, context))
                    {
                        if(!foundBasisSet.Contains(keyValuePair.Key))
                        {
                            foundBasisList.Add(keyValuePair.Key);
                            foundBasisSet.Add(keyValuePair.Key);
                        }
                        break;
                    }
                }
            }

            Debug.Assert(foundBasisList.Count > 0);
            Debug.Assert(foundBasisList[0] == "1");

            cppFileText += "\n";
            cppFileText += $"void {name}::GetMatrixSize(int& numRows, int& numCols) const\n";
            cppFileText += "{\n";
            cppFileText += $"\tnumRows = {foundBasisList.Count};\n";
            cppFileText += $"\tnumCols = {basisList.Count};\n";
            cppFileText += "}\n";
            cppFileText += "\n";

            cppFileText += $"void {name}::ToSquareMatrix(std::function<void(int, int, double)> elementCallback) const\n";
            cppFileText += "{\n";

            foreach (Operand resultTerm in resultSum.operandList)
            {
                Operand resultBasisElement = resultTerm.Copy();
                Debug.Assert(resultBasisElement is Blade);
                (resultBasisElement as Blade).scalar = new NumericScalar(1.0);

                int row = -1;

                foreach (var keyValuePair in gaMultivectorClass.basisElementToOperandMap)
                {
                    Operand basisElement = keyValuePair.Value;
                    double sign = 0.0;
                    if (BasisElementsAreTheSame(basisElement, resultBasisElement, out sign, context))
                    {
                        row = foundBasisList.IndexOf(keyValuePair.Key);
                        break;
                    }
                }

                Debug.Assert(row != -1);

                Operand scalarTerm = resultTerm.Copy();
                if (scalarTerm is Blade)
                    scalarTerm = (scalarTerm as Blade).scalar;

                int col = -1;

                string scalarTermStr = scalarTerm.Print(Operand.Format.PARSEABLE);
                for (int i = this.basisSet.Count - 1; i >= 0; i--)
                {
                    if (scalarTermStr.IndexOf($"b{i}") != -1)
                    {
                        col = i;
                        break;
                    }
                }

                Debug.Assert(col != -1);

                GeometricProduct product = new GeometricProduct();
                product.operandList.Add(scalarTerm);
                product.operandList.Add(new SymbolicScalarTerm($"b{col}", -1));
                Operand productResultOperand = Operand.ExhaustEvaluation(product, context);

                scalarTermStr = productResultOperand.Print(Operand.Format.PARSEABLE);

                string elementCode = ReplaceScalarsInExpression(scalarTermStr, this, "this", "a");
                elementCode = elementCode.Replace("-1", "-1.0");

                cppFileText += $"\telementCallback({row}, {col}, {elementCode});\n";
            }

            cppFileText += "}\n";
            cppFileText += "\n";
            cppFileText += $"void {name}::ToColumnMatrix(std::function<void(int, double)> elementCallback) const\n";
            cppFileText += "{\n";

            for (int row = 0; row < basisList.Count; row++)
            {
                string basisElementStr = this.MakeScalarNameFromBasisElement(basisList[row]);
                cppFileText += $"\telementCallback({row}, this->{basisElementStr});\n";
            }

            cppFileText += "}\n";
            cppFileText += "\n";

            cppFileText += $"void {name}::FromColumnMatrix(std::function<void(int, double&)> elementCallback)\n";
            cppFileText += "{\n";

            for (int row = 0; row < basisList.Count; row++)
            {
                string basisElementStr = this.MakeScalarNameFromBasisElement(basisList[row]);
                cppFileText += $"\telementCallback({row}, this->{basisElementStr});\n";
            }

            cppFileText += "}\n";
            cppFileText += "\n";
        }
    }
}