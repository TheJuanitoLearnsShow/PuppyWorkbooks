namespace PuppyWorkbooks;

public class WorkSheet
{
    public string Name { get; set; } = string.Empty;
    public List<WorkCell> Cells { get; set; } = new List<WorkCell>();
}