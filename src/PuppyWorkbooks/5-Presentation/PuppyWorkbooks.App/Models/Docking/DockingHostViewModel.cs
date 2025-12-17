using System.IO;
using Dock.Model.Core;
using ReactiveUI;

namespace PuppyWorkbooks.App.Models.Docking;

public partial class DockingHostViewModel : ReactiveObject, IRoutableViewModel
{
    private readonly DockFactory _factory;
    [Reactive] private IDock? _layout;

    public string UrlPathSegment { get; } = "dock";
    public IScreen HostScreen { get; }
    
    [Reactive] private string _help;
    [Reactive] private DocumentEditorViewModel _registeredMappings;
    private int _openedDocCnt = 0;

    public DockingHostViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
        _factory = new DockFactory(hostScreen);
        var layout = _factory.CreateLayout();
        if (layout is not null)
        {
            _factory.InitLayout(layout);
        }
        // Layout = layout;
        // Help = "This is help";
        var mappingBaseFolder = "./SampleMap";

        if (!Directory.Exists(mappingBaseFolder))
        {
            Directory.CreateDirectory(mappingBaseFolder);
        }
        // _registeredMappings = new DocumentEditorViewModel(HostScreen, OnDocumentOpen, mappingBaseFolder);
    }

    // private void OnDocumentOpen(MappingDocumentIdeEditorViewModel documentToOpen)
    // {
    //     var newDoc = new DocumentViewModel(HostScreen, documentToOpen) { Id = $"Doc{_openedDocCnt++}", Title = $"Document {_openedDocCnt++}" };
    //     _factory.ShowDocument(newDoc);
    // }
}