using Microsoft.UI;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using PuppyWorkbooks;
using PuppyWorkbooks.WinUI.Reactor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using static Microsoft.UI.Reactor.Factories;

var cmdLineArgs = Environment.GetCommandLineArgs();
var parser = new CommandLineParser(cmdLineArgs.Skip(1).ToArray());

if (parser.HasArgument("help"))
{
    Console.WriteLine(CommandLineParser.GetHelpText());
    return;
}

ReactorApp.Run<App>("PuppyWorkbooks Workbook", width: 1400, height: 900);

class App : Component
{
    public override Element Render()
    {
        var window = UseWindow();
        var (worksheet, setWorksheet) = UseState(WorkSheetStorage.NewWorkSheet());
        var (selectedIndex, setSelectedIndex) = UseState(0);
        var (currentFilePath, setCurrentFilePath) = UseState<string?>(null);
        var (statusMessage, setStatusMessage) = UseState<string?>(null);

        UseEffect(() =>
        {
            var initialPath = GetInitialWorkbookPath();
            if (!string.IsNullOrWhiteSpace(initialPath) && File.Exists(initialPath))
            {
                _ = LoadWorkbookFromPathAsync(initialPath, setWorksheet, setSelectedIndex, setCurrentFilePath, setStatusMessage);
            }
        }, Array.Empty<object>());

        void InitializePicker(object picker)
        {
            if (window is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
        }

        async Task SaveAsync()
        {
            var targetPath = currentFilePath;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                var picker = new FileSavePicker();
                picker.FileTypeChoices.Add("XML Workbook", new[] { ".xml" });
                picker.SuggestedFileName = string.IsNullOrWhiteSpace(worksheet.Name) ? "Workbook" : worksheet.Name;
                picker.DefaultFileExtension = ".xml";
                InitializePicker(picker);

                var file = await picker.PickSaveFileAsync();
                if (file is null)
                {
                    return;
                }

                targetPath = file.Path;
            }

            WorkSheetStorage.SaveToFile(worksheet, targetPath!);
            setCurrentFilePath(targetPath);
            setStatusMessage($"Saved to {Path.GetFileName(targetPath)}");
        }

        async Task OpenAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".xml");
            InitializePicker(picker);

            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            await LoadWorkbookFromPathAsync(file.Path, setWorksheet, setSelectedIndex, setCurrentFilePath, setStatusMessage);
        }

        void AddCell()
        {
            var clone = CloneWorksheet(worksheet);
            clone.Cells.Add(new WorkCell
            {
                Id = clone.Cells.Count + 1,
                Name = "New formula",
                Formula = string.Empty,
                Comments = string.Empty
            });
            setWorksheet(clone);
            setSelectedIndex(clone.Cells.Count - 1);
            setStatusMessage("Added a new formula entry");
        }

        void UpdateWorksheetName(string name)
        {
            var clone = CloneWorksheet(worksheet);
            clone.Name = name;
            setWorksheet(clone);
        }

        void UpdateSelectedCell(Action<WorkCell> mutator)
        {
            if (selectedIndex < 0 || selectedIndex >= worksheet.Cells.Count)
            {
                return;
            }

            var clone = CloneWorksheet(worksheet);
            mutator(clone.Cells[selectedIndex]);
            setWorksheet(clone);
        }

        void RunAll()
        {
            setStatusMessage("Run all is ready for Power Fx integration");
        }

        void RunUpToSelected()
        {
            setStatusMessage("Run to selection is ready for Power Fx integration");
        }

        var selectedCell = selectedIndex >= 0 && selectedIndex < worksheet.Cells.Count
            ? worksheet.Cells[selectedIndex]
            : null;

        var toolbar = FlexRow(
            Button("New entry", () => AddCell()).Margin(0, 0, 8, 0),
            Button("Open", () => { _ = OpenAsync(); }).Margin(0, 0, 8, 0),
            Button("Save", () => { _ = SaveAsync(); }).Margin(0, 0, 8, 0),
            Button("Run all", RunAll).Margin(0, 0, 8, 0),
            Button("Run to selection", RunUpToSelected).Margin(0, 0, 8, 0),
            TextBlock(string.IsNullOrWhiteSpace(worksheet.Name) ? "Untitled worksheet" : worksheet.Name)
                .Margin(16, 0, 0, 0)
                .Foreground("#0F6CBD")
                .FontSize(14)
        )
        .Padding(0, 0, 0, 8);

        var listVw = ListView(
                worksheet.Cells,
                cell => cell.Name,
                (cell, index) => VStack(
                    TextBlock(cell.Name)
                        .FontSize(14),
                    TextBlock(cell.Formula)
                        .FontSize(12)
                        .Foreground("#666666")
                ).Padding(8)
            )
            .Padding(8)
            .SelectedIndexChanged(setSelectedIndex);

        var leftList = Border(
            listVw
        )
        .Background("#F5F7FA")
        .CornerRadius(12)
        .Width(320)
        .Padding(4);

        var detailForm = Border(
            VStack(
                Heading("Formula details")
                    .FontSize(20)
                    .Margin(0, 0, 0, 12),

                TextBlock("Worksheet name")
                    .FontSize(12)
                    .Foreground("#6B7280"),
                TextBox(worksheet.Name, text => UpdateWorksheetName(text), "Worksheet name")
                    .Margin(0, 0, 0, 12),

                TextBlock("Name")
                    .FontSize(12)
                    .Foreground("#6B7280"),
                TextBox(selectedCell?.Name ?? string.Empty, text => UpdateSelectedCell(cell => cell.Name = text), "Name")
                    .Margin(0, 0, 0, 12),

                TextBlock("Formula")
                    .FontSize(12)
                    .Foreground("#6B7280"),
                TextBox(selectedCell?.Formula ?? string.Empty, text => UpdateSelectedCell(cell => cell.Formula = text), "Formula")
                    .AcceptsReturn(true)
                    .TextWrapping(TextWrapping.Wrap)
                    .Height(140)
                    .Margin(0, 0, 0, 12),

                TextBlock("Comments")
                    .FontSize(12)
                    .Foreground("#6B7280"),
                TextBox(selectedCell?.Comments ?? string.Empty, text => UpdateSelectedCell(cell => cell.Comments = text), "Comments")
                    .AcceptsReturn(true)
                    .TextWrapping(TextWrapping.Wrap)
                    .Height(100)
                    .Margin(0, 0, 0, 12),

                TextBlock(statusMessage ?? string.Empty)
                    .FontSize(12)
                    .Foreground("#0F6CBD")
                    .Margin(0, 12, 0, 0)
            )
            .Padding(20)
        )
        .Background("#FFFFFF")
        .CornerRadius(12)
        .Padding(4)
        .Flex(1);

        var mainContent = FlexRow(leftList, Border(TextBlock(string.Empty)).Width(8).Background("#E5E7EB").Margin(12, 0, 12, 0), detailForm)
            .Padding(0, 0, 0, 8);

        return FlexColumn(toolbar, mainContent)
            .Background("#F3F4F6")
            .Padding(12)
            .OnKeyDown((_,e) =>
            {
                if (e is KeyRoutedEventArgs args && IsCtrlSPressed(args))
                {
                    _ = SaveAsync();
                    args.Handled = true;
                }
                else if (e is KeyRoutedEventArgs args2 && IsCtrlOPressed(args2))
                {
                    _ = OpenAsync();
                    args2.Handled = true;
                }
                else if (e is KeyRoutedEventArgs args3 && IsCtrlNPressed(args3))
                {
                    AddCell();
                    args3.Handled = true;
                }
            });
    }

    private static string? GetInitialWorkbookPath()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        return args.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
    }

    private static async Task LoadWorkbookFromPathAsync(string path, Action<WorkSheet> setWorksheet, Action<int> setSelectedIndex, Action<string?> setCurrentFilePath, Action<string?> setStatusMessage)
    {
        try
        {
            var loaded = WorkSheetStorage.LoadFromFile(path);
            setWorksheet(loaded);
            setSelectedIndex(loaded.Cells.Count > 0 ? 0 : -1);
            setCurrentFilePath(path);
            setStatusMessage($"Loaded {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            setStatusMessage($"Could not load {path}: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private static bool IsCtrlSPressed(KeyRoutedEventArgs e)
    {
        return e.Key == Windows.System.VirtualKey.S && Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static bool IsCtrlOPressed(KeyRoutedEventArgs e)
    {
        return e.Key == Windows.System.VirtualKey.O && Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static bool IsCtrlNPressed(KeyRoutedEventArgs e)
    {
        return e.Key == Windows.System.VirtualKey.N && Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
    }

    private static WorkSheet CloneWorksheet(WorkSheet worksheet)
    {
        return new WorkSheet
        {
            Name = worksheet.Name,
            Cells = worksheet.Cells.Select(cell => new WorkCell
            {
                Id = cell.Id,
                Name = cell.Name,
                Formula = cell.Formula,
                Comments = cell.Comments
            }).ToList()
        };
    }
}

internal sealed class CommandLineParser
{
    private readonly IReadOnlyList<string> _args;

    public CommandLineParser(IEnumerable<string> args)
    {
        _args = args.ToList();
    }

    public bool HasArgument(string name)
    {
        return _args.Any(arg => string.Equals(arg, $"--{name}", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, $"-{name}", StringComparison.OrdinalIgnoreCase));
    }

    public static string GetHelpText() => "Usage: PuppyWorkbooks.WinUI.Reactor [--help] [path-to-workbook.xml]";
}

namespace PuppyWorkbooks.WinUI.Reactor
{
    public static class WorkSheetStorage
    {
        public static WorkSheet NewWorkSheet() => new WorkSheet
        {
            Name = "Untitled Worksheet",
            Cells = new List<WorkCell>()
        };

        public static WorkSheet LoadFromFile(string path)
        {
            var serializer = new PuppyWorkbooks.Serialization.WorkSheetSerializer();
            return serializer.DeserializeFromXmlFile(path);
        }

        public static void SaveToFile(WorkSheet worksheet, string path)
        {
            var serializer = new PuppyWorkbooks.Serialization.WorkSheetSerializer();
            serializer.SerializeToXmlFile(path, worksheet);
        }
    }
}