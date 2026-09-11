using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlRoot("Integration")]
public sealed class IntegrationDefinition
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlArray("HttpConfigurations")]
    [XmlArrayItem("HttpConfiguration")]
    public List<HttpProviderSettings> HttpConfigurations { get; set; } = [];
    [XmlArray("Steps")]
    [XmlArrayItem("Map", typeof(MapStep))]
    [XmlArrayItem("Filter", typeof(FilterStep))]
    [XmlArrayItem("Reduce", typeof(ReduceStep))]
    [XmlArrayItem("Switch", typeof(SwitchStep))]
    [XmlArrayItem("IOInput", typeof(InputStep))]
    [XmlArrayItem("IOOutput", typeof(OutputStep))]
    public List<IntegrationStep> Steps { get; set; } = [];
}
