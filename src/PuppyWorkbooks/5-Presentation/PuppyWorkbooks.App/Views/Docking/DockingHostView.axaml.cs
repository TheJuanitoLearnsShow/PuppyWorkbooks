using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI.Avalonia;
using ReactiveUI;
using Avalonia.Markup.Xaml;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DockingHostView : ReactiveUserControl<DockingHostViewModel>
{
    public DockingHostView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Add activation logic or subscriptions here if needed in the future.
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
