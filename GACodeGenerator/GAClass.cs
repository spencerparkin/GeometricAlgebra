using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
                string scalarName = basisElement.Replace('^', '_');
                if (char.IsDigit(scalarName[0]))
                    scalarName = "_" + scalarName;

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

        private bool CanTakeResult(Result result, Context context)
        {
            Operand resultOperand = result.output.Copy();

            if(resultOperand is SymbolicScalarTerm || resultOperand is NumericScalar)
            {
                Blade blade = new Blade();
                blade.scalar = resultOperand;
                resultOperand = blade;
            }

            if(resultOperand is Blade)
            {
                Sum sum = new Sum();
                sum.operandList.Add(resultOperand);
                resultOperand = sum;
            }

            Sum resultSum = resultOperand as Sum;

            for(int i = 0; i < resultSum.operandList.Count; i++)
            {
                Operand term = resultSum.operandList[i];
                if(term.IsAdditiveIdentity)
                {
                    resultSum.operandList[i] = new NumericScalar(1.0);
                }
            }

            foreach(Operand resultBasisElement in resultSum.operandList)
            {
                bool hasBasisElement = false;

                foreach (var keyValuePair in basisElementToOperandMap)
                {
                    Operand basisElement = keyValuePair.Value;

                    Inverse inverse = new Inverse();
                    inverse.operandList.Add(basisElement.Copy());

                    GeometricProduct geometricProduct = new GeometricProduct();
                    geometricProduct.operandList.Add(resultBasisElement.Copy());
                    geometricProduct.operandList.Add(inverse);

                    Operand geometricProductResult = Operand.ExhaustEvaluation(geometricProduct, context);

                    if(geometricProductResult.Grade == 0)
                    {
                        hasBasisElement = true;
                        break;
                    }
                }

                if (!hasBasisElement)
                    return false;
            }

            return true;
        }

        private string ReplaceScalarsInExpression(string expression, GAClass gaClass, string gaClassInstanceName, string varName)
        {
            string newExpression = expression;

            List<string> basisElementList = gaClass.GetSortedListOfBasisElements();

            for (int i = 0; i < basisElementList.Count; i++)
            {
                string memberName = basisElementList[i];
                if (memberName == "1")
                    memberName = "_1";

                memberName = memberName.Replace("^", "_");
                memberName = $"{gaClassInstanceName}.{memberName}";

                newExpression = newExpression.Replace($"${varName}{i}", memberName);
            }

            return newExpression;
        }

        private string GenerateCodeForResult(Result result, Context context, GAClass gaClassA, GAClass gaClassB)
        {
            Operand resultOperand = result.output.Copy();

            if (!(resultOperand is Sum))
            {
                Sum sum = new Sum();
                sum.operandList.Add(result.output);
                resultOperand = sum;
            }

            Sum resultSum = resultOperand as Sum;

            var componentMap = new Dictionary<string, List<string>>();

            foreach (Operand resultBasisElement in resultSum.operandList)
            {
                foreach (var keyValuePair in basisElementToOperandMap)
                {
                    Operand basisElement = keyValuePair.Value;

                    Inverse inverse = new Inverse();
                    inverse.operandList.Add(basisElement.Copy());

                    GeometricProduct geometricProduct = new GeometricProduct();
                    geometricProduct.operandList.Add(resultBasisElement.Copy());
                    geometricProduct.operandList.Add(inverse);

                    Operand geometricProductResult = Operand.ExhaustEvaluation(geometricProduct, context);
                    if (geometricProductResult.Grade != 0)
                        continue;

                    string expression = geometricProductResult.Print(Operand.Format.PARSEABLE, context);

                    expression = ReplaceScalarsInExpression(expression, gaClassA, $"{gaClassA.name.ToLower()}A", "a");
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

            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();

            //--------------------------------------------------------------------

            string hFileText = "#pragma once\n\n";

            hFileText += $"namespace {nameSpace}\n";
            hFileText += "{\n";

            hFileText += $"\tclass {name}\n";
            hFileText += "\t{\n";
            hFileText += "\tpublic:\n";

            //
            // Construction
            //

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
                        hFileText += $"\t\tvoid Subract(const {gaClassA.name}& {gaClassA.name.ToLower()}A, const {gaClassB.name}& {gaClassB.name.ToLower()}B);\n";

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
            // Members
            //

            hFileText += $"\t\tdouble {string.Join(", ", memberList)};\n";
            hFileText += "\t};\n";
            hFileText += "}";

            //--------------------------------------------------------------------

            string cppFileText = $"#include \"{name}.h\"\n";
            cppFileText += "\n";
            cppFileText += $"using namespace {nameSpace};\n";
            cppFileText += "\n";

            //
            // Construction
            //

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
                                cppFileText += $"\tthis->{member} = {gaClassB.name.ToLower()}B.{member};\n";
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

            File.WriteAllText(hFilePath, hFileText);
            File.WriteAllText(cppFilePath, cppFileText);
        }
    }
}