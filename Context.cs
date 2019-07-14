﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace GeometricAlgebra
{
    public class Context
    {
        private Dictionary<string, Operand> operandStorage;
        public List<Function> funcList;
        public List<string> logMessageList;

        public Context()
        {
            funcList = new List<Function>();
            operandStorage = new Dictionary<string, Operand>();
            logMessageList = new List<string>();

            funcList.Add(new Inverse());
            funcList.Add(new Reverse());
            funcList.Add(new GradePart());
        }

        public void Log(string message)
        {
            logMessageList.Add(message);
        }

        public void ClearStorage(string variableName)
        {
            if (operandStorage.ContainsKey(variableName))
                operandStorage.Remove(variableName);
        }

        public void SetStorage(string variableName, Operand operand)
        {
            ClearStorage(variableName);

            operandStorage.Add(variableName, operand.Copy());
        }

        public bool GetStorage(string variableName, ref Operand operand)
        {
            if (!operandStorage.ContainsKey(variableName))
                return false;

            operand = operandStorage[variableName].Copy();
            return true;
        }

        // The operand returned here should have grade zero.
        public virtual Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            return new SymbolicScalarTerm(vectorNameA, vectorNameB);
        }

        public virtual bool IsLinearlyDependentSet(List<string> vectorNameList)
        {
            List<string> basisVectorList = ReturnBasisVectors();
            if (basisVectorList != null)
            {
                if (vectorNameList.Count > basisVectorList.Count)
                    return true;
            }

            return false;
        }

        public virtual List<string> ReturnBasisVectors()
        {
            return null;
        }

        public virtual Operation CreateFunction(string name)
        {
            IEnumerable<Function> enumerable = from func in funcList where func.Name(Operand.Format.PARSEABLE) == name select func;
            if (enumerable.Count() == 0)
                return null;

            return enumerable.ToArray()[0].Copy() as Operation;
        }

        public virtual string TranslateVectorNameForLatex(string vectorName)
        {
            return @"\vec{" + SubscriptNameForLatex(vectorName) + "}";
        }

        public virtual string TranslateScalarNameForLatex(string scalarName)
        {
            return SubscriptNameForLatex(scalarName);
        }

        public virtual string TranslateVariableNameForLatex(string varName)
        {
            return @"\textbf{" + SubscriptNameForLatex(varName) + "}";
        }

        public static string SubscriptNameForLatex(string name)
        {
            Regex rx = new Regex(@"^([a-zA-Z]+)([0-9]+)$");
            MatchCollection collection = rx.Matches(name);
            if (collection.Count != 1)
                return name;

            Match match = collection[0];
            return match.Groups[1].Value + "_{" + match.Groups[2].Value + "}";
        }
    }
}