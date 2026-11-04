using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class SqlInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.SqlReader;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionStringFromField { get; set; } = string.Empty;
    [XmlAttribute] public string TableName { get; set; } = string.Empty;
    [XmlAttribute] public string TableNameFromField { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string QueryFromField { get; set; } = string.Empty;
    [XmlAttribute] public SqlCommandType CommandType { get; set; } = SqlCommandType.Text;
    [XmlElement("SqlParam")] public List<SqlParameterOption> SqlParams { get; set; } = [];

    public bool ShouldSerializeConnectionString() => !string.IsNullOrWhiteSpace(ConnectionString);
    public bool ShouldSerializeConnectionStringFromField() => !string.IsNullOrWhiteSpace(ConnectionStringFromField);
    public bool ShouldSerializeTableName() => !string.IsNullOrWhiteSpace(TableName);
    public bool ShouldSerializeTableNameFromField() => !string.IsNullOrWhiteSpace(TableNameFromField);
    public bool ShouldSerializeQuery() => !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeQueryFromField() => !string.IsNullOrWhiteSpace(QueryFromField);
    public bool ShouldSerializeCommandType() => CommandType != SqlCommandType.Text;
}