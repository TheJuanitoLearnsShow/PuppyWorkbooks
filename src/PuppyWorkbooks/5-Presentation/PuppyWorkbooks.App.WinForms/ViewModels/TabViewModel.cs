namespace PuppyWorkbooks.App.WinForms.ViewModels

public sealed class TabViewModel
{
    public string Title { get; }
    public string PagePath { get; }

    public event EventHandler<string>? PageMessageReceived;
    public event EventHandler<string>? ViewModelMessageRaised;

    public TabViewModel(string title, string pagePath)
    {
        Title = title;
        PagePath = pagePath;
    }

    public void OnPageMessage(string message)
    {
        PageMessageReceived?.Invoke(this, message);

        // Example: echo back
        RaiseMessageToPage($"VM received: {message}");
    }

    public void RaiseMessageToPage(string message)
    {
        ViewModelMessageRaised?.Invoke(this, message);
    }
}
