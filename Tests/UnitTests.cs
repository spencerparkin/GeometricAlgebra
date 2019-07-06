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
            string inputText = "$blah";
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
            Assert.AreEqual("a^(b^c)", outputText);
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
            string inputText = "grade((a+b)*c,grade(a,1*(2+3)))";
            Operand operand = parser.Parse(inputText);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual(inputText, outputText);
        }
    }

    [TestClass]
    public class UnitTests_Blades
    {
        [TestMethod]
        public void SortVectors()
        {
            /*EvaluationContext context = new EvaluationContext();
            Parser parser = new Parser();
            string inputText = "c^b^a";
            Operand operand = parser.Parse(inputText);
            operand = Operand.FullyEvaluate(operand, context);
            string outputText = operand.Print(Operand.Format.PARSEABLE);
            Assert.AreEqual("-1.0*a^b^c", outputText);*/
        }
    }
}
