using System.Reactive.Disposables.Fluent;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DocumentDefinitionsView : ReactiveUserControl<DocumentDefinitionsViewModel>
{
    public DocumentDefinitionsView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            
            this.BindCommand(ViewModel, vm => vm.AddDocumentCommand,
                v => v.AddMapping)
                .DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditDocumentCommand,
                    v => v.OpenMapping)
                .DisposeWith(disposables);
            
        });
    }
}