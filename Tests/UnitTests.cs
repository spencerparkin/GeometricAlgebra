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
            string inputText = "grade((a + b)*c,grade(a,1*(2 + 3)))";
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
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "($a*($b + $c))*(x^y)";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("($a*$b + $a*$c)*x^y", outputText);
        }

        [TestMethod]
        public void Arithmetic()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "3*(2+1)";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("9", outputText);
        }

        [TestMethod]
        public void Divide()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "1/(1+1)";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0.5", outputText);
        }

        [TestMethod]
        public void ConformalPoint()
        {
            EvaluationContext context = new GeometricAlgebra.ConformalModel.Conformal3D_EvaluationContext();
            Operand.FullyEvaluate("@v = $x*e1 + $y*e2 + $z*e3", context);
            Operand.FullyEvaluate("@p = no + @v + 0.5*(@v.@v)*ni", context);
            Operand operand = Operand.FullyEvaluate("@p.@p", context);
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
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "a - b";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a + (-1)*b", outputText);
        }

        [TestMethod]
        public void SortVectors()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "c^b^a";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("(-1)*a^b^c", outputText);
        }

        [TestMethod]
        public void LinearlyDependentVectors()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "c^b^a^c";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("0", outputText);
        }

        [TestMethod]
        public void BladeReverse()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "reverse(a^b^c)";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("(-1)*a^b^c", outputText);
        }

        // TODO: Test blade inverse.

        [TestMethod]
        public void BladeCancellation()
        {
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "a^b + b^a";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
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
            EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "@a = a^b";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a^b", outputText);
            inputText = "@a";
            operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            outputText = operand.Print(Operand.Format.PARSEABLE, context);
            Assert.AreEqual("a^b", outputText);
        }
    }
}
