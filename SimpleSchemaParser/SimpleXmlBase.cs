using System.Xml.Linq;

namespace SimpleSchemaParser {
	public abstract class SimpleXmlBase {
		public SimpleXmlBase(XName name) {
			Name = name;
		}

		public XName Name {
			get;
		}
	}
}
