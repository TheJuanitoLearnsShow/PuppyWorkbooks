using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;

namespace PuppyWorkbooks.ViewModels;

public partial class WorkSheetViewModel : ReactiveObject
{
    private bool _showResultsForEachCell;

    private WorkCellViewModel? _selectedFormula = null;
    public ObservableCollection<WorkCellViewModel> Cells { get; set; } = [];

    public WorkSheetViewModel()
    {
        ExecuteCommand = ReactiveCommand.CreateFromTask(() => ExecuteUpToCellAsync(-1));
        AddCellCommand = ReactiveCommand.Create(AddCell);
        RemoveCellCommand = ReactiveCommand.Create<int>(RemoveCell);
    }
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
    
    public bool ShowResultsForEachCell
    {
        get => _showResultsForEachCell;
        set => this.RaiseAndSetIfChanged(ref _showResultsForEachCell, value);
    }

    public WorkCellViewModel SelectedFormula
    {
        get => _selectedFormula;
        set => this.RaiseAndSetIfChanged(ref _selectedFormula, value);
    }
    public ReactiveCommand<Unit, Unit> ExecuteCommand { get; }
    public ReactiveCommand<Unit, Unit> AddCellCommand { get; }
    public ReactiveCommand<int, Unit> RemoveCellCommand { get; }
}