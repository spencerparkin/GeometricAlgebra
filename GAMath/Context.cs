using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace GeometricAlgebra
{
    public class Context
    {
        public OperandStorage operandStorage;
        public OperandStorage operandCache;
        public bool useOperandCache;
        public List<Function> funcList;
        public List<string> logMessageList;
        public double epsilon;
        public double evaluationTimeoutMilliseconds;

        public Context()
        {
            funcList = new List<Function>();
            operandStorage = new OperandStorage();
            operandCache = new OperandStorage();
            useOperandCache = true;
            logMessageList = new List<string>();
            epsilon = 1e-9;
            evaluationTimeoutMilliseconds = 4000.0;

            funcList.Add(new Help());
            funcList.Add(new Reset());
            funcList.Add(new Freeze());
            funcList.Add(new Magnitude());
            funcList.Add(new Normalize());
            funcList.Add(new Inverse());
            funcList.Add(new Reverse());
            funcList.Add(new GradePart());
            funcList.Add(new Trim());
            funcList.Add(new Delete());
            funcList.Add(new Sine());
            funcList.Add(new Cosine());
            funcList.Add(new Power());
            funcList.Add(new Exponent());
            funcList.Add(new Logarithm());
            funcList.Add(new SquareRoot());
            funcList.Add(new Adjugate());
            funcList.Add(new Determinant());
            funcList.Add(new FactorBlade());
            funcList.Add(new FactorVersor());
            funcList.Add(new FactorPolynomial());
            funcList.Add(new Join());
            funcList.Add(new Meet());
        }

        public virtual void GenerateDefaultStorage()
        {
            operandStorage.SetStorage("I", this.MakePsuedoScalar());
            operandStorage.SetStorage("pi", Operand.Evaluate("3.1415926535897932384626434", this).output);
            operandStorage.SetStorage("e", Operand.Evaluate("2.7182818284590452353602874", this).output);
        }

        public virtual bool SerializeToXml(XElement rootElement)
        {
            if(!operandStorage.SerializeToXml(rootElement, this))
                return false;

            return true;
        }

        public virtual bool DeserializeFromXml(XElement rootElement)
        {
            if(!operandStorage.DeserializeFromXml(rootElement, this))
                return false;

            return true;
        }

        public void Log(string message)
        {
            logMessageList.Add(message);
        }

        public void ClearLog()
        {
            logMessageList.Clear();
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

        public virtual Operand MakePsuedoScalar()
        {
            return new OuterProduct(ReturnBasisVectors().Select(vectorName => (Operand)new Blade(vectorName)).ToList());
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

        public virtual bool LookupVariableByName(string name, ref Operand operand)
        {
            return operandStorage.GetStorage(name, ref operand);
        }
    }

    public class EuclideanContext : Context
    {
        public int dimension;
        private Regex basisVectorRx;

        public EuclideanContext(int dimension) : base()
        {
            this.dimension = dimension;
            this.basisVectorRx = new Regex(@"^e([0-9]+)$", RegexOptions.Compiled);
        }

        public override Operand BilinearForm(string vectorNameA, string vectorNameB)
        {
            MatchCollection collectionA = this.basisVectorRx.Matches(vectorNameA);
            if (collectionA.Count != 1)
                return base.BilinearForm(vectorNameA, vectorNameB);

            MatchCollection collectionB = this.basisVectorRx.Matches(vectorNameB);
            if (collectionB.Count != 1)
                return base.BilinearForm(vectorNameA, vectorNameB);

            Match matchA = collectionA[0];
            int dimA = Int32.Parse(matchA.Groups[1].Value);

            Match matchB = collectionB[0];
            int dimB = Int32.Parse(matchB.Groups[1].Value);

            return new NumericScalar(dimA == dimB ? 1.0 : 0.0);
        }

        public override List<string> ReturnBasisVectors()
        {
            return Enumerable.Range(0, this.dimension).Select(i => $"e{i}").ToList();
        }
    }
}