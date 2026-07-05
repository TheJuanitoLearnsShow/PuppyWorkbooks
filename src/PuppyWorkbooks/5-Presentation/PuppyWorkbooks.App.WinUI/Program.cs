// Program.cs
using Microsoft.UI.Reactor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PowerFxWorkbookApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ReactorApp.Run<MainWindow>(
                title: "PowerFx Workbook",
                width: 1100,
                height: 700);
        }
    }
}

// Models.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace PowerFxWorkbookApp
{
    [Serializable]
    public class FormulaEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    [Serializable]
    public class Workbook
    {
        public string WorkbookName { get; set; } = "Untitled Workbook";
        public List<FormulaEntry> Entries { get; set; } = new();
    }

    public static class WorkbookStorage
    {
        public static Workbook NewWorkbook() => new Workbook
        {
            WorkbookName = "Untitled Workbook",
            Entries = new List<FormulaEntry>()
        };

        public static Workbook LoadFromFile(string path)
        {
            using var fs = File.OpenRead(path);
            var serializer = new XmlSerializer(typeof(Workbook));
            return (Workbook)serializer.Deserialize(fs)!;
        }

        public static void SaveToFile(Workbook workbook, string path)
        {
            using var fs = File.Create(path);
            var serializer = new XmlSerializer(typeof(Workbook));
            serializer.Serialize(fs, workbook);
        }
    }
}

// MainWindow.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Reactor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PowerFxWorkbookApp
{
    public class MainWindow : Component
    {
        public override Element Render()
        {
            var (workbook, setWorkbook) = UseState(WorkbookStorage.NewWorkbook());
            var (selectedIndex, setSelectedIndex) = UseState(0);
            var (currentFilePath, setCurrentFilePath) = UseState<string?>(null);

            UseEffect(() =>
            {
                // Command-line: first non-exe argument is XML path
                var args = Environment.GetCommandLineArgs();
                var xmlArg = args.Skip(1).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(xmlArg) && File.Exists(xmlArg))
                {
                    try
                    {
                        var loaded = WorkbookStorage.LoadFromFile(xmlArg);
                        setWorkbook(loaded);
                        setCurrentFilePath(xmlArg);
                        setSelectedIndex(loaded.Entries.Count > 0 ? 0 : -1);
                    }
                    catch
                    {
                        // ignore load errors for now
                    }
                }
            }, Array.Empty<object>());

            async Task SaveAsync()
            {
                if (currentFilePath is { Length: > 0 } existing)
                {
                    WorkbookStorage.SaveToFile(workbook, existing);
                    return;
                }

                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("XML Workbook", new[] { ".xml" });
                picker.SuggestedFileName = workbook.WorkbookName;
                picker.DefaultFileExtension = ".xml";

                // WinUI 3: initialize picker with window handle
                var hwnd = ReactorApp.PrimaryWindow?.Handle;
                if (hwnd != null)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd.Value);
                }

                StorageFile file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    WorkbookStorage.SaveToFile(workbook, file.Path);
                    setCurrentFilePath(file.Path);
                }
            }

            async Task OpenAsync()
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".xml");

                var hwnd = ReactorApp.PrimaryWindow?.Handle;
                if (hwnd != null)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd.Value);
                }

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    try
                    {
                        var loaded = WorkbookStorage.LoadFromFile(file.Path);
                        setWorkbook(loaded);
                        setCurrentFilePath(file.Path);
                        setSelectedIndex(loaded.Entries.Count > 0 ? 0 : -1);
                    }
                    catch
                    {
                        // ignore load errors for now
                    }
                }
            }

            void OnKeyDown(object sender, KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.S &&
                    (Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control)
                     .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)))
                {
                    _ = SaveAsync();
                }
            }

            void AddEntry()
            {
                var clone = WorkbookStorage.NewWorkbook();
                clone.WorkbookName = workbook.WorkbookName;
                clone.Entries = workbook.Entries.ToList();
                clone.Entries.Add(new FormulaEntry
                {
                    Name = "New Formula",
                    Formula = "",
                    Comments = ""
                });
                setWorkbook(clone);
                setSelectedIndex(clone.Entries.Count - 1);
            }

            void UpdateEntry(Action<FormulaEntry> mutator)
            {
                if (selectedIndex < 0 || selectedIndex >= workbook.Entries.Count)
                    return;

                var clone = WorkbookStorage.NewWorkbook();
                clone.WorkbookName = workbook.WorkbookName;
                clone.Entries = workbook.Entries.Select(e => new FormulaEntry
                {
                    Name = e.Name,
                    Formula = e.Formula,
                    Comments = e.Comments
                }).ToList();

                mutator(clone.Entries[selectedIndex]);
                setWorkbook(clone);
            }

            void UpdateWorkbookName(string name)
            {
                var clone = WorkbookStorage.NewWorkbook();
                clone.WorkbookName = name;
                clone.Entries = workbook.Entries.Select(e => new FormulaEntry
                {
                    Name = e.Name,
                    Formula = e.Formula,
                    Comments = e.Comments
                }).ToList();
                setWorkbook(clone);
            }

            void RunAll()
            {
                // TODO: PowerFx evaluation for all formulas
            }

            void RunUpToSelected()
            {
                // TODO: PowerFx evaluation up to selectedIndex
            }

            var selectedEntry = (selectedIndex >= 0 && selectedIndex < workbook.Entries.Count)
                ? workbook.Entries[selectedIndex]
                : null;

            return Grid(
                RowDefinitions("Auto", "*"),
                ColumnDefinitions("*"),
                // Toolbar row
                CommandBar(
                    AppBarButton("New", AddEntry)
                        .Icon("Add"),
                    AppBarButton("Open...", async () => await OpenAsync())
                        .Icon("OpenFile"),
                    AppBarButton("Save", async () => await SaveAsync())
                        .Icon("Save"),
                    AppBarSeparator(),
                    AppBarButton("Run all", RunAll)
                        .Icon("Play"),
                    AppBarButton("Run to selection", RunUpToSelected)
                        .Icon("Play"),
                    AppBarSeparator(),
                    TextBlock(workbook.WorkbookName)
                        .Margin(16, 0, 0, 0)
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Foreground("#0078D4")
                        .FontSize(14)
                )
                .Background(Colors.Transparent)
                .Row(0),

                // Main content row
                Grid(
                    ColumnDefinitions("3*", "Auto", "5*"),
                    // Left list
                    Border(
                        ListView()
                            .Items(workbook.Entries.Select((e, idx) =>
                                ListViewItem(
                                    VStack(
                                        TextBlock(e.Name)
                                            .FontSize(14)
                                            .FontWeight(FontWeights.SemiBold),
                                        TextBlock(e.Formula)
                                            .FontSize(12)
                                            .Foreground("#666666")
                                            .TextTrimming(TextTrimming.CharacterEllipsis)
                                    )
                                ).Tag(idx)
                            ))
                            .SelectedIndex(selectedIndex)
                            .OnSelectionChanged((sender, args) =>
                            {
                                if (sender is ListView lv && lv.SelectedItem is ListViewItem item && item.Tag is int idx)
                                {
                                    setSelectedIndex(idx);
                                }
                            })
                            .Padding(8)
                    )
                    .Background("#F3F3F3")
                    .CornerRadius(4)
                    .Margin(8)
                    .Column(0),

                    // Splitter
                    GridSplitter()
                        .Column(1)
                        .Width(4)
                        .Background("#DDDDDD")
                        .HorizontalAlignment(HorizontalAlignment.Stretch),

                    // Right form
                    Border(
                        VStack(
                            Heading("Formula details")
                                .FontSize(18)
                                .Margin(0, 0, 0, 12),

                            TextBlock("Workbook name")
                                .FontSize(12)
                                .Foreground("#666666"),
                            TextBox(workbook.WorkbookName)
                                .OnTextChanged((tb, args) =>
                                {
                                    UpdateWorkbookName(tb.Text);
                                })
                                .Margin(0, 0, 0, 12),

                            TextBlock("Name")
                                .FontSize(12)
                                .Foreground("#666666"),
                            TextBox(selectedEntry?.Name ?? "")
                                .OnTextChanged((tb, args) =>
                                {
                                    UpdateEntry(e => e.Name = tb.Text);
                                })
                                .Margin(0, 0, 0, 12),

                            TextBlock("Formula")
                                .FontSize(12)
                                .Foreground("#666666"),
                            TextBox(selectedEntry?.Formula ?? "")
                                .AcceptsReturn(true)
                                .TextWrapping(TextWrapping.Wrap)
                                .Height(120)
                                .OnTextChanged((tb, args) =>
                                {
                                    UpdateEntry(e => e.Formula = tb.Text);
                                })
                                .Margin(0, 0, 0, 12),

                            TextBlock("Comments")
                                .FontSize(12)
                                .Foreground("#666666"),
                            TextBox(selectedEntry?.Comments ?? "")
                                .AcceptsReturn(true)
                                .TextWrapping(TextWrapping.Wrap)
                                .Height(80)
                                .OnTextChanged((tb, args) =>
                                {
                                    UpdateEntry(e => e.Comments = tb.Text);
                                })
                        )
                        .Padding(16)
                    )
                    .Background("#FFFFFF")
                    .CornerRadius(4)
                    .Margin(8)
                    .Column(2)
                )
                .Row(1)
                .OnKeyDown(OnKeyDown)
                .Background(Colors.Transparent)
            )
            .Background(Colors.Transparent);
        }
    }
}
