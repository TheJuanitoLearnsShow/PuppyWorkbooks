namespace PuppyWorkbooks;

public class WorkSheet
{
    public string Name { get; set; } = string.Empty;
    public List<WorkCell> Cells { get; set; } = new List<WorkCell>();

    public void SetFormulaValue(string inputValueKey, string inputValueValue)
    {
        var cell = Cells.FirstOrDefault(c => string.Equals(c.Name, inputValueKey, StringComparison.CurrentCulture));
        cell?.Formula = inputValueValue;
    }
}