namespace PuppyWorkbooks.Integration.Models;

public enum InputKind { CSVReader, SqlReader, HttpReader, JsonReader, XmlReader }

public static class InputKindXmlNames
{
    public static string GetElementName(this InputKind kind) => kind switch
    {
        InputKind.CSVReader => "CsvInput",
        InputKind.SqlReader => "SqlInput",
        InputKind.HttpReader => "HttpInput",
        InputKind.JsonReader => "JsonInput",
        InputKind.XmlReader => "XmlInput",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported input provider kind.")
    };

    public static bool TryParseElementName(string elementName, out InputKind kind)
    {
        kind = elementName switch
        {
            "CsvInput" or "CsvInputProvider" => InputKind.CSVReader,
            "SqlInput" or "SqlInputProvider" => InputKind.SqlReader,
            "HttpInput" or "HttpInputProvider" => InputKind.HttpReader,
            "JsonInput" or "JsonInputProvider" => InputKind.JsonReader,
            "XmlInput" or "XmlInputProvider" => InputKind.XmlReader,
            _ => default
        };

        return elementName is "CsvInput" or "CsvInputProvider"
            or "SqlInput" or "SqlInputProvider"
            or "HttpInput" or "HttpInputProvider"
            or "JsonInput" or "JsonInputProvider"
            or "XmlInput" or "XmlInputProvider";
    }
}
