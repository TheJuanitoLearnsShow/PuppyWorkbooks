using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class OutputStep : IntegrationStep
{
    [XmlAttribute] public OutputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionStringFromField { get; set; } = string.Empty;
    [XmlAttribute] public string TableName { get; set; } = string.Empty;
    [XmlAttribute] public string TableNameFromField { get; set; } = string.Empty;
    [XmlAttribute] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string QueryFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string EndpointFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "POST";
    [XmlAttribute] public HttpPayloadFormat PayloadFormat { get; set; } = HttpPayloadFormat.Json;
    [XmlAttribute] public string JsonPath { get; set; } = string.Empty;
    [XmlAttribute] public string JsonPathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElementFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElementFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElementFromField { get; set; } = string.Empty;
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeFilePath() => (Kind is OutputKind.CSVWriter or OutputKind.JsonWriter or OutputKind.XmlWriter) && !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeConnectionString() => Kind == OutputKind.SqlWriter && !string.IsNullOrWhiteSpace(ConnectionString);
    public bool ShouldSerializeConnectionStringFromField() => !string.IsNullOrWhiteSpace(ConnectionStringFromField);
    public bool ShouldSerializeTableName() => Kind == OutputKind.SqlWriter && !string.IsNullOrWhiteSpace(TableName);
    public bool ShouldSerializeTableNameFromField() => !string.IsNullOrWhiteSpace(TableNameFromField);
    public bool ShouldSerializeQuery() => Kind == OutputKind.SqlWriter && !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeQueryFromField() => !string.IsNullOrWhiteSpace(QueryFromField);
    public bool ShouldSerializeHttpConfiguration() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializeEndpoint() => Kind == OutputKind.HttpWriter && !string.IsNullOrWhiteSpace(Endpoint);
    public bool ShouldSerializeEndpointFromField() => !string.IsNullOrWhiteSpace(EndpointFromField);
    public bool ShouldSerializeHttpMethod() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializePayloadFormat() => Kind == OutputKind.HttpWriter;
    public bool ShouldSerializeJsonPath() => !string.IsNullOrWhiteSpace(JsonPath);
    public bool ShouldSerializeJsonPathFromField() => !string.IsNullOrWhiteSpace(JsonPathFromField);
    public bool ShouldSerializeXmlItemElement() => !string.IsNullOrWhiteSpace(XmlItemElement);
    public bool ShouldSerializeXmlItemElementFromField() => !string.IsNullOrWhiteSpace(XmlItemElementFromField);
    public bool ShouldSerializeXmlRootElement() => Kind == OutputKind.XmlWriter && !string.IsNullOrWhiteSpace(XmlRootElement);
    public bool ShouldSerializeXmlRootElementFromField() => !string.IsNullOrWhiteSpace(XmlRootElementFromField);
    public bool ShouldSerializeXmlRecordElement() => Kind == OutputKind.XmlWriter && !string.IsNullOrWhiteSpace(XmlRecordElement);
    public bool ShouldSerializeXmlRecordElementFromField() => !string.IsNullOrWhiteSpace(XmlRecordElementFromField);
}
