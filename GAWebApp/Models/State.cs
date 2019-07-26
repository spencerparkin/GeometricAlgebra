using System;
using System.Collections.Generic;
using System.Linq;
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

        public HistoryItem()
        {
            inputLatex = "?";
            outputLatex = "?";
            inputPlain = "?";
            outputPlain = "?";
            error = "";
        }
    }

    public class State
    {
        public Context context;
        public List<HistoryItem> history;
        public bool showLatex;

        public State()
        {
            context = new GeometricAlgebra.ConformalModel.Conformal3D_Context();
            history = new List<HistoryItem>();
            showLatex = true;
        }

        public void Calculate(string expression)
        {
            HistoryItem item = new HistoryItem();

            // TODO: We should probably do this in a task and then wait with time-out.
            var result = Operand.Evaluate(expression, context);

            item.expression = expression;
            item.inputLatex = result.input == null ? "" : result.input.Print(Operand.Format.LATEX, context);
            item.outputLatex = result.output == null ? "" : result.output.Print(Operand.Format.LATEX, context);
            item.inputPlain = result.input == null ? "" : result.input.Print(Operand.Format.PARSEABLE, context);
            item.outputPlain = result.output == null ? "" : result.output.Print(Operand.Format.PARSEABLE, context);
            item.error = result.error;

            // TODO: Spaces are needed in some places for valid Latex, and this code is breaking that latex.
            //       This is needed, however, to overcome the URI encoding problem.  Is there a latex service
            //       we can call in a way where we pass the latex string in POST form rather than GET?
            item.inputLatex = item.inputLatex.Replace(" ", "");
            item.outputLatex = item.outputLatex.Replace(" ", "");

            history.Add(item);
        }

        public bool SerializeToXml(XElement rootElement)
        {
            if(!context.SerializeToXml(rootElement))
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

            return false;
        }

        public bool DeserializeFromXml(XElement rootElement)
        {
            if(!context.DeserializeFromXml(rootElement))
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

            return false;
        }
    }
}
