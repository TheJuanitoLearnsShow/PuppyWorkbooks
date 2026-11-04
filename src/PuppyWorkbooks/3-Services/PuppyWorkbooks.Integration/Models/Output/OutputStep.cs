using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Output;

public class OutputStep : IntegrationStep
{
    [XmlIgnore] public virtual OutputKind Kind { get; set; }
}