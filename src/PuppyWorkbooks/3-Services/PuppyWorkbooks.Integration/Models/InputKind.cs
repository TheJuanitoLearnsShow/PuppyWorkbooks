namespace PuppyWorkbooks.Integration.Models;

public enum InputKind { CSVReader, SqlReader, HttpReader, JsonReader, XmlReader }

public static class InputKindXmlNames
{
    public static string GetElementName(this InputKind kind) => kind switch
    {
        InputKind.CSVReader => "CsvInputProvider",
        InputKind.SqlReader => "SqlInputProvider",
        InputKind.HttpReader => "HttpInputProvider",
        InputKind.JsonReader => "JsonInputProvider",
        InputKind.XmlReader => "XmlInputProvider",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported input provider kind.")
    };

    public static bool TryParseElementName(string elementName, out InputKind kind)
    {
        kind = elementName switch
        {
            "CsvInputProvider" => InputKind.CSVReader,
            "SqlInputProvider" => InputKind.SqlReader,
            "HttpInputProvider" => InputKind.HttpReader,
            "JsonInputProvider" => InputKind.JsonReader,
            "XmlInputProvider" => InputKind.XmlReader,
            _ => default
        };

        return elementName is "CsvInputProvider" or "SqlInputProvider" or "HttpInputProvider"
            or "JsonInputProvider" or "XmlInputProvider";
    }
}
