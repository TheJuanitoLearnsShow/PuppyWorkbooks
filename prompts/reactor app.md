```csharp
namespace PuppyWorkbooks;

public class WorkSheet
{
    public string Name { get; set; } = string.Empty;
    public List<WorkCell> Cells { get; set; } = new List<WorkCell>();
}
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
```

Using reactor win ui code first approach, create winui app that:

- maintains a list of WorkCell (from PuppyWorkSheets namespace), as a WorkSheet.
- visually it has a list of the WorkCell in the left side. On theright side is the form to edit the details of the selected formula entry
- each entry contains name, formula, comments
- the app allows saving the viewmodel to xml via a toolbar button and ctrl+s. It also allows opening and existing xml file. Each WorkSheet has its own name, editable from the app
- toolbar also has a button to run all the WorkCell or all up to the selected WorkCell. Do not code the running part, we will fill in the powerfx interpretation later.
- follow the most visually appealing fluent design techniques. Left list a right form to e separated by splitter.
- allow the app to be run from command line to load the WorkSheet from an xml file passed by argument in the command line

