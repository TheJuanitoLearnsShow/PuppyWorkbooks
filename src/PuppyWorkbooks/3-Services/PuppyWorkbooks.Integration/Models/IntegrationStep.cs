using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlInclude(typeof(MapStep)), XmlInclude(typeof(FilterStep)), XmlInclude(typeof(ReduceStep)), XmlInclude(typeof(SwitchStep)),
 XmlInclude(typeof(InputStep)), XmlInclude(typeof(OutputStep))]
public abstract class IntegrationStep
{
    [XmlAttribute] public string Id { get; set; } = string.Empty;
    [XmlElement("Worksheet")] public WorkSheet? Worksheet { get; set; }

    // Populated by IntegrationXmlSerializer when the worksheet is loaded from
    // another XML file. It is intentionally not serialized as part of the
    // integration model.
    [XmlIgnore] public string? WorksheetPath { get; set; }
}
