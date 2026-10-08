using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.CLI.Output;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.CLI;

public class PuppyWorksheetWorker
{
    private readonly ILogger<PuppyWorksheetWorker>? _logger;
    private readonly ExecutionSettings _settings;
    private readonly WorkSheetSerializer _workSheetSerializer = new();

    public PuppyWorksheetWorker(
        ILogger<PuppyWorksheetWorker> logger,
        IOptions<ExecutionSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }
    
    public PuppyWorksheetWorker(ExecutionSettings settings)
    {
        _logger = null;
        _settings = settings;
    }
    public async Task ExecuteWorksheets(CancellationToken cancellationToken)
    {
        using IOutputWriter outputWriter = new ConsoleOutputWriter();
        try
        {
            var workbookPaths = _settings.WorkbookPaths;
            if (workbookPaths == null || workbookPaths.Length == 0)
            {
                var firstPositional = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(a =>
                    !string.IsNullOrWhiteSpace(a) && !a.StartsWith("-") && !a.StartsWith("/"));
                if (!string.IsNullOrEmpty(firstPositional))
                {
                    workbookPaths = [firstPositional];
                }
            }

            var inputValues = InputDataProvider.LoadInputValues(_settings);
            outputWriter.OpenWriter();
            await ExecuteWorkbooks(workbookPaths, inputValues, outputWriter, cancellationToken);
        }
        catch (Exception fatalError)
        {
            _logger?.LogError(fatalError.Message);
        }
        finally
        {
            outputWriter.CloseWriter();
        }
    }
    
    private async Task ExecuteWorkbooks(
        string[] workbookPaths,
        Dictionary<string, string> inputValues,
        IOutputWriter outputWriter,
        CancellationToken cancellationToken)
    {
        foreach (var path in workbookPaths)
        {
            await ExecuteWorkbook(inputValues, outputWriter, path, cancellationToken);
        }
    }

    private async Task ExecuteWorkbook(Dictionary<string, string> inputValues, IOutputWriter outputWriter, string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var workbook = _workSheetSerializer.DeserializeFromXmlFile(path);
            outputWriter.StartWorkbookResult(workbook.Name);
            foreach (var inputValue in inputValues)
            {
                workbook.SetInputValue(inputValue.Key, inputValue.Value);
            }

            var interpreter = new WorkbookInterpreter();
            await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true,
                               cancellationToken: cancellationToken))
            {
                outputWriter.WriteCellResult(result);
            }

            outputWriter.EndWorkbookResult();
        }
        catch (Exception e)
        {
            _logger?.LogError(e.Message, "Error executing workbook at path: {Path}", path);
        }
    }

}