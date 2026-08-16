using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class FilterStep : IntegrationStep
{
    [XmlAttribute] public bool KeepWhenTrue { get; set; } = true;
}