using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SimpleSchemaParser {
	public class SimpleXmlElement: SimpleXmlBase {
		private readonly Dictionary<XName, SimpleXmlAttribute> attributes = new Dictionary<XName, SimpleXmlAttribute>();
		private readonly HashSet<XName> children = new HashSet<XName>();

		public SimpleXmlElement(XName name, bool isTopLevelElement): base(name) {
			IsTopLevelElement = isTopLevelElement;
		}

		public ICollection<SimpleXmlAttribute> Attributes => attributes.Values;

		public ICollection<XName> Children => children;

		public bool IsTopLevelElement {
			get;
		}

		public bool AddAttribute(SimpleXmlAttribute attribute) {
			if (attributes.ContainsKey(attribute.Name)) {
				return false;
			}
			attributes.Add(attribute.Name, attribute);
			return true;
		}

		public bool AddChild(XName child) {
			return children.Add(child);
		}
	}
}
