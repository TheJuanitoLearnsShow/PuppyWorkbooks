using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class OutputStep : IntegrationStep
{
    [XmlAttribute] public OutputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlElement] public string TableName { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
}