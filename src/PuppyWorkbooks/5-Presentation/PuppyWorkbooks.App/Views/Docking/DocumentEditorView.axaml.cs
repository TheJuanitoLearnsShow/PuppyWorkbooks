using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI.Avalonia;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DocumentEditorView : ReactiveUserControl<DocumentEditorViewModel>
{
    public DocumentEditorView()
    {
        InitializeComponent();
    }
}