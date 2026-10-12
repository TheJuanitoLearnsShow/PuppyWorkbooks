using System.Xml.Serialization;

namespace PuppyWorkbooks.Integration.Models;

/// <summary>
/// Runs another integration for the current row. The row, or the value named by
/// <see cref="FromField"/>, is supplied to the child integration's first memory input.
/// </summary>
public sealed class RunIntegrationStep : IntegrationStep
{
    [XmlAttribute] public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional name of the current-row field to pass to the child integration.
    /// When omitted, the complete current row is passed.
    /// </summary>
    [XmlAttribute] public string FromField { get; set; } = string.Empty;
}
