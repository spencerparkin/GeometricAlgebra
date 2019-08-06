using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GeometricAlgebra;

namespace Tests
{
    [TestClass]
    public class UnitTests_Parsing
    {
        [TestMethod]
        public void ParseNumber()
        {
            Parser parser = new Parser();
            string inputText = "1234.5678";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }

        [TestMethod]
        public void ParseSymbolicVector()
        {
            Parser parser = new Parser();
            string inputText = "blah";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }

        [TestMethod]
        public void ParseSymbolicVariable()
        {
            Parser parser = new Parser();
            string inputText = "@blah";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }

        [TestMethod]
        public void ParseBlade()
        {
            Parser parser = new Parser();
            string inputText = "a^b^c";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual("(a^b)^c", outputText);
        }

        [TestMethod]
        public void ParseSubexpression()
        {
            Parser parser = new Parser();
            string inputText = "a*(b + c)";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }

        [TestMethod]
        public void ParseFunction()
        {
            Parser parser = new Parser();
            string inputText = "grade((a + b)*c, grade(a, 1*(2 + 3)))";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }
    }

    [TestClass]
    public class UnitTests_Scalars
    {
        [TestMethod]
        public void Distribute()
        {
            Context context = new Context();
            string inputText = "($a*($b + $c))*(x^y)";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("($a*$b + $a*$c)*x^y", outputText);
        }

        [TestMethod]
        public void Arithmetic()
        {
            Context context = new Context();
            string inputText = "3*(2+1)";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("9", outputText);
        }

        [TestMethod]
        public void Divide()
        {
            Context context = new Context();
            string inputText = "1/(1+1)";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0.5", outputText);
        }

        [TestMethod]
        public void ConformalPoint()
        {
            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();
            Operand.Evaluate("@v = $x*e1 + $y*e2 + $z*e3", context);
            Operand.Evaluate("@p = no + @v + 0.5*(@v.@v)*ni", context);
            Operand operand = Operand.Evaluate("@p.@p", context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0", outputText);
        }
    }

    [TestClass]
    public class UnitTests_Blades
    {
        [TestMethod]
        public void Subtract()
        {
            Context context = new Context();
            string inputText = "a - b";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a + (-1)*b", outputText);
        }

        [TestMethod]
        public void SortVectors()
        {
            Context context = new Context();
            string inputText = "c^b^a";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("(-1)*a^b^c", outputText);
        }

        [TestMethod]
        public void LinearlyDependentVectors()
        {
            Context context = new Context();
            string inputText = "c^b^a^c";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0", outputText);
        }

        [TestMethod]
        public void BladeReverse()
        {
            Context context = new Context();
            string inputText = "rev(a^b^c)";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("(-1)*a^b^c", outputText);
        }

        // TODO: Test blade inverse.

        [TestMethod]
        public void BladeCancellation()
        {
            Context context = new Context();
            string inputText = "a^b + b^a";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0", outputText);
        }
    }

    [TestClass]
    public class UnitTests_Variables
    {
        [TestMethod]
        public void StorageAndRetrieval()
        {
            Context context = new Context();
            string inputText = "@a = a^b";
            Operand operand = Operand.Evaluate(inputText, context).output;
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a^b", outputText);
            inputText = "@a";
            operand = Operand.Evaluate(inputText, context).output;
            outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a^b", outputText);
        }
    }

    [TestClass]
    public class UnitTests_Identities
    {
        [TestMethod]
        public void Case1()
        {
            // (a^b)*@i = (a.(b*@i))
            // 0.5*(a*b - b*a)*@i = 0.5*(a*b*@i - b*@i*a)
        }

        [TestMethod]
        public void Case2()
        {
            // This expression is causing the evaluator to either loop way too long, or perhaps forever!  :(
            // (a^b)*(reverse(a^b)*inverse((a^b).(a^b)))
        }
    }

    [TestClass]
    public class UnitTests_Inverse
    {
        [TestMethod]
        public void Case1()
        {
            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();
            Operand.Evaluate("@multivector = 5 - e1 + 2 * e2 + 7 * e1 ^ e3", context);
            Operand.Evaluate("@multivectorInv = inv(@multivector)", context);
            Operand result = Operand.Evaluate("trim(@multivector * @multivectorInv - 1)", context).output;
            Assert.IsTrue(result.IsAdditiveIdentity);
        }

        [TestMethod]
        public void Case2()
        {
        }

        [TestMethod]
        public void MatrixInverse()
        {
            Context context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();
            Result result = Operand.Evaluate("inv([[$a, $b, $c],[$d, $e, $f],[$g, $h, $i]])", context);
            Assert.IsTrue(result.error == "");
        }
    }
}
