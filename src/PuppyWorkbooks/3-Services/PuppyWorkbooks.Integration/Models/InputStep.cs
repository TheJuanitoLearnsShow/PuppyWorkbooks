using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public class InputStep : IntegrationStep
{
    [XmlIgnore] public virtual InputKind Kind { get; set; }
    [XmlAttribute] public string MockCsvFilePath { get; set; } = string.Empty;
    [XmlElement] public string MockCsv { get; set; } = string.Empty;
    [XmlElement] public string MockData { get; set; } = string.Empty;

    public bool ShouldSerializeMockCsvFilePath() => !string.IsNullOrWhiteSpace(MockCsvFilePath);
    public bool ShouldSerializeMockCsv() => !string.IsNullOrWhiteSpace(MockCsv);
    public bool ShouldSerializeMockData() => !string.IsNullOrWhiteSpace(MockData);
    public bool ShouldSerializeMockDataSourcesList() => MockDataSources.Count > 0;

    [XmlIgnore]
    public Dictionary<string, MockDataSource> MockDataSources { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [XmlArray("MockDataSources")]
    [XmlArrayItem("MockData", typeof(MockDataSource))]
    public List<MockDataSource> MockDataSourcesList
    {
        get => MockDataSources.Values.ToList();
        set
        {
            if (value is not null)
            {
                foreach (var item in value)
                {
                    var key = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : (!string.IsNullOrWhiteSpace(item.Key) ? item.Key : $"Mock_{MockDataSources.Count + 1}");
                    MockDataSources[key] = item;
                }
            }
        }
    }
}

public class CsvInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.CSVReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
}

public sealed class CsvInputStep : CsvInputProviderOptions
{
}

public class SqlInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.SqlReader;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionStringFromField { get; set; } = string.Empty;
    [XmlAttribute] public string TableName { get; set; } = string.Empty;
    [XmlAttribute] public string TableNameFromField { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string QueryFromField { get; set; } = string.Empty;

    public bool ShouldSerializeConnectionString() => !string.IsNullOrWhiteSpace(ConnectionString);
    public bool ShouldSerializeConnectionStringFromField() => !string.IsNullOrWhiteSpace(ConnectionStringFromField);
    public bool ShouldSerializeTableName() => !string.IsNullOrWhiteSpace(TableName);
    public bool ShouldSerializeTableNameFromField() => !string.IsNullOrWhiteSpace(TableNameFromField);
    public bool ShouldSerializeQuery() => !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeQueryFromField() => !string.IsNullOrWhiteSpace(QueryFromField);
}

public sealed class SqlInputStep : SqlInputProviderOptions
{
}

public class HttpInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.HttpReader;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string EndpointFromField { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "GET";
    [XmlAttribute] public string JsonPath { get; set; } = "$";
    [XmlAttribute] public string JsonPathFromField { get; set; } = string.Empty;
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeHttpConfiguration() => !string.IsNullOrWhiteSpace(HttpConfiguration);
    public bool ShouldSerializeEndpoint() => !string.IsNullOrWhiteSpace(Endpoint);
    public bool ShouldSerializeEndpointFromField() => !string.IsNullOrWhiteSpace(EndpointFromField);
    public bool ShouldSerializeHttpMethod() => !string.IsNullOrWhiteSpace(HttpMethod);
    public bool ShouldSerializeJsonPath() => !string.IsNullOrWhiteSpace(JsonPath);
    public bool ShouldSerializeJsonPathFromField() => !string.IsNullOrWhiteSpace(JsonPathFromField);
}

public sealed class HttpInputStep : HttpInputProviderOptions
{
}

public class JsonInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.JsonReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string JsonPath { get; set; } = "$";
    [XmlAttribute] public string JsonPathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeJsonPath() => !string.IsNullOrWhiteSpace(JsonPath);
    public bool ShouldSerializeJsonPathFromField() => !string.IsNullOrWhiteSpace(JsonPathFromField);
}

public sealed class JsonInputStep : JsonInputProviderOptions
{
}

public class XmlInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.XmlReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElementFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeXmlItemElement() => !string.IsNullOrWhiteSpace(XmlItemElement);
    public bool ShouldSerializeXmlItemElementFromField() => !string.IsNullOrWhiteSpace(XmlItemElementFromField);
}

public sealed class XmlInputStep : XmlInputProviderOptions
{
}

public class FileSystemInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.FileSystemReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public bool AddFileSizeField { get; set; }
    [XmlAttribute] public bool AddCreatedOnDate { get; set; }
    [XmlAttribute] public bool AddLastModifiedDateField { get; set; }

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeAddFileSizeField() => AddFileSizeField;
    public bool ShouldSerializeAddCreatedOnDate() => AddCreatedOnDate;
    public bool ShouldSerializeAddLastModifiedDateField() => AddLastModifiedDateField;
}

public sealed class FileSystemInputStep : FileSystemInputProviderOptions
{
}

public class MemoryInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.MemoryReader;
    [XmlIgnore] public object? Data { get; set; }
}

public sealed class MemoryInputStep : MemoryInputProviderOptions
{
}
