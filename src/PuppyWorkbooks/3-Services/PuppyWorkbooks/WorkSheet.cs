using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace PuppyWorkbooks;

public class WorkSheet
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Formula-backed variables registered before worksheet cells run.</summary>
    [XmlIgnore, JsonIgnore]
    public Dictionary<string, string> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [XmlArray("Variables"), XmlArrayItem("Variable"), JsonIgnore]
    public List<WorkSheetVariable> VariableDefinitions
    {
        get => Variables.Select(pair => new WorkSheetVariable { Key = pair.Key, Value = pair.Value }).ToList();
        set => Variables = (value ?? []).ToDictionary(pair => pair.Key, pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }
    public List<WorkCell> Cells { get; set; } = new List<WorkCell>();

    public void SetFormulaValue(string inputValueKey, string inputValueValue)
    {
        var cell = Cells.FirstOrDefault(c => string.Equals(c.Name, inputValueKey, StringComparison.CurrentCulture));
        cell?.Formula = inputValueValue;
    }
}

public sealed class WorkSheetVariable
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
