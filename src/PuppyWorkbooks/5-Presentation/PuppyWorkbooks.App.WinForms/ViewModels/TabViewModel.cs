using System.Text.Json;
using PuppyWorkbooks;
using PuppyWorkbooks.App.WinForms.Models;

namespace PuppyWorkbooks.App.WinForms.ViewModels;

public sealed class TabViewModel
{
    private WorkSheet _model = new WorkSheet();
    public string Title { get; }
    public string PagePath { get; }

    public event EventHandler<string>? PageMessageReceived;
    public event EventHandler<string>? ViewModelMessageRaised;

    public TabViewModel(string title, string pagePath)
    {
        Title = title;
        PagePath = pagePath;
    }

    public void OnPageMessage(string message)
    {
        PageMessageReceived?.Invoke(this, message);

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
        RaiseMessageToPage(_model);
    }
    
    private async Task RunAllFormulas()
    {
        var workbook = ToModel();
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true))
        {
            formulas[result.CellId].Result = result.DisplayOutput;
        }
        dgvFormulas.Refresh();
    }
    public PuppyWorkbooks.WorkSheet ToModel()
    {
        var model = new PuppyWorkbooks.WorkSheet
        {
            Name = this.SheetName
        };
        foreach (var cellViewModel in formulas)
        {
            model.Cells.Add(cellViewModel.ToModel());
        }
        return model;
    }
    public void FromModel(WorkSheet model)
    {
        _model = model;
    }
}
