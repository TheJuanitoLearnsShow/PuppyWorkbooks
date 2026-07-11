using System.Text.Json;
using PuppyWorkbooks;
using PuppyWorkbooks.App.WinForms.Models;

namespace PuppyWorkbooks.App.WinForms.ViewModels;

public sealed class TabViewModel
{
    private WorkSheet _model = new WorkSheet();
    public string Title { get; private set; }
    public string PagePath { get; }

    public WorkSheet Model => _model;

    public event EventHandler<string>? PageMessageReceived;
    public event EventHandler<string>? ViewModelMessageRaised;

    public TabViewModel(string title, string pagePath)
    {
        Title = title;
        PagePath = pagePath;
    }

    public async Task OnPageMessage(string message)
    {
        
        var messageBase = JsonSerializer.Deserialize<ViewMessageBase>(message);
        if (messageBase?.Type == "runUpToSelected")
        {
            
        }
        if (messageBase?.Type == "runAll")
        {
            await RunAllFormulas();
        }
        // PageMessageReceived?.Invoke(this, message);

        // Example: echo back
        //RaiseMessageToPage($"VM received: {message}");
    }

    public void RaiseMessageToPage<T>(T payload, string type)
    {
        var msg = new ViewMessage<T>() { Payload  =  payload, Type = type };
            
        var message = JsonSerializer.Serialize(msg);
        ViewModelMessageRaised?.Invoke(this, message);
    }
    
    public void SendModel()
    {
        RaiseMessageToPage(_model, "worksheet");
    }
    
    private async Task RunAllFormulas()
    {
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(_model, yieldResultsForEachCell: true))
        {
            RaiseMessageToPage(result, "cellResult");
        }
    }
    
    // public PuppyWorkbooks.WorkSheet ToModel()
    // {
    //     var model = new PuppyWorkbooks.WorkSheet
    //     {
    //         Name = this.SheetName
    //     };
    //     foreach (var cellViewModel in formulas)
    //     {
    //         model.Cells.Add(cellViewModel.ToModel());
    //     }
    //     return model;
    // }
    
    public void FromModel(WorkSheet model)
    {
        _model = model;
        Title = model.Name;
    }
}
