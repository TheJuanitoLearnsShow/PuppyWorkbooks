namespace PuppyWorkbooks.App.WinForms;

public class FormulaEntry
{
    public int Id { get; set; } = 1;
    public string Name { get; set; }
    public string Expression { get; set; }
    public string Comments { get; set; }
    public string Result { get; set; }

    
    public PuppyWorkbooks.WorkCell ToModel()
    {
        return new PuppyWorkbooks.WorkCell
        {
            Id = Id,
            Name = Name,
            Formula = Expression,
            Comments = Comments
        };
    }
}