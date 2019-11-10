using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SimpleSchemaParser {
	public class SimpleXmlAttribute: SimpleXmlBase {
		private readonly HashSet<string> possibleValues = new HashSet<string>();

		public SimpleXmlAttribute(XName name): base(name) { }

		public ICollection<string> PossibleValues => possibleValues;

		public bool AddPossibleValue(string possibleValue) {
			return possibleValues.Add(possibleValue);
		}
	}
}
