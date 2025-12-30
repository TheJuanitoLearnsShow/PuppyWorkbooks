using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels.Docking;

public class EditorDockFactory : Factory
{
    private readonly IScreen _host;
    private readonly DocumentEditorViewModel _editor;

    public EditorDockFactory(IScreen host, DocumentEditorViewModel editor)
    {
        _host = host;
        _editor = editor;
    }
    public override IRootDock CreateLayout()
    {
        var document1 = new DocumentViewModel(_host, _editor) { Id = "Edit1", Title = "Editor" };
        var documentInput = new DocumentViewModel(_host, _editor) { Id = "Edit1", Title = "Editor" };
        var document3 = new DocumentViewModel(_host, _editor) { Id = "Edit1", Title = "Editor" };
       
        
        var _documentDock = new DocumentDock
        {
            Id = "Documents",
            VisibleDockables = CreateList<IDockable>(),
            ActiveDockable = null,
            CanCreateDocument = false,
            CanClose = false
            
        };
        
        var root = CreateRootDock();
        root.VisibleDockables = CreateList<IDockable>(_documentDock);

        return root;
    }

    public override void InitLayout(IDockable layout)
    {
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };
        
        base.InitLayout(layout);
    }
}