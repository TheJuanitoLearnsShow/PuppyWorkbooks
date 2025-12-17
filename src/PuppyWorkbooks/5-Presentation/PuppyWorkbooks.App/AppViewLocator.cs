using System;
using PuppyMapper.AvaloniaApp.ViewModels.Docking;
using PuppyMapper.AvaloniaApp.Views;
using PuppyMapper.AvaloniaApp.Views.Docking;
using PuppyMapper.AvaloniaApp.Views.Inputs;
using PuppyMapper.AvaloniaApp.Views.Outputs;
using PuppyMapper.Viewmodels;
using PuppyMapper.ViewModels.Inputs;
using PuppyMapper.ViewModels.Outputs;
using ReactiveUI;
using CsvInputEditorView = PuppyMapper.AvaloniaApp.Views.Inputs.CsvInputEditorView;
using CsvInputEditorViewModel = PuppyMapper.ViewModels.Inputs.CsvInputEditorViewModel;
using InputEditorView = PuppyMapper.AvaloniaApp.Views.Inputs.InputEditorView;
using InputEditorViewModel = PuppyMapper.ViewModels.Inputs.InputEditorViewModel;
using DockModel = Dock.Model.ReactiveUI.Controls;
using DockControls = Dock.Model.Avalonia.Controls;

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