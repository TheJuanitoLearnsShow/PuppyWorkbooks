using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class FileSystemInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.FileSystemReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public bool AddFileSizeField { get; set; }
    [XmlAttribute] public bool AddCreatedOnDate { get; set; }
    [XmlAttribute] public bool AddLastModifiedDateField { get; set; }

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeAddFileSizeField() => AddFileSizeField;
    public bool ShouldSerializeAddCreatedOnDate() => AddCreatedOnDate;
    public bool ShouldSerializeAddLastModifiedDateField() => AddLastModifiedDateField;
}