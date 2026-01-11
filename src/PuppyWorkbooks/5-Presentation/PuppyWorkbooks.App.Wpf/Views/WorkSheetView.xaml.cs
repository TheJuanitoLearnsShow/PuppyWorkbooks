using System.ComponentModel;
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
        DataContext = ViewModel;
        this.WhenActivated((disposables) =>
        {
            this.WhenAnyValue(x => x.ViewModel.SelectedFormula)
                .Subscribe(
                selectedFormula =>
                {
                    if (selectedFormula != null)
                    {
                        ShowEditPanel();
                    }
                    else
                    {
                        HideEditPanel();
                    }
                })
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddCellCommand, 
                    v => v.AddCellBtn)
                .DisposeWith(disposables);
            
        });
        // ViewModel.PropertyChanged += OnSelectedFormulaChanged;
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
    private void OnSelectedFormulaChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.SelectedFormula))
        {
            if (ViewModel?.SelectedFormula != null)
            {
                ShowEditPanel();
            }
            else
            {
                HideEditPanel();
            }
        }
    }


}