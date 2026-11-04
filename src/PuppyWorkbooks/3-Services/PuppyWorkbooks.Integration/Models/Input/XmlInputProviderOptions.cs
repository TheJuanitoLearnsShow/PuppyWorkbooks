using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Input;

public class XmlInputProviderOptions : InputStep
{
    [XmlIgnore] public override InputKind Kind { get; set; } = InputKind.XmlReader;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlItemElementFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeXmlItemElement() => !string.IsNullOrWhiteSpace(XmlItemElement);
    public bool ShouldSerializeXmlItemElementFromField() => !string.IsNullOrWhiteSpace(XmlItemElementFromField);
}