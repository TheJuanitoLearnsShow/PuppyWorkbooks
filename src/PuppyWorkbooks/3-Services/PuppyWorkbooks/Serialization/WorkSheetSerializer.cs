using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace PuppyWorkbooks.Serialization;

public class WorkSheetSerializer
{
    private readonly XmlSerializer _serializer = new (typeof(WorkSheet));
    private readonly JsonSerializerOptions _jsonOptions = new() {   };

    public void SerializeToXmlFile(string filePath, WorkSheet worksheet)
    {
        using var writer = new StreamWriter(filePath);
        _serializer.Serialize(writer, worksheet);
    }
    public WorkSheet DeserializeFromXmlFile(string filePath)
    {
        using var stringReader = new StreamReader(filePath);
        var worksheet = (WorkSheet)_serializer.Deserialize(stringReader)!;
        LoadVariables(worksheet, File.ReadAllText(filePath));
        return worksheet;
    }

    private static void LoadVariables(WorkSheet worksheet, string xml)
    {
        var variables = XDocument.Parse(xml).Root?.Element("Variables")?.Elements("Variable") ?? [];
        foreach (var variable in variables)
        {
            var key = variable.Element("Key")?.Value;
            if (!string.IsNullOrWhiteSpace(key))
                worksheet.Variables[key] = variable.Element("Value")?.Value ?? string.Empty;
        }
    }
    
    public void SerializeToJsonFile(string filePath, WorkSheet worksheet)
    {
        var json = JsonSerializer.Serialize(worksheet, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    public WorkSheet DeserializeFromJsonFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<WorkSheet>(json, _jsonOptions) ?? new WorkSheet();
    }
}
