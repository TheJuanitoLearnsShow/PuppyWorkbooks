using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PuppyWorkbooks;
using PuppyWorkbooks.App.WinForms.ViewModels;

public partial class TabView : UserControl
{
    private readonly TabViewModel _vm;
    private WebView2 webView21;

    public string SheetName => _vm.Title;

    public TabViewModel Vm => _vm;

    public TabView(TabViewModel vm)
    {
        InitializeComponent();
        _vm = vm;

        _vm.ViewModelMessageRaised += OnViewModelMessageRaised;

        Load += async (_, __) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        await webView21.EnsureCoreWebView2Async();

        webView21.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        string fullPath = Path.Combine(Application.StartupPath, _vm.PagePath);
        webView21.Source = new Uri(fullPath);
        
        _vm.SendModel();
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string msg = e.WebMessageAsJson;
        await _vm.OnPageMessage(msg);
    }

    private void OnViewModelMessageRaised(object? sender, string message)
    {
        webView21.CoreWebView2?.PostWebMessageAsString(message);
    }
    private void InitializeComponent()
    {
        this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
        ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
        this.SuspendLayout();

        this.webView21.Dock = DockStyle.Fill;
        this.webView21.CreationProperties = null;
        this.webView21.Name = "webView21";

        this.Controls.Add(this.webView21);
        this.Name = "TabView";
        this.Size = new Size(800, 600);

        ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
        this.ResumeLayout(false);
    }
}
