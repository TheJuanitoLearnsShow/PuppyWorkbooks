using System;
using System.Reactive;
using ReactiveUI;

namespace PuppyWorkbooks.App.ViewModels.Docking;

public class DocumentViewModel : ReactiveObject
{
    private string _text = "";
    private bool _isDirty;

    public string Title { get; set; } = "Untitled";

    public string Text
    {
        get => _text;
        set
        {
            this.RaiseAndSetIfChanged(ref _text, value);
            IsDirty = true;
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public string DisplayTitle => IsDirty ? $"{Title} *" : Title;

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    public DocumentViewModel()
    {
        SaveCommand = ReactiveCommand.Create(Save);
    }

    private void Save()
    {
        // Real save is handled by MainWindowViewModel using dialogs
        // This just clears the dirty flag
        IsDirty = false;
    }
}
