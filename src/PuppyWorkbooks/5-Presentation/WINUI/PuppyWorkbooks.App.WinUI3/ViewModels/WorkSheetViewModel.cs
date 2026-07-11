using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Windows.Storage.Pickers;

namespace PuppyWorkbooks.App.WinUI.ViewModels;

public class WorkSheetViewModel : INotifyPropertyChanged
{
    private WorkSheet _worksheet = new();
    private WorkCell? _selectedCell;
    private string? _currentFilePath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WorkSheetName
    {
        get => _worksheet.Name;
        set
        {
            if (_worksheet.Name != value)
            {
                _worksheet.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<WorkCell> Cells { get; } = new();

    public WorkCell? SelectedCell
    {
        get => _selectedCell;
        set
        {
            if (_selectedCell != value)
            {
                _selectedCell = value;
                OnPropertyChanged();
                RunToSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand RunAllCommand { get; }
    public RelayCommand RunToSelectedCommand { get; }

    public WorkSheetViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save(), _ => true);
        OpenCommand = new RelayCommand(_ => Open(), _ => true);
        RunAllCommand = new RelayCommand(_ => RunAll(), _ => Cells.Count > 0);
        RunToSelectedCommand = new RelayCommand(_ => RunToSelected(), _ => SelectedCell != null);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;

        using var fs = File.OpenRead(path);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(WorkSheet));
        if (serializer.Deserialize(fs) is WorkSheet ws)
        {
            _worksheet = ws;
            _currentFilePath = path;

            Cells.Clear();
            foreach (var c in ws.Cells)
                Cells.Add(c);

            OnPropertyChanged(nameof(WorkSheetName));
            OnPropertyChanged(nameof(Cells));
        }
    }

    private async void Save()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            var picker = new FileSavePicker()
            {
                SuggestedStartLocation = DocumentsLibrary,
                SuggestedFileName = string.IsNullOrWhiteSpace(WorkSheetName) ? "worksheet" : WorkSheetName
            };
            picker.FileTypeChoices.Add("XML", new List<string> { ".xml" });

            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            _currentFilePath = file.Path;
        }

        _worksheet.Cells = Cells.ToList();

        await using var fs = File.Create(_currentFilePath);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(WorkSheet));
        serializer.Serialize(fs, _worksheet);
    }

    private async void Open()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".xml");

        WinRT.Interop.InitializeWithWindow.Initialize(picker,
            WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        _currentFilePath = file.Path;
        LoadFromFile(file.Path);
    }

    private void RunAll()
    {
        // TODO: PowerFx interpretation for all Cells
        // Use _worksheet.Cells
    }

    private void RunToSelected()
    {
        if (SelectedCell == null) return;
        // TODO: PowerFx interpretation for Cells up to SelectedCell
    }
}