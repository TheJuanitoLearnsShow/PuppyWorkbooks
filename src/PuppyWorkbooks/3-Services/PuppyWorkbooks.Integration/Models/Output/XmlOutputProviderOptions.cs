using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models.Output;

public class XmlOutputProviderOptions : OutputStep
{
    [XmlIgnore] public override OutputKind Kind { get; set; } = OutputKind.XmlWriter;
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;
    [XmlAttribute] public string FilePathFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRootElementFromField { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElement { get; set; } = string.Empty;
    [XmlAttribute] public string XmlRecordElementFromField { get; set; } = string.Empty;

    public bool ShouldSerializeFilePath() => !string.IsNullOrWhiteSpace(FilePath);
    public bool ShouldSerializeFilePathFromField() => !string.IsNullOrWhiteSpace(FilePathFromField);
    public bool ShouldSerializeXmlRootElement() => !string.IsNullOrWhiteSpace(XmlRootElement);
    public bool ShouldSerializeXmlRootElementFromField() => !string.IsNullOrWhiteSpace(XmlRootElementFromField);
    public bool ShouldSerializeXmlRecordElement() => !string.IsNullOrWhiteSpace(XmlRecordElement);
    public bool ShouldSerializeXmlRecordElementFromField() => !string.IsNullOrWhiteSpace(XmlRecordElementFromField);
}