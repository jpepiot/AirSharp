using System;
using System.Collections.Generic;
using System.Linq;

namespace AirSharp {
    using System.Globalization;
    using System.Xml.Linq;

    internal class PropertyList : Dictionary<string, object> {

        public PropertyList() {
        }

        public PropertyList(string pListContent) {
            Load(pListContent);
        }

        public void Load(string content) {
            Clear();
            XDocument doc = XDocument.Parse(content);
            XElement plist = doc.Element("plist");
            if (plist != null) {
                XElement dict = plist.Element("dict");
                if (dict != null) {
                    var dictElements = dict.Elements();
                    Parse(this, dictElements);
                }
            }
        }

        private static void Parse(PropertyList dict, IEnumerable<XElement> elements) {
            for (int i = 0; i < elements.Count(); i += 2) {
                XElement key = elements.ElementAt(i);
                XElement val = elements.ElementAt(i + 1);
                dict[key.Value] = ParseValue(val);
            }
        }

        private static List<object> ParseArray(IEnumerable<XElement> elements) {
            return elements.Select(ParseValue).ToList();
        }

        private static object ParseValue(XElement val) {
            switch (val.Name.ToString()) {
                case "string":
                    return val.Value;
                case "integer":
                    return long.Parse(val.Value);
                case "real":
                    return float.Parse(val.Value, CultureInfo.InvariantCulture);
                case "date":
                    return DateTime.Parse(val.Value);
                case "true":
                    return true;
                case "false":
                    return false;
                case "dict":
                    PropertyList plist = new PropertyList();
                    Parse(plist, val.Elements());
                    return plist;
                case "array":
                    List<dynamic> list = ParseArray(val.Elements());
                    return list;
                default:
                    throw new NotSupportedException(val.Name + " type is not supported");
            }
        }
    }
}
