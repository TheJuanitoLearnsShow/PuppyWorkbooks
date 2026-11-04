using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Output;

public class MemoryOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.MemoryWriter;
    [XmlIgnore] public Action<IntegrationRecord>? Action { get; set; }
}