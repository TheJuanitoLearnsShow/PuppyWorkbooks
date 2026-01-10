using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace PuppyWorkbooks.ViewModels;

public partial class WorkSheetViewModel : ReactiveObject
{
    [Reactive]
    private bool _showResultsForEachCell;
    public ObservableCollection<WorkCellViewModel> Cells { get; set; } = [];
    
    public void FromModel(PuppyWorkbooks.WorkSheet model)
    {
        Cells.Clear();
        foreach (var cell in model.Cells)
        {
            var cellViewModel = new WorkCellViewModel();
            cellViewModel.FromModel(cell);
            Cells.Add(cellViewModel);
        }
    }
    public PuppyWorkbooks.WorkSheet ToModel()
    {
        var model = new PuppyWorkbooks.WorkSheet();
        foreach (var cellViewModel in Cells)
        {
            model.Cells.Add(cellViewModel.ToModel());
        }
        return model;
    }

    [ReactiveCommand]
    public Task ExecuteAsync()
    {
        return ExecuteUpToCellAsync(-1);
    }

    public async Task ExecuteUpToCellAsync(int cellIdx)
    {
        var workbook = ToModel();
        var interpreter = new WorkbookInterpreter();
        await foreach (var result in interpreter.ExecuteAsync(workbook, cellIdx, yieldResultsForEachCell: ShowResultsForEachCell))
        {
            Cells[result.CellId].Result = result.DisplayOutput;
        }
    }

    [ReactiveCommand]
    private void AddCell()
    {
        var newCell = new WorkCellViewModel
        {
            Id = Cells.Count,
            Name = $"Formula{Cells.Count + 1}",
            Formula = string.Empty,
            Result = string.Empty
        };
        Cells.Add(newCell);
    }
    
    [ReactiveCommand]
    public void RemoveCell(int cellIdx)
    {
        Cells.RemoveAt(cellIdx);
        ReIndexCells();
    }

    private void ReIndexCells()
    {
        for (var i = 0; i < Cells.Count; i++)
        {
            Cells[i].Id = i;
        }
    }
}