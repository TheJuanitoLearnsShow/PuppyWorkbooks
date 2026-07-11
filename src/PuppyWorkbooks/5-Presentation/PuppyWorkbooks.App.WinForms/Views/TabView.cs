using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PuppyWorkbooks;
using PuppyWorkbooks.App.WinForms.ViewModels;

public partial class TabView : UserControl
{
    private const string InternalDomain = "appassets.puppy";
    private readonly TabViewModel _vm;
    private readonly WebView2 _webView21 = new ();

    public string SheetName => _vm.Title;

    public TabViewModel Vm => _vm;

    public TabView(TabViewModel vm)
    {
        InitializeComponent();
        
        _vm = vm;

        _vm.ViewModelMessageRaised += OnViewModelMessageRaised;

        Load += async (_, __) => await InitializeWebViewAsync();
        _webView21.NavigationCompleted += OnNavigationCompleted;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _vm.SendModel();
    }

    private async Task InitializeWebViewAsync()
    {
        await _webView21.EnsureCoreWebView2Async();

        _webView21.CoreWebView2.SetVirtualHostNameToFolderMapping(
            InternalDomain,
            "wwwroot",
            CoreWebView2HostResourceAccessKind.DenyCors
        );
        _webView21.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var fullPath = $"https://{InternalDomain}/worksheet-editor/index.html";
        _webView21.Source = new Uri(fullPath);
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string msg = e.WebMessageAsJson;
        await _vm.OnPageMessage(msg);
    }

    private void OnViewModelMessageRaised(object? sender, string message)
    {
        _webView21.CoreWebView2?.PostWebMessageAsString(message);
    }
    private void InitializeComponent()
    {
        ((System.ComponentModel.ISupportInitialize)(this._webView21)).BeginInit();
        this.SuspendLayout();

        this._webView21.Dock = DockStyle.Fill;
        this._webView21.CreationProperties = null;
        this._webView21.Name = "_webView21";

        this.Controls.Add(this._webView21);
        this.Name = "TabView";
        this.Size = new Size(800, 600);

        ((System.ComponentModel.ISupportInitialize)(this._webView21)).EndInit();
        this.ResumeLayout(false);
    }
}
