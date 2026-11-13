using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class CsvInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.CSVReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    
    [XmlAttribute] public char Separator { get; set; } = ',';
    [XmlAttribute] public bool HasHeaderRecord { get; set; } = true;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
}