using System.Configuration;
using System.Data;
using System.Reflection;
using System.Windows;
using ReactiveUI;
using Splat;

namespace PuppyWorkbooks.App.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize ReactiveUI routing
        AppLocator.CurrentMutable.RegisterViewsForViewModels(typeof(App).Assembly);
    }
}