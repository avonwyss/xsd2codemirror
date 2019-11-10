using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleSchemaParser {
	public class CodeMirrorSchemaInfoSerializer {
		private readonly IEnumerable<SimpleXmlElement> elements;
		private JsonTextWriter writer;

		public bool Pretty {
			get;
			set;
		}

		/*
		 * {
		    "!top": ["top"],
		    top: {
		      attrs: {
		        lang: ["en", "de", "fr", "nl"],
		        freeform: null
		      },
		      children: ["animal", "plant"]
		    },
		    animal: {
		      attrs: {
		        name: null,
		        isduck: ["yes", "no"]
		      },
		      children: ["wings", "feet", "body", "head", "tail"]
		    },
		    plant: {
		      attrs: {name: null},
		      children: ["leaves", "stem", "flowers"]
		    },
		    wings: dummy, feet: dummy, body: dummy, head: dummy, tail: dummy,
		    leaves: dummy, stem: dummy, flowers: dummy
		  }
		 */

		public CodeMirrorSchemaInfoSerializer(IEnumerable<SimpleXmlElement> elements) {
			this.elements = elements;
		}

		public string ToJsonString() {
			using (var buffer = new StringWriter()) {
				using (writer = new JsonTextWriter(buffer)) {
					writer.Formatting = Pretty ? Formatting.Indented : Formatting.None;
					writer.WriteStartObject();
					WriteTopElements(elements.Where(e => e.IsTopLevelElement));
					foreach (var element in elements.OrderByQualifiedName()) {
						WriteElement(element);
					}
					writer.WriteEndObject();
				}
				return buffer.ToString();
			}
		}

		private void WriteTopElements(IEnumerable<SimpleXmlElement> elements) {
			if (!elements.Any()) {
				return;
			}
			writer.WritePropertyName("!top");
			writer.WriteStartArray();
			foreach (var element in elements.OrderByQualifiedName()) {
				writer.WriteValue(element.Name.ToString());
			}
			writer.WriteEndArray();
		}

		private void WriteElement(SimpleXmlElement element) {
			writer.WritePropertyName(element.Name.ToString());
			writer.WriteStartObject();
			if (element.Attributes != null && element.Attributes.Any()) {
				writer.WritePropertyName("attrs");
				writer.WriteStartObject();
				foreach (var attribute in element.Attributes.OrderByQualifiedName()) {
					writer.WritePropertyName(attribute.Name.ToString());
					if (attribute.PossibleValues == null || !attribute.PossibleValues.Any()) {
						writer.WriteNull();
					} else {
						writer.WriteStartArray();
						foreach (var value in attribute.PossibleValues.OrderBy(v => v, StringComparer.InvariantCultureIgnoreCase)) {
							writer.WriteValue(value);
						}
						writer.WriteEndArray();
					}
				}
				writer.WriteEndObject();
			}
			if (element.Children != null && element.Children.Any()) {
				writer.WritePropertyName("children");
				writer.WriteStartArray();
				foreach (var child in element.Children.OrderBy(v => v.ToString(), StringComparer.Ordinal)) {
					writer.WriteValue(child.ToString());
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
	}
}
