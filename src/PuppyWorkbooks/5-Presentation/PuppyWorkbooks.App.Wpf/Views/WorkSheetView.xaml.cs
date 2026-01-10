using System.Reactive.Disposables.Fluent;
using System.Windows.Controls;
using PuppyWorkbooks.ViewModels;
using ReactiveUI;

namespace PuppyWorkbooks.App.Wpf.Views;

public partial class WorkSheetView : ReactiveUserControl<WorkSheetViewModel>
{
    public WorkSheetView()
    {
        InitializeComponent();
        ViewModel = new WorkSheetViewModel();
        this.WhenActivated((disposables) =>
        {
            this.BindCommand(ViewModel, vm => vm.AddCellCommand, 
                    v => v.AddCellBtn)
                .DisposeWith(disposables);
        });
    }
}