using System;
using ReactiveUI;

namespace PuppyMapper.AvaloniaApp;

public class AppViewLocator : ReactiveUI.IViewLocator
{
    public IViewFor ResolveView<T>(T? viewModel, string? contract = null)
    {
        return AppViewLocator.GetView(viewModel);
    }
    public static IViewFor GetView<T>(T? viewModel) => viewModel switch
    {
        // MappingDocumentIdeEditorViewModel context => new MappingDocumentIDEEditorView { DataContext = context },
        //
        // InputEditorViewModel context => new InputEditorView { DataContext = context },
        // CsvInputEditorViewModel context => new CsvInputEditorView { DataContext = context },
        // MemoryInputEditorViewModel context => new MemoryInputEditorView { DataContext = context },
        //
        // OutputEditorViewModel context => new OutputEditorView { DataContext = context },
        // CsvOutputEditorViewModel context => new CsvOutputEditorView { DataContext = context },
        // MemoryOutputEditorViewModel context => new MemoryOutputEditorView { DataContext = context },
        //
        // DockingHostViewModel context => new DockingHostView { DataContext = context },
        // DocumentViewModel context => new DocumentView { DataContext = context },
        // DocumentEditorViewModel context => new DocumentEditorView { DataContext = context },
        _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    };
}