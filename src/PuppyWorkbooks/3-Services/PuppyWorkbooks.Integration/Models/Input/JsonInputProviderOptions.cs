using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class JsonInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.JsonReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string JsonPath { get; set; } = "$";
    [XmlAttribute] public string JsonPathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeJsonPath() => !string.IsNullOrWhiteSpace(JsonPath);
    public bool ShouldSerializeJsonPathFromField() => !string.IsNullOrWhiteSpace(JsonPathFromField);
}