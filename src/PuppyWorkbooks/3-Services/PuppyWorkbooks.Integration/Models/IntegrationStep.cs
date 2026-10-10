using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlInclude(typeof(MapStep)), XmlInclude(typeof(FilterStep)), XmlInclude(typeof(ReduceStep)), XmlInclude(typeof(SwitchStep)),
 XmlInclude(typeof(InputStep)), XmlInclude(typeof(OutputStep)),
 XmlInclude(typeof(CsvInputStep)), XmlInclude(typeof(SqlInputStep)), XmlInclude(typeof(HttpInputStep)),
 XmlInclude(typeof(JsonInputStep)), XmlInclude(typeof(XmlInputStep)), XmlInclude(typeof(FileSystemInputStep)), XmlInclude(typeof(MemoryInputStep)),
 XmlInclude(typeof(CsvOutputStep)), XmlInclude(typeof(SqlOutputStep)), XmlInclude(typeof(HttpOutputStep)),
 XmlInclude(typeof(JsonOutputStep)), XmlInclude(typeof(XmlOutputStep)),
 XmlInclude(typeof(CsvInputProviderOptions)), XmlInclude(typeof(SqlInputProviderOptions)), XmlInclude(typeof(HttpInputProviderOptions)),
 XmlInclude(typeof(JsonInputProviderOptions)), XmlInclude(typeof(XmlInputProviderOptions)), XmlInclude(typeof(FileSystemInputProviderOptions)), XmlInclude(typeof(MemoryInputProviderOptions)),
 XmlInclude(typeof(CsvOutputProviderOptions)), XmlInclude(typeof(SqlOutputProviderOptions)), XmlInclude(typeof(HttpOutputProviderOptions)),
 XmlInclude(typeof(JsonOutputProviderOptions)), XmlInclude(typeof(XmlOutputProviderOptions))]
public abstract class IntegrationStep
{
    [XmlAttribute] public string Id { get; set; } = string.Empty;
    [XmlElement("Worksheet")] public WorkSheet? Worksheet { get; set; }

    // Populated by IntegrationXmlSerializer when the worksheet is loaded from
    // another XML file. It is intentionally not serialized as part of the
    // integration model.
    [XmlIgnore] public string? WorksheetPath { get; set; }
}
