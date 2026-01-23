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

    public WorkSheetViewModel? ActiveDocument
    {
        get => _activeDocument;
        set => this.RaiseAndSetIfChanged(ref _activeDocument, value);
    }
    public void NewDocument_Click()
    {
        var doc = new WorkSheetView();
        var dockItem = new DockItem() { Header = "Doc", 
            Content = doc, 
            State = DockState.Document 
        };
        
        DockCollections.Add(dockItem);
    }

    public void SaveDocument()
    {
    //     var selectedDoc = DockCollections
    //         .FirstOrDefault(d => d is { IsSelectedTab: true, State: DockState.Document });
    //     var view = selectedDoc?.Content as WorkSheetView;
        // Implement save logic here
        // view?.ViewModel?.SaveToXmlFile("temp.xml");
        _activeDocument?.SaveToXmlFile("temp.xml");
    }
    
    public void LoadDocument()
    {
        // var selectedDoc = DockCollections
        //     .FirstOrDefault(d => d is { IsSelectedTab: true, State: DockState.Document });
        // var view = selectedDoc?.Content as WorkSheetView;
        // // Implement save logic here
        // view?.ViewModel?.LoadFromXmlFile("temp.xml");
        _activeDocument?.LoadFromXmlFile("temp.xml");
    }
}