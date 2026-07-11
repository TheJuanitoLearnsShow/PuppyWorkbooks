namespace PuppyWorkbooks.App.WinForms.Models;

public class ViewMessageBase
{
 public string Type { get; set; } // runUpToSelected, runAll, worksheet, cellResult
}
public class ViewMessage<T> : ViewMessageBase
{
 public T Payload { get; set; }
}