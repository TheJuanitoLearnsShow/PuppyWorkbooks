using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using PuppyWorkbooks.App.Wpf.ViewModels.Docking;
using PuppyWorkbooks.App.Wpf.Views;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;

namespace PuppyWorkbooks.App.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int _docCounter = 1;
    private DockingShellViewModel _vm = new DockingShellViewModel();
    public MainWindow()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(SyncfusionLicenseConstant.LicenseKey);
        SfSkinManager.ApplyThemeAsDefaultStyle = true;
        SfSkinManager.ApplicationTheme = new Theme("FluentLight");
        InitializeComponent();
        this.DataContext = _vm;
        
        DockManager.IsSelectedDocument += DockManagerOnIsSelectedDocument;
    }

    private void DockManagerOnIsSelectedDocument(FrameworkElement sender, IsSelectedChangedEventArgs e)
    {
    }

    private void NewDocument_Click(object sender, RoutedEventArgs e)
    {
        _vm.NewDocument_Click();
    }

    private void SaveDocument_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveDocument();
        // if (DocumentHost.SelectedItem is not DocumentTabItem tab) return;
        // if (tab.Content is not DocumentView view) return;
        // var dlg = new SaveFileDialog { Filter = "Text Files|*.txt", FileName = tab.Header.ToString() };
        // if (dlg.ShowDialog() == true)
        // {
        //     File.WriteAllText(dlg.FileName, $"{view.TextBox1.Text}\n{view.TextBox2.Text}");
        // }
    }

    private void OpenDocument_Click(object sender, RoutedEventArgs e)
    {
        _vm.LoadDocument();
    }
}