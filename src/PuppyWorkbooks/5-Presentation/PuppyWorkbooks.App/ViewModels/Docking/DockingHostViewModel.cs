using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels.Docking;

public class DockingHostViewModel : ViewModelBase, IRoutableViewModel
{
    // IRoutableViewModel
    public string UrlPathSegment { get; } = nameof(DockingHostViewModel);
    public IScreen HostScreen { get; }

    public ObservableCollection<DocumentViewModel> LeftPane { get; } = new();
    public ObservableCollection<DocumentViewModel> RightPane { get; } = new();

    private DocumentViewModel? _leftSelected;
    public DocumentViewModel? LeftSelected
    {
        get => _leftSelected;
        set => this.RaiseAndSetIfChanged(ref _leftSelected, value);
    }

    private DocumentViewModel? _rightSelected;
    public DocumentViewModel? RightSelected
    {
        get => _rightSelected;
        set => this.RaiseAndSetIfChanged(ref _rightSelected, value);
    }

    public DockingHostViewModel(IScreen host)
    {
        HostScreen = host ?? throw new ArgumentNullException(nameof(host));

        // when selection changes, raise property changed for ActiveDocument
        this.WhenAnyValue(x => x.LeftSelected, x => x.RightSelected)
            .Select(tuple => tuple.Item2 ?? tuple.Item1)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(ActiveDocument)));
    }

    public DocumentViewModel? ActiveDocument => RightSelected ?? LeftSelected;

    public async Task OpenFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // read file content
        string content = string.Empty;
        if (File.Exists(path))
        {
            content = await File.ReadAllTextAsync(path);
        }

        var doc = new DocumentViewModel(HostScreen, Path.GetFileName(path))
        {
            FilePath = path,
            Text = content
        };

        // add to left pane by default
        LeftPane.Add(doc);
        LeftSelected = doc;
    }

    public async Task SaveActiveDocumentAsync(string? path)
    {
        var active = ActiveDocument;
        if (active == null) return;

        var savePath = path;
        if (string.IsNullOrWhiteSpace(savePath)) savePath = active.FilePath;

        if (string.IsNullOrWhiteSpace(savePath))
        {
            // No path given; caller (view) should prompt for SaveAs. We'll just return.
            return;
        }

        await File.WriteAllTextAsync(savePath, active.Text ?? string.Empty);
        active.FilePath = savePath;
        active.IsDirty = false;
    }

    public void MoveToOtherPane(DocumentViewModel doc)
    {
        if (doc == null) return;
        if (LeftPane.Contains(doc))
        {
            LeftPane.Remove(doc);
            RightPane.Add(doc);
            RightSelected = doc;
        }
        else if (RightPane.Contains(doc))
        {
            RightPane.Remove(doc);
            LeftPane.Add(doc);
            LeftSelected = doc;
        }
    }

    public void CloseDocument(DocumentViewModel doc)
    {
        if (LeftPane.Contains(doc))
        {
            if (LeftSelected == doc) LeftSelected = null;
            LeftPane.Remove(doc);
        }
        if (RightPane.Contains(doc))
        {
            if (RightSelected == doc) RightSelected = null;
            RightPane.Remove(doc);
        }
    }
}

