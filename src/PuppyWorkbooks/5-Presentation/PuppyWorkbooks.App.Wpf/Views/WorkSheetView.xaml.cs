using System.Reactive.Disposables.Fluent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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
    private void ShowEditPanel()
    {
        var storyboard = (Storyboard)FindResource("SlideInStoryboard");
        storyboard.Begin();
        EditPanelHost.Visibility = Visibility.Visible;
    }

    private void HideEditPanel()
    {
        var storyboard = (Storyboard)FindResource("SlideOutStoryboard");
        storyboard.Completed += (s, e) =>
        {
            EditPanelHost.Visibility = Visibility.Collapsed;
        };
        storyboard.Begin();
    }

}