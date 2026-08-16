using System.Xml.Serialization;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration;

public sealed class IntegrationXmlSerializer
{
    private readonly XmlSerializer _serializer = new(typeof(IntegrationDefinition));

    public IntegrationDefinition Deserialize(string xml)
    {
        using var reader = new StringReader(xml);
        return (IntegrationDefinition)_serializer.Deserialize(reader)!;
    }

    public IntegrationDefinition DeserializeFile(string path) => Deserialize(File.ReadAllText(path));

    public string Serialize(IntegrationDefinition definition)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        _serializer.Serialize(writer, definition);
        return writer.ToString();
    }
}
