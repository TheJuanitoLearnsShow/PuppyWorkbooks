namespace PuppyWorkbooks.Integration.Models;

public enum OutputKind { CSVWriter, SqlWriter, HttpWriter, JsonWriter, XmlWriter, MemoryWriter }

public static class OutputKindXmlNames
{
    public static string GetElementName(this OutputKind kind) => kind switch
    {
        OutputKind.CSVWriter => "CsvOutput",
        OutputKind.SqlWriter => "SqlOutput",
        OutputKind.HttpWriter => "HttpOutput",
        OutputKind.JsonWriter => "JsonOutput",
        OutputKind.XmlWriter => "XmlOutput",
        OutputKind.MemoryWriter => "MemoryOutput",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported output provider kind.")
    };

    public static bool TryParseElementName(string elementName, out OutputKind kind)
    {
        kind = elementName switch
        {
            "CsvOutput" => OutputKind.CSVWriter,
            "SqlOutput" => OutputKind.SqlWriter,
            "HttpOutput" => OutputKind.HttpWriter,
            "JsonOutput" => OutputKind.JsonWriter,
            "XmlOutput" => OutputKind.XmlWriter,
            "MemoryOutput" => OutputKind.MemoryWriter,
            _ => default
        };

        return elementName is "CsvOutput" or "SqlOutput" or "HttpOutput" or "JsonOutput" or "XmlOutput" or "MemoryOutput";
    }
}
