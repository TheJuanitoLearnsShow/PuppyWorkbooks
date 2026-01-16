using System.Collections.ObjectModel;
using PuppyWorkbooks.App.Wpf.Views;
using Syncfusion.Windows.Tools.Controls;

namespace PuppyWorkbooks.App.Wpf.ViewModels.Docking;

public class DockingShellViewModel
{
    public ObservableCollection<DockItem> DockCollections { get; } = [];


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
        var selectedDoc = DockCollections
            .FirstOrDefault(d => d is { IsSelectedTab: true, State: DockState.Document });
        var view = selectedDoc?.Content as WorkSheetView;
        // Implement save logic here
        view?.ViewModel?.SaveToXmlFile("temp.xml");
    }
    
    public void LoadDocument()
    {
        var selectedDoc = DockCollections
            .FirstOrDefault(d => d is { IsSelectedTab: true, State: DockState.Document });
        var view = selectedDoc?.Content as WorkSheetView;
        // Implement save logic here
        view?.ViewModel?.LoadFromXmlFile("temp.xml");
    }
}