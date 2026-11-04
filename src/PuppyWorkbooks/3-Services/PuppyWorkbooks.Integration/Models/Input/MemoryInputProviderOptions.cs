using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class MemoryInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.MemoryReader;
    [XmlIgnore] public object? Data { get; set; }
}