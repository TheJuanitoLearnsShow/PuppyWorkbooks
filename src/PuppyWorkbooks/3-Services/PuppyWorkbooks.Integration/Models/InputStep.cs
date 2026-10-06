using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class InputStep : IntegrationStep
{
    [XmlAttribute] public InputKind Kind { get; set; }
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string ConnectionString { get; set; } = string.Empty;
    [XmlElement] public string Query { get; set; } = string.Empty;
    [XmlAttribute] public string MockCsvFilePath { get; set; } = string.Empty;
    [XmlElement] public string MockCsv { get; set; } = string.Empty;
    [XmlElement] public string MockData { get; set; } = string.Empty;
    [XmlAttribute] public string HttpConfiguration { get; set; } = string.Empty;
    [XmlAttribute] public string Endpoint { get; set; } = string.Empty;
    [XmlAttribute] public string HttpMethod { get; set; } = "GET";
    [XmlAttribute] public string JsonPath { get; set; } = "$";
    [XmlAttribute] public string XmlItemElement { get; set; } = string.Empty;
    [XmlAttribute] public bool AddFileSizeField { get; set; }
    [XmlAttribute] public bool AddCreatedOnDate { get; set; }
    [XmlAttribute] public bool AddLastModifiedDateField { get; set; }
    [XmlIgnore] public HttpProviderSettings? ResolvedHttpConfiguration { get; set; }

    public bool ShouldSerializeFilePath() => Kind is InputKind.CSVReader or InputKind.JsonReader or InputKind.XmlReader or InputKind.FileSystemReader;
    public bool ShouldSerializeConnectionString() => Kind == InputKind.SqlReader;
    public bool ShouldSerializeQuery() => Kind == InputKind.SqlReader && !string.IsNullOrWhiteSpace(Query);
    public bool ShouldSerializeMockCsvFilePath() => !string.IsNullOrWhiteSpace(MockCsvFilePath);
    public bool ShouldSerializeHttpConfiguration() => Kind == InputKind.HttpReader;
    public bool ShouldSerializeEndpoint() => Kind == InputKind.HttpReader;
    public bool ShouldSerializeHttpMethod() => Kind == InputKind.HttpReader;
    public bool ShouldSerializeJsonPath() => Kind is InputKind.HttpReader or InputKind.JsonReader;
    public bool ShouldSerializeXmlItemElement() => Kind == InputKind.XmlReader;
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
