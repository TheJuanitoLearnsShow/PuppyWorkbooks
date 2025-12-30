using System.Reactive.Disposables.Fluent;
using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PuppyWorkbooks.App.Views.Docking;

public partial class DockingHostView : ReactiveUserControl<DockingHostViewModel>
{
    public DockingHostView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            
            // this.Bind(ViewModel, vm => vm.RegisteredMappings,
            //     v => v.DocumentLst.ViewModel)
            //     .DisposeWith(disposables);
            
            this.Bind(ViewModel, vm => vm.Layout,
                v => v.DockCtrl.Layout)
                .DisposeWith(disposables);
            
        });
        // var list = this.FindControl<ListBox>("DocumentsList");
        // list.DoubleTapped += (s, e) =>
        // {
        //     if (DataContext is MainWindowViewModel vm && list.SelectedItem is MappingDocumentIdeEditorViewModel doc)
        //     {
        //         vm.OpenDocument(doc);
        //     }
        // };
    }

}
