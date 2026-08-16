using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

[XmlRoot("Integration")]
public sealed class IntegrationDefinition
{
    [XmlAttribute] public string Name { get; set; } = string.Empty;
    [XmlArray("Steps")]
    [XmlArrayItem("Map", typeof(MapStep))]
    [XmlArrayItem("Filter", typeof(FilterStep))]
    [XmlArrayItem("Reduce", typeof(ReduceStep))]
    [XmlArrayItem("IOInput", typeof(InputStep))]
    [XmlArrayItem("IOOutput", typeof(OutputStep))]
    public List<IntegrationStep> Steps { get; set; } = [];
}

[XmlInclude(typeof(MapStep)), XmlInclude(typeof(FilterStep)), XmlInclude(typeof(ReduceStep)),
 XmlInclude(typeof(InputStep)), XmlInclude(typeof(OutputStep))]
public abstract class IntegrationStep
{
    [XmlAttribute] public string Id { get; set; } = string.Empty;
    [XmlElement("Worksheet")] public WorkSheet? Worksheet { get; set; }
}

public sealed class MapStep : IntegrationStep
{
    [XmlAttribute] public string OutputField { get; set; } = string.Empty;
}

public sealed class FilterStep : IntegrationStep
{
    [XmlAttribute] public bool KeepWhenTrue { get; set; } = true;
}

public sealed class ReduceStep : IntegrationStep
{
    /// JSON object used as the initial accumulator state. The worksheet receives it as `State`.
    [XmlElement] public string InitialStateJson { get; set; } = "{}";
    [XmlAttribute] public string OutputField { get; set; } = "State";
}

public sealed class InputStep : IntegrationStep
{
    [XmlAttribute] public InputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
}

public sealed class OutputStep : IntegrationStep
{
    [XmlAttribute] public OutputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlElement] public string TableName { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
}

public enum InputKind { CSVReader, SqlReader }
public enum OutputKind { CSVWriter, SqlWriter }

public sealed record IntegrationRecord(Dictionary<string, object?> Values)
{
    public object? this[string key] { get => Values.TryGetValue(key, out var value) ? value : null; set => Values[key] = value; }
}

public sealed record OutputStatus(bool Succeeded, string Message, int AffectedRows = 1)
{
    public static OutputStatus Success(string message, int rows = 1) => new(true, message, rows);
}

public sealed record IntegrationResult(long Read, long Written, long Excluded, IntegrationRecord? FinalState);
