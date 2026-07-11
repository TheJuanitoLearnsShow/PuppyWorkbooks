using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.CLI.Output;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.CLI;

public sealed class WorkbooksWorker : IHostedService
{
    private readonly ILogger? _logger;
    private readonly ExecutionSettings _settings;
    private readonly WorkSheetSerializer _workSheetSerializer = new ();

    public WorkbooksWorker(
        ILogger<WorkbooksWorker> logger,
        IOptions<ExecutionSettings> options,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _settings = options.Value;
    }
    public WorkbooksWorker(ExecutionSettings settings)
    {
        _logger = null;
        _settings = settings;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IOutputWriter outputWriter = new ConsoleOutputWriter();
        try
        {
            var workbookPaths = _settings.WorkbookPaths;
            var inputValues = LoadInputValues();
            outputWriter.OpenWriter();
            await ExecuteWorkbooks(workbookPaths, inputValues, outputWriter, cancellationToken);
        }
        catch (Exception fatalError)
        {
            _logger?.LogError(fatalError.Message);
        }
        finally{
            outputWriter.CloseWriter();
        }
        return;
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

    private async Task ExecuteWorkbook(Dictionary<string, string> inputValues, IOutputWriter outputWriter, string path, CancellationToken cancellationToken)
    {
        try
        {
            var workbook = _workSheetSerializer.DeserializeFromXmlFile(path);
            outputWriter.StartWorkbookResult(workbook.Name);
            foreach (var inputValue in inputValues)
            {   
                workbook.SetFormulaValue(inputValue.Key, inputValue.Value);
            }
            var interpreter = new WorkbookInterpreter();
            await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true, cancellationToken: cancellationToken))
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

    private Dictionary<string, string> LoadInputValues()
    {
        var inputValues = _settings.InputData;
        if (string.IsNullOrEmpty(_settings.InputDataPath))
        {
            return inputValues;
        }
        var valuesFromInputFile = JsonSerializer.Deserialize<
            Dictionary<string, string>>(_settings.InputDataPath);
        if (valuesFromInputFile is not null)
        {
            foreach (var kv in valuesFromInputFile)
            {
                if (!inputValues.ContainsKey(kv.Key))
                {
                    inputValues[kv.Key] = kv.Value;
                }
            }
        }
        return inputValues;
    }


    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("7. StopAsync has been called.");

        return Task.CompletedTask;
    }

}