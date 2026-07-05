namespace PuppyWorkbooks.App.WinForms.Models;

public class ViewMessage<T>
{
 public string Type { get; set; }
 public T Payload { get; set; }
}