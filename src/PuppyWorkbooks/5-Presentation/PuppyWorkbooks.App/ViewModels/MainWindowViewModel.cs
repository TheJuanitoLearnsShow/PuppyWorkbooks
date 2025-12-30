using System;
using System.Reactive;
using AvaloniaEdit.Utils;
using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels;

public class MainWindowViewModel : ViewModelBase, IScreen
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    // The Router associated with this Screen.
    // Required by the IScreen interface.
    public RoutingState Router { get; } = new ();
    // The command that navigates a user back.
    public ReactiveCommand<Unit, IRoutableViewModel> GoBack => Router.NavigateBack;

    public MainWindowViewModel()
    {
        
        var dockViewModel = new DockingHostViewModel(this);
        Router.Navigate.Execute(dockViewModel).Subscribe();
    }
}