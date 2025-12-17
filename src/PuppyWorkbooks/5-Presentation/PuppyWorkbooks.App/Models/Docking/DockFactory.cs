using System;
using System.Collections.Generic;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using ReactiveUI;

namespace PuppyWorkbooks.App.Models.Docking;

public class DockFactory : Factory
{
    private readonly IScreen _host;
    private DocumentDock _documentDock;

    public DockFactory(IScreen host)
    {
        _host = host;
    }

    public void ShowDocument(DocumentViewModel openedDocument)
    {
        _documentDock.AddDocument(openedDocument);
        // _documentDock.VisibleDockables?.Add(openedDocument);
        // _documentDock.ActiveDockable = openedDocument;
        // _root.VisibleDockables?.Add(openedDocument);
        // _root.ActiveDockable = openedDocument;
    }
    public override IRootDock CreateLayout()
    {
        var document1 = new DocumentViewModel(_host, new DocumentEditorViewModel(, "test")) { Id = "Doca1", Title = "Document 1" };
        _documentDock = new DocumentDock
        {
            Id = "Documents",
            VisibleDockables = CreateList<IDockable>(document1),
            ActiveDockable = null,
            CanCreateDocument = false,
            
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
