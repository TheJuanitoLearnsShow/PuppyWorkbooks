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
    [XmlElement("CsvInput", typeof(CsvInputStep))]
    [XmlElement("SqlInput", typeof(SqlInputStep))]
    [XmlElement("HttpInput", typeof(HttpInputStep))]
    [XmlElement("JsonInput", typeof(JsonInputStep))]
    [XmlElement("XmlInput", typeof(XmlInputStep))]
    [XmlElement("FileSystemInput", typeof(FileSystemInputStep))]
    [XmlElement("MemoryInput", typeof(MemoryInputStep))]
    [XmlElement("IOInput", typeof(InputStep))]
    [XmlElement("CsvOutput", typeof(CsvOutputStep))]
    [XmlElement("SqlOutput", typeof(SqlOutputStep))]
    [XmlElement("HttpOutput", typeof(HttpOutputStep))]
    [XmlElement("JsonOutput", typeof(JsonOutputStep))]
    [XmlElement("XmlOutput", typeof(XmlOutputStep))]
    [XmlElement("IOOutput", typeof(OutputStep))]
    public List<IntegrationStep> Steps { get; set; } = [];
}
