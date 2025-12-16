namespace PuppyWorkbooks;

public class WorkCell
{
    public WorkCell()
    {
    }
    public WorkCell(int id, string name, string formula, string comments)
    {
        Id = id;
        Name = name;
        Formula = formula;
        Comments = comments;
    }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}