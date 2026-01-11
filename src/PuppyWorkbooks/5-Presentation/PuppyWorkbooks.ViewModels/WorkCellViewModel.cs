using System.Reactive;
using ReactiveUI;

namespace PuppyWorkbooks.ViewModels;

public partial class WorkCellViewModel : ReactiveObject
{
    private int _id;
    private string _name = string.Empty;
    private string _formula = string.Empty;
    private string _comments = string.Empty;

    private readonly WorkSheetViewModel _parentSheet;

    private string _result = string.Empty;

    public WorkCellViewModel()
    {
    }
    public WorkCellViewModel(int id, string name, string formula, string comments, WorkSheetViewModel parentSheet)
    {
        _id = id;
        _name = name;
        _formula = formula;
        _comments = comments;
        _parentSheet = parentSheet;
        
        RemoveCellCommand = ReactiveCommand.Create(() => _parentSheet.RemoveCell(Id));
        ExecuteCommand = ReactiveCommand.CreateFromTask(() => _parentSheet.ExecuteUpToCellAsync(Id));
    }
    
    public void FromModel(PuppyWorkbooks.WorkCell model)
    {
        _id = model.Id;
        _name = model.Name;
        _formula = model.Formula;
        _comments = model.Comments;
    }
    public PuppyWorkbooks.WorkCell ToModel()
    {
        return new PuppyWorkbooks.WorkCell
        {
            Id = _id,
            Name = _name,
            Formula = _formula,
            Comments = _comments
        };
    }
    
    private void RemoveCell()
    {
        _parentSheet.RemoveCell(Id);
    }
    
    private Task ExecuteAsync()
    {
        return _parentSheet.ExecuteUpToCellAsync(Id);
    }
    
    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Formula
    {
        get => _formula;
        set => this.RaiseAndSetIfChanged(ref _formula, value);
    }

    public string Comments
    {
        get => _comments;
        set => this.RaiseAndSetIfChanged(ref _comments, value);
    }

    public string Result
    {
        get => _result;
        set => this.RaiseAndSetIfChanged(ref _result, value);
    }

    public ReactiveCommand<Unit, Unit> RemoveCellCommand { get; }
    public ReactiveCommand<Unit, Unit> ExecuteCommand { get; }
}