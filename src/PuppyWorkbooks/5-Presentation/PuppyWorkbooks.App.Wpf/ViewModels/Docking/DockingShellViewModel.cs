using System.Collections.ObjectModel;
using PuppyWorkbooks.App.Wpf.Views;
using PuppyWorkbooks.ViewModels;
using ReactiveUI;
using Syncfusion.Windows.Tools.Controls;

namespace PuppyWorkbooks.App.Wpf.ViewModels.Docking;

public class DockingShellViewModel : ReactiveObject
{
    public ObservableCollection<DockItem> DockCollections { get; } = [];


    private WorkSheetViewModel? _activeDocument;
    private WorkSheetView _activeView;

    public WorkSheetViewModel? ActiveDocument
    {
        get => _activeDocument;
        set => this.RaiseAndSetIfChanged(ref _activeDocument, value);
    }
    public WorkSheetView? ActiveView
    {
        get => _activeView;
        set => this.RaiseAndSetIfChanged(ref _activeView, value);
    }
    
    public string? ActiveDocumentName => _activeDocument?.Name;

    

    public void NewDocument(string? name = null)
    {
        var doc = new WorkSheetView();
        if (name != null)
        {
            doc.Name = name;
        }
        NewTab(doc);
    }

    private void NewTab(WorkSheetView doc)
    {
        var dockItem = new DockItem() { Header = doc.Name, 
            Content = doc, 
            State = DockState.Document ,
        };

        DockCollections.Add(dockItem);
    }

    public void SaveDocument(string filePath)
    {
        
        var nameOnly = System.IO.Path.GetFileNameWithoutExtension(filePath);
        ActiveDocument?.Name = nameOnly;
        var selectedDoc = DockCollections
            .FirstOrDefault(d => d.Content.DataContext == ActiveDocument);
        selectedDoc?.Header = ActiveDocument?.Name;
        
    //     var view = selectedDoc?.Content as WorkSheetView;
        // Implement save logic here
        // view?.ViewModel?.SaveToXmlFile("temp.xml");
        _activeDocument?.SaveToXmlFile(filePath);
    }
    
    public void LoadDocument(string filePath)
    {
        // var selectedDoc = DockCollections
        //     .FirstOrDefault(d => d is { IsSelectedTab: true, State: DockState.Document });
        // var view = selectedDoc?.Content as WorkSheetView;
        // // Implement save logic here
        // view?.ViewModel?.LoadFromXmlFile("temp.xml");
        var doc = new WorkSheetView();
        doc.ViewModel?.LoadFromXmlFile(filePath);
        NewTab(doc);
    }
}