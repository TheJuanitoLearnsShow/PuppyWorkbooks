using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class OutputStep : IntegrationStep
{
    [XmlAttribute] public OutputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string TableName { get; set; } = string.Empty;
    [XmlAttribute] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "POST";
    [XmlAttribute] public HttpPayloadFormat PayloadFormat { get; set; } = HttpPayloadFormat.Json;
    [XmlAttribute] public string XmlRootElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElement { get; set; } = string.Empty;
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeFilePath() => Kind is OutputKind.CSVWriter or OutputKind.JsonWriter or OutputKind.XmlWriter;
    public bool ShouldSerializeConnectionString() => Kind == OutputKind.SqlWriter;
    public bool ShouldSerializeTableName() => Kind == OutputKind.SqlWriter && !string.IsNullOrWhiteSpace(TableName);
    public bool ShouldSerializeQuery() => Kind == OutputKind.SqlWriter && !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeHttpConfiguration() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializeEndpoint() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializeHttpMethod() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializePayloadFormat() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializeXmlRootElement() => Kind == OutputKind.XmlWriter;
    public bool ShouldSerializeXmlRecordElement() => Kind == OutputKind.XmlWriter;
}
