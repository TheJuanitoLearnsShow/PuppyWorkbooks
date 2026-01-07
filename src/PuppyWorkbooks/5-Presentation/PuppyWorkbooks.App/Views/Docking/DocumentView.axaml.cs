using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI.Avalonia;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DocumentView : ReactiveUserControl<DocumentViewModel>
{
    public DocumentView()
    {
        InitializeComponent();
    }
}