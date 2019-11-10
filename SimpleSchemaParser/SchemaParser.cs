using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace SimpleSchemaParser {
	/// <summary>
	/// The SchemaParser can parse a schema into simple element definitions. This class
	/// uses <see cref="System.Xml.Schema.XmlSchemaSet"/> to iterate over all the toplevel element
	/// and recursively parses them.
	/// 
	/// The parser doesn't support xml elements occuring in multiple contexts. The first occurence of the
	/// element will be output, other occurences will be ignored.
	/// </summary>
	public class SchemaParser {
		private readonly Dictionary<XName, SimpleXmlElement> elements = new Dictionary<XName, SimpleXmlElement>();

		private readonly string schemaPath;

		private ILogger log = NullLogger.Instance;
		private XmlSchemaSet schemaSet;

		public SchemaParser(string schemaPath) {
			this.schemaPath = schemaPath;
		}

		public ILogger Logger {
			get => log;
			set => log = value ?? NullLogger.Instance;
		}

		public string TargetNamespace {
			get;
			set;
		}

		/// <summary>
		/// Compiles the schema. Includes are assumed to be relative to the schemaPath provided. If they cannot be read,
		/// an exception will only be thrown if an element from the included resource is used.
		/// A missing file itself is ignored by the XmlSchemaSet class.
		/// </summary>
		public void Compile() {
			try {
				schemaSet = new XmlSchemaSet();
				var xsdPath = new FileInfo(schemaPath);
				schemaSet.Add(TargetNamespace, new Uri(xsdPath.FullName).LocalPath);
				log.WriteLine("Schema read...");
				schemaSet.Compile();
				log.WriteLine("Schema compiled...");
			} catch (Exception e) {
				log.WriteLine("Could not compile schema: {0}: {1}", e.GetType().Name, e.Message);
				throw new InvalidOperationException(string.Format("Could not compile schema: {0}: {1}", e.GetType().Name, e.Message), e);
			}
		}

		private string GetParticleDesc(XmlSchemaParticle particle) {
			var desc = particle.GetType().Name.Replace("XmlSchema", "");
			if (particle is XmlSchemaElement) {
				desc += "("+((XmlSchemaElement)particle).QualifiedName+")";
			}
			if (particle.SourceUri == null) {
				if (particle.Id != null) {
					return string.Format("{0}:id:{1}", desc, particle.Id);
				}
				return string.Format("{0}:{1}:{2}", desc, particle.LineNumber, particle.LinePosition);
			} else {
				var segments = new Uri(particle.SourceUri).Segments;
				return string.Format("{0}:{1}:{2}:{3}", segments[segments.Length-1], desc, particle.LineNumber, particle.LinePosition);
			}
		}

		public IEnumerable<SimpleXmlElement> GetXmlElements() {
			if (schemaSet == null) {
				throw new InvalidOperationException("Schema is not compiled yet.");
			}
			Queue<XmlSchemaElement> pending = new Queue<XmlSchemaElement>(schemaSet.GlobalElements.Values.Cast<XmlSchemaElement>());
			int topLevelCount = pending.Count;
			while (pending.Count > 0) {
				var schemaElement = pending.Dequeue();
				if (!elements.ContainsKey(schemaElement.QualifiedName.ToXName())) {
					var isTopLevelElement = elements.Count < topLevelCount;
					var element = ParseElement(schemaElement, isTopLevelElement, pending);
					elements.Add(element.Name, element);
				}
			}
			return elements.Values;
		}

		private SimpleXmlElement ParseElement(XmlSchemaElement schemaElement, bool isTopLevelElement, Queue<XmlSchemaElement> pending) {
			var element = new SimpleXmlElement(schemaElement.QualifiedName.ToXName(), isTopLevelElement);
			if (schemaElement.ElementSchemaType is XmlSchemaComplexType type) {
				using (log.Indent()) {
					log.WriteLine("Attributes");
					using (log.Indent()) {
						foreach (XmlSchemaAttribute attribute in type.AttributeUses.Values) {
							element.AddAttribute(ParseAttribute(attribute));
							log.WriteLine("{0}", attribute.QualifiedName.Name);
						}
					}
					var particle = type.ContentTypeParticle;
					if (particle != null) {
						log.WriteLine("Child Particle {0}", GetParticleDesc(particle));
						using (log.Indent()) {
							switch (particle) {
							case XmlSchemaGroupRef _:
							case XmlSchemaGroupBase _:
								foreach (var childSchemaElement in ParseGroupBase((particle as XmlSchemaGroupBase) ?? ((XmlSchemaGroupRef)particle).Particle)) {
									pending.Enqueue(childSchemaElement);
									element.Children.Add(childSchemaElement.QualifiedName.ToXName());
								}
								break;
							default: {
								if (particle.GetType().Name != "EmptyParticle") {
									throw new NotImplementedException(particle.GetType().Name);
								}
								break;
							}
							}
						}
					}
				}
			}
			return element;
		}

		private SimpleXmlAttribute ParseAttribute(XmlSchemaAttribute attribute) {
			var simpleAttribute = new SimpleXmlAttribute(attribute.QualifiedName.ToXName());
			var type = attribute.AttributeSchemaType;
			if (type != null && type.Content is XmlSchemaSimpleTypeRestriction) {
				var restriction = (XmlSchemaSimpleTypeRestriction)type.Content;
				foreach (var facet in restriction.Facets) {
					if (facet is XmlSchemaEnumerationFacet) {
						simpleAttribute.AddPossibleValue(((XmlSchemaEnumerationFacet)facet).Value);
					}
				}
			}
			return simpleAttribute;
		}

		/// <summary>
		/// Parses xs:sequence and xs:choice elements in the schema
		/// </summary>
		/// <param name="group"></param>
		/// <returns>A list of direct children elements references</returns>
		private IEnumerable<XmlSchemaElement> ParseGroupBase(XmlSchemaGroupBase group) {
			log.WriteLine("Parsing group {0}", GetParticleDesc(group));
			using (log.Indent()) {
				return ParseGroupBaseInternal(group, new HashSet<XmlSchemaGroupBase>());
			}
		}

		private IEnumerable<XmlSchemaElement> ParseGroupBaseInternal(XmlSchemaGroupBase group, HashSet<XmlSchemaGroupBase> processed) {
			if (!processed.Add(@group)) {
				yield break;
			}
			using (log.Indent()) {
				foreach (var particle in @group.Items) {
					switch (particle) {
					case XmlSchemaGroupBase subGroup: {
						foreach (var result in ParseGroupBaseInternal(subGroup, processed)) {
							yield return result;
						}
						break;
					}
					case XmlSchemaElement element:
						yield return element;
						break;
					case XmlSchemaAny _:
						break;
					default:
						throw new NotImplementedException(particle.GetType().Name);
					}
				}
			}
		}

		/// <summary>
		/// Parses xs:group ref="..." elements in the schema
		/// </summary>
		/// <param name="groupRef"></param>
		/// <returns></returns>
		private IEnumerable<XmlSchemaElement> ParseGroupRef(XmlSchemaGroupRef groupRef) {
			log.WriteLine("Parsing groupRef {0}", groupRef.RefName);
			using (log.Indent()) {
				return ParseGroupBase(groupRef.Particle);
			}
		}
	}
}
