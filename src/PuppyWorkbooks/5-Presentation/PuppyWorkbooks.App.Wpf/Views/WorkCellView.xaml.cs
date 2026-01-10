using System.Reactive.Disposables.Fluent;
using System.Windows.Controls;
using PuppyWorkbooks.ViewModels;
using ReactiveUI;

namespace PuppyWorkbooks.App.Wpf.Views;

public partial class WorkCellView : ReactiveUserControl<WorkCellViewModel>
{
    public WorkCellView()
    {
        InitializeComponent();
        
        this.WhenActivated((disposables) =>
        {
            this.Bind(ViewModel, vm => vm.Name, v => v.Name)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.Formula, v => v.Formula)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Comments, v => v.Comments)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Result, v => v.Result)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ExecuteCommand, 
                    v => v.ExecuteBtn)
                .DisposeWith(disposables);
        });
    }
}