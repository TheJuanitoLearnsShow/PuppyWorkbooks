using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlInclude(typeof(MapStep)), XmlInclude(typeof(FilterStep)), XmlInclude(typeof(ReduceStep)),
 XmlInclude(typeof(InputStep)), XmlInclude(typeof(OutputStep))]
public abstract class IntegrationStep
{
    [XmlAttribute] public string Id { get; set; } = string.Empty;
    [XmlElement("Worksheet")] public WorkSheet? Worksheet { get; set; }
}