namespace PuppyWorkbooks;

public class WorkSheet
{
    public IList<WorkCell> Cells { get; set; } = new List<WorkCell>();
}