using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public class OutputStep : IntegrationStep
{
    [XmlIgnore] public virtual OutputKind Kind { get; set; }
}

public class CsvOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.CSVWriter;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
}

public sealed class CsvOutputStep : CsvOutputProviderOptions
{
}

public class SqlOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.SqlWriter;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionStringFromField { get; set; } = string.Empty;
    [XmlAttribute] public string TableName { get; set; } = string.Empty;
    [XmlAttribute] public string TableNameFromField { get; set; } = string.Empty;
    [XmlAttribute] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string QueryFromField { get; set; } = string.Empty;

    public bool ShouldSerializeConnectionString() => !string.IsNullOrWhiteSpace(ConnectionString);
    public bool ShouldSerializeConnectionStringFromField() => !string.IsNullOrWhiteSpace(ConnectionStringFromField);
    public bool ShouldSerializeTableName() => !string.IsNullOrWhiteSpace(TableName);
    public bool ShouldSerializeTableNameFromField() => !string.IsNullOrWhiteSpace(TableNameFromField);
    public bool ShouldSerializeQuery() => !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeQueryFromField() => !string.IsNullOrWhiteSpace(QueryFromField);
}

public sealed class SqlOutputStep : SqlOutputProviderOptions
{
}

public class HttpOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.HttpWriter;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string EndpointFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "POST";
    [XmlAttribute] public HttpPayloadFormat PayloadFormat { get; set; } = HttpPayloadFormat.Json;
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeHttpConfiguration() => !string.IsNullOrWhiteSpace(HttpConfiguration);
    public bool ShouldSerializeEndpoint() => !string.IsNullOrWhiteSpace(Endpoint);
    public bool ShouldSerializeEndpointFromField() => !string.IsNullOrWhiteSpace(EndpointFromField);
    public bool ShouldSerializeHttpMethod() => !string.IsNullOrWhiteSpace(HttpMethod);
    public bool ShouldSerializePayloadFormat() => true;
}

public sealed class HttpOutputStep : HttpOutputProviderOptions
{
}

public class JsonOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.JsonWriter;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
}

public sealed class JsonOutputStep : JsonOutputProviderOptions
{
}

public class XmlOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.XmlWriter;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElementFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElementFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeXmlRootElement() => !string.IsNullOrWhiteSpace(XmlRootElement);
    public bool ShouldSerializeXmlRootElementFromField() => !string.IsNullOrWhiteSpace(XmlRootElementFromField);
    public bool ShouldSerializeXmlRecordElement() => !string.IsNullOrWhiteSpace(XmlRecordElement);
    public bool ShouldSerializeXmlRecordElementFromField() => !string.IsNullOrWhiteSpace(XmlRecordElementFromField);
}

public sealed class XmlOutputStep : XmlOutputProviderOptions
{
}
