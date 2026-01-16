using System;
using System.Reactive;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels.Docking;

public class DocumentViewModel : ViewModelBase, IRoutableViewModel
{
    public string UrlPathSegment { get; }
    public IScreen HostScreen { get; }

    public string Title { get; }

    private string? _text;
    public string? Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    private string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public ReactiveCommand<string?, Unit> OpenCommand { get; }
    public ReactiveCommand<string?, Unit> SaveCommand { get; }

    public DocumentViewModel(IScreen host, string title)
    {
        HostScreen = host ?? throw new ArgumentNullException(nameof(host));
        UrlPathSegment = Guid.NewGuid().ToString();
        Title = title;

        OpenCommand = ReactiveCommand.Create<string?>(path =>
        {
            // handled by DockHost which reads file and creates a DocumentViewModel
        });

        SaveCommand = ReactiveCommand.Create<string?>(path =>
        {
            // handled by DockHost SaveActiveDocumentAsync
        });
    }
}

