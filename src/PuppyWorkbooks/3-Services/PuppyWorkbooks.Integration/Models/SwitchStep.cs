using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class SwitchStep : IntegrationStep
{
    [XmlElement("Branch")]
    public List<SwitchBranch> Branches { get; set; } = [];
}

public sealed class SwitchBranch
{
    /// Name of the worksheet cell whose value controls this branch.
    [XmlAttribute("WorkCell")]
    public string WorkCell { get; set; } = string.Empty;

    [XmlElement("Map", typeof(MapStep))]
    [XmlElement("Filter", typeof(FilterStep))]
    [XmlElement("Reduce", typeof(ReduceStep))]
    [XmlElement("Switch", typeof(SwitchStep))]
    public List<IntegrationStep> Steps { get; set; } = [];
}
