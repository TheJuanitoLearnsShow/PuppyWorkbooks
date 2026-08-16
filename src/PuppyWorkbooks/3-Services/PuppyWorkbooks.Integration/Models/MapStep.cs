using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class MapStep : IntegrationStep
{
    [XmlAttribute] public string OutputField { get; set; } = string.Empty;
}