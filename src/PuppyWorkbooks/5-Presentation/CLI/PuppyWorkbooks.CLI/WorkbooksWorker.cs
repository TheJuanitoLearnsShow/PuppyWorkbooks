using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.CLI;

public class ExecutionSettings
{
    /// <summary>
    /// Input path to JSON object containing a dictionary of formula values to inject
    /// into the workbooks. The value will be injected in each workbook that contains a
    /// formula with the name in key of the value.
    /// </summary>
    public string? InputDataPath { get; set; }
    
    public Dictionary<string, string> InputData { get; set; } = new();

    /// <summary>
    /// List of workbooks to execute. All the outputs of each workbook will be included in the
    /// results collection in the output.
    /// </summary>
    public string[] WorkbookPaths { get; set; } = [];

    public string? OutputPath { get; set; }
}

public sealed class WorkbooksWorker : IHostedService
{
    private readonly ILogger _logger;
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


    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var workbookPaths = _settings.WorkbookPaths;    
        var inputValues = LoadInputValues();
        foreach (var path in workbookPaths)
        {
            var workbook = _workSheetSerializer.DeserializeFromXmlFile(path);
            foreach (var inputValue in inputValues)
            {   
                workbook.SetFormulaValue(inputValue.Key, inputValue.Value);
            }
            var interpreter = new WorkbookInterpreter();
            await foreach (var result in interpreter.ExecuteAsync(workbook, yieldResultsForEachCell: true, cancellationToken: cancellationToken))
            {
            }
        }
        return;
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