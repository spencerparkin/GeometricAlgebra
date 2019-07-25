using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GeometricAlgebra
{
    public class OperandStorage
    {
        private Dictionary<string, Operand> operandMap;

        public OperandStorage()
        {
            operandMap = new Dictionary<string, Operand>();
        }

        public bool SerializeToXml(XElement rootElement, Context context)
        {
            XElement storageElement = new XElement("Storage");

            foreach (var pair in this.operandMap)
            {
                XElement entryElement = new XElement("Entry");

                entryElement.Add(new XElement("key", pair.Key));
                entryElement.Add(new XElement("value", pair.Value.Print(Operand.Format.PARSEABLE, context)));

                storageElement.Add(entryElement);
            }

            rootElement.Add(storageElement);

            return true;
        }

        public bool DeserializeFromXml(XElement rootElement, Context context)
        {
            XElement storageElement = rootElement.Element("Storage");
            if (storageElement == null)
                return false;
            
            foreach (XElement entryElement in storageElement.Elements())
            {
                if (entryElement.Name == "Entry")
                {
                    string key = entryElement.Element("key").Value;
                    string value = entryElement.Element("value").Value;

                    SetStorage(key, Operand.Evaluate(value, context).output);
                }
            }

            return true;
        }

        public void ClearStorage(string key)
        {
            if (operandMap.ContainsKey(key))
                operandMap.Remove(key);
        }

        public void SetStorage(string key, Operand operand)
        {
            ClearStorage(key);

            if (operand != null)
                operandMap.Add(key, operand.Copy());
        }

        public bool GetStorage(string key, ref Operand operand)
        {
            if (!operandMap.ContainsKey(key))
                return false;

            operand = operandMap[key].Copy();
            return true;
        }
    }
}
