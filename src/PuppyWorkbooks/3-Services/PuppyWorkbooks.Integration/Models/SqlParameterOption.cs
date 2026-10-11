using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public enum SqlCommandType
{
    Text,
    StoredProcedure
}

public sealed class SqlParameterOption
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string? FromField { get; set; }

    [XmlAttribute]
    public string? FromValue { get; set; }

    public bool ShouldSerializeFromField() => FromField is not null;
    public bool ShouldSerializeFromValue() => FromValue is not null;
}
