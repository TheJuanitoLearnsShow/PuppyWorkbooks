using System;
using System.Reactive;
using System.Threading.Tasks;
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

    public DockingHostViewModel DockHost { get; }

    public ReactiveCommand<string, Unit> OpenFileCommand { get; }
    public ReactiveCommand<string, Unit> SaveFileCommand { get; }

    public MainWindowViewModel()
    {
        DockHost = new DockingHostViewModel(this);
        Router.Navigate.Execute(DockHost).Subscribe();

        OpenFileCommand = ReactiveCommand.CreateFromTask<string>(async path =>
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            await DockHost.OpenFileAsync(path);
        });

        SaveFileCommand = ReactiveCommand.CreateFromTask<string>(async path =>
        {
            // If no path provided, ask active document to SaveAs (view will show a dialog)
            await DockHost.SaveActiveDocumentAsync(path);
        });
    }
}