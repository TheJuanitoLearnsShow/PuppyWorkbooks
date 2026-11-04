using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Output;

public class CsvOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.CSVWriter;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
}