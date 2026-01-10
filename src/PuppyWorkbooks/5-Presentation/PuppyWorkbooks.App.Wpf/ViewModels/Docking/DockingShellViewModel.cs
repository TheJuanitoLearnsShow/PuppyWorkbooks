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
        
        DockCollections.Add(new DockItem() { Header = "Doc", Content = doc, State = DockState.Document});
    }
}