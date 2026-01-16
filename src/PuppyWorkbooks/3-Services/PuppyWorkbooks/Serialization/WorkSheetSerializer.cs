using System.Xml.Serialization;

namespace PuppyWorkbooks.Serialization;

public class WorkSheetSerializer
{
    private readonly XmlSerializer _serializer = new (typeof(WorkSheet));

    public void SerializeToXmlFile(string filePath, WorkSheet worksheet)
    {
        using var writer = new StreamWriter(filePath);
        _serializer.Serialize(writer, worksheet);
    }
    public WorkSheet DeserializeFromXmlFile(string filePath)
    {
        using var stringReader = new StreamReader(filePath);
        return (WorkSheet)_serializer.Deserialize(stringReader)!;
    }
}