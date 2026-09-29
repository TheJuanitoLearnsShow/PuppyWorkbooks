using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class MockDataSource
{
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
    public string Content { get; set; } = string.Empty;

    [XmlIgnore]
    public string RawText
    {
        get => Content;
        set => Content = value;
    }
}
