using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using GeometricAlgebra;

namespace GAWebApp.Models
{
    public class HistoryItem
    {
        public string inputLatex;
        public string outputLatex;
        public string inputPlain;
        public string outputPlain;
        public string expression;
        public string error;
        public Operand input;
        public Operand output;

        public HistoryItem()
        {
            inputLatex = "?";
            outputLatex = "?";
            inputPlain = "?";
            outputPlain = "?";
            error = "";
            input = null;
            output = null;
        }
    }

    public class State : GeometricAlgebra.ConformalModel.Conformal3D_Context
    {
        private Regex varRx;
        public List<HistoryItem> history;
        public bool showLatex;

        public State()
        {
            this.varRx = new Regex("^h([0-9]+)$", RegexOptions.Compiled);
            this.history = new List<HistoryItem>();
            this.showLatex = true;
        }

        public override bool LookupVariableByName(string name, ref Operand operand)
        {
            MatchCollection collection = varRx.Matches(name);
            if (collection != null && collection.Count > 0)
            {
                Match match = collection[0];
                int i = history.Count - 1 - Convert.ToInt32(match.Groups[1].Value);
                if (i < 0 || i >= history.Count)
                    throw new MathException($"History variable {name} is out of range.");

                if(history[i].error.Length > 0)
                    throw new MathException($"History variable {name} references error result.");

                if(history[i].output == null)
                    history[i].output = Operand.Evaluate(history[i].outputPlain, this).output;

                operand = history[i].output.Copy();
                return true;
            }

            return base.LookupVariableByName(name, ref operand);
        }

        public void Calculate(string expression)
        {
            HistoryItem item = new HistoryItem();
            item.expression = expression;

            Task task = Task.Factory.StartNew(() => {
                var result = Operand.Evaluate(expression, this);

                item.inputLatex = result.input == null ? "" : result.input.Print(Operand.Format.LATEX, this);
                item.outputLatex = result.output == null ? "" : result.output.Print(Operand.Format.LATEX, this);
                item.inputPlain = result.input == null ? "" : result.input.Print(Operand.Format.PARSEABLE, this);
                item.outputPlain = result.output == null ? "" : result.output.Print(Operand.Format.PARSEABLE, this);
                item.input = result.input;
                item.output = result.output;
                item.error = result.error;

                item.inputLatex = item.inputLatex.Replace(" ", "&space;");
                item.outputLatex = item.outputLatex.Replace(" ", "&space;");
            });

            TimeSpan timeout = TimeSpan.FromSeconds(4.0);
            if (!task.Wait(timeout))
            {
                item.error = "Timed-out waiting for evaluation to complete.";
            }

            history.Add(item);
        }

        public override bool SerializeToXml(XElement rootElement)
        {
            if(!base.SerializeToXml(rootElement))
                return false;

            XElement historyElement = new XElement("History");

            foreach(HistoryItem item in history)
            {
                XElement entryElement = new XElement("Entry");

                entryElement.Add(new XElement("expression", item.expression));
                entryElement.Add(new XElement("inputLatex", item.inputLatex));
                entryElement.Add(new XElement("outputLatex", item.outputLatex));
                entryElement.Add(new XElement("inputPlain", item.inputPlain));
                entryElement.Add(new XElement("outputPlain", item.outputPlain));
                entryElement.Add(new XElement("error", item.error));

                historyElement.Add(entryElement);
            }

            rootElement.Add(historyElement);

            rootElement.Add(new XAttribute("showLatex", this.showLatex));

            return true;
        }

        public string SerializeToString()
        {
            XElement rootElement = new XElement("State");
            SerializeToXml(rootElement);
            return rootElement.ToString();
        }

        public override bool DeserializeFromXml(XElement rootElement)
        {
            if(!base.DeserializeFromXml(rootElement))
                return false;

            XElement historyElement = rootElement.Element("History");
            if(historyElement != null)
            {
                history.Clear();

                foreach(XElement entryElement in historyElement.Elements())
                {
                    HistoryItem item = new HistoryItem();

                    item.expression = entryElement.Element("expression").Value;
                    item.inputLatex = entryElement.Element("inputLatex").Value;
                    item.outputLatex = entryElement.Element("outputLatex").Value;
                    item.inputPlain = entryElement.Element("inputPlain").Value;
                    item.outputPlain = entryElement.Element("outputPlain").Value;
                    item.error = entryElement.Element("error").Value;

                    history.Add(item);
                }
            }

            XAttribute attr = rootElement.Attribute("showLatex");
            this.showLatex = (attr == null) ? true : (attr.Value == "1");

            return true;
        }

        public bool DeserializeFromString(string rootXml)
        {
            XElement rootElement = XElement.Parse(rootXml);
            return DeserializeFromXml(rootElement);
        }
    }
}