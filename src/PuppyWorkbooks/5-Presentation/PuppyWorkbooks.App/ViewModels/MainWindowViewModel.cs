using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Utils;
using PuppyWorkbooks.App.ViewModels.Docking;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public ObservableCollection<DocumentViewModel> LeftDocuments { get; } =
        new ObservableCollection<DocumentViewModel>();

    public ObservableCollection<DocumentViewModel> RightDocuments { get; } =
        new ObservableCollection<DocumentViewModel>();

    private DocumentViewModel? _selectedLeftDocument;
    public DocumentViewModel? SelectedLeftDocument
    {
        get => _selectedLeftDocument;
        set => this.RaiseAndSetIfChanged(ref _selectedLeftDocument, value);
    }

    private DocumentViewModel? _selectedRightDocument;
    public DocumentViewModel? SelectedRightDocument
    {
        get => _selectedRightDocument;
        set => this.RaiseAndSetIfChanged(ref _selectedRightDocument, value);
    }

    public Func<IStorageProvider>? GetStorageProvider { get; set; }

    public ReactiveCommand<Unit, Unit> SaveActiveDocumentCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsActiveDocumentCommand { get; }

    public MainWindowViewModel()
    {
        NewDocumentCommand = ReactiveCommand.Create(AddNewDocument);
        SaveActiveDocumentCommand = ReactiveCommand.CreateFromTask(SaveActiveDocumentAsync);
        SaveAsActiveDocumentCommand = ReactiveCommand.CreateFromTask(SaveAsActiveDocumentAsync);
    }

    public ReactiveCommand<Unit, Unit> NewDocumentCommand { get; set; }


    private void AddNewDocument()
    {
        var doc = new DocumentViewModel
        {
            Title = $"Document {LeftDocuments.Count + RightDocuments.Count + 1}"
        };

        LeftDocuments.Add(doc);
        SelectedLeftDocument = doc;
    }

    private void SaveActiveDocument()
    {
        var active = SelectedLeftDocument ?? SelectedRightDocument;
        active?.SaveCommand.Execute().Subscribe();
    }
    private async Task SaveActiveDocumentAsync()
    {
        var doc = SelectedLeftDocument ?? SelectedRightDocument;
        if (doc == null) return;

        var provider = GetStorageProvider?.Invoke();
        if (provider == null) return;

        // Ask user where to save
        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Document",
            SuggestedFileName = doc.Title + ".txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(doc.Text);

            doc.IsDirty = false;
        }
    }

    private async Task SaveAsActiveDocumentAsync()
    {
        await SaveActiveDocumentAsync();
    }

}
