using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class MockDataSource
{
    private string _content = string.Empty;

    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlIgnore]
    public string Key
    {
        get => Name;
        set
        {
            if (string.IsNullOrWhiteSpace(Name))
                Name = value;
        }
    }

    [XmlAttribute]
    public string FilePath { get; set; } = string.Empty;

    [XmlText]
    public string Content
    {
        get => _content;
        set => _content = value;
    }

    [XmlIgnore]
    public string RawText
    {
        get => _content;
        set => _content = value;
    }
}
