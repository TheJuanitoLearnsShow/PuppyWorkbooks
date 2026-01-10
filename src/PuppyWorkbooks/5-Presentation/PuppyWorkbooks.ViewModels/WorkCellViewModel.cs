using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace PuppyWorkbooks.ViewModels;

public partial class WorkCellViewModel : ReactiveObject
{
    [Reactive]
    private int _id;
    [Reactive]
    private string _name = string.Empty;
    [Reactive]
    private string _formula = string.Empty;
    [Reactive]
    private string _comments = string.Empty;

    private readonly WorkSheetViewModel _parentSheet;

    [Reactive]
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
    
    [ReactiveCommand]
    private void RemoveCell()
    {
        _parentSheet.RemoveCell(Id);
    }
    [ReactiveCommand]
    private Task ExecuteAsync()
    {
        return _parentSheet.ExecuteUpToCellAsync(Id);
    }
}