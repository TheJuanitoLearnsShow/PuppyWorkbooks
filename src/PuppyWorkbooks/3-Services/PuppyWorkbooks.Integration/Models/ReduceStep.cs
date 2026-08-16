using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class ReduceStep : IntegrationStep
{
    /// JSON object used as the initial accumulator state. The worksheet receives it as `State`.
    [XmlElement] public string InitialStateJson { get; set; } = "{}";
    [XmlAttribute] public string OutputField { get; set; } = "State";
}