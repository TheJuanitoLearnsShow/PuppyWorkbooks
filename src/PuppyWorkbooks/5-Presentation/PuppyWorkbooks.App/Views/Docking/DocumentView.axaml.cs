using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI.Avalonia;
using ReactiveUI;
using Avalonia.Markup.Xaml;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DocumentView : ReactiveUserControl<DocumentViewModel>
{
    public DocumentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // activation logic if required
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
