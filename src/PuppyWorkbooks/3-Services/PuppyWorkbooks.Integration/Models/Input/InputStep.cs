using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

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