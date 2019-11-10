using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SimpleSchemaParser {
	internal static class Extensions {
		public static XName ToXName(this XmlQualifiedName that) {
			return XName.Get(that.Name, that.Namespace);
		}

		public static string ToQualifiedNameString(this SimpleXmlBase that) {
			return that.Name.ToString();
		}

		public static IEnumerable<T> OrderByQualifiedName<T>(this IEnumerable<T> that) where T: SimpleXmlBase {
			return that.OrderBy(ToQualifiedNameString);
		}
	}
}
