using Avalonia.Controls;
using PuppyWorkbooks.App.ViewModels;

namespace PuppyWorkbooks.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainWindowViewModel
        {
            GetStorageProvider = () => this.StorageProvider
        };
        DataContext = vm;
    }
}