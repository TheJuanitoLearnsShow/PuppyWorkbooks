using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.CLI.Output;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.CLI;

public sealed class WorkbooksWorker : IHostedService
{
    private readonly ILogger? _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ExecutionSettings _settings;
    private readonly WorkSheetSerializer _workSheetSerializer = new();
    private readonly IntegrationXmlSerializer _integrationSerializer = new();

    public WorkbooksWorker(
        ILogger<WorkbooksWorker> logger,
        IOptions<ExecutionSettings> options,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _settings = options.Value;
    }

    public WorkbooksWorker(ExecutionSettings settings)
    {
        _logger = null;
        _settings = settings;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cmdArgs = Environment.GetCommandLineArgs();

        try
        {
            await Start(cancellationToken, cmdArgs);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Fatal error executing workbooks or integration.");
        }
        _appLifetime?.StopApplication();
        return;
    }

    private async Task Start(CancellationToken cancellationToken, string[] cmdArgs)
    {
        var isDebug = _settings.Debug || cmdArgs.Any(IsDebugArgument);
        var mockSteps = !string.IsNullOrWhiteSpace(_settings.UseMockDataForSteps)
            ? _settings.UseMockDataForSteps
            : GetMockArgument(cmdArgs);
        var simplePaths = GetPositionalArguments(cmdArgs);
        if (simplePaths.Count == 1)
        {
            var rootName = GetFirstXmlNodeName(simplePaths[0]);
            switch (rootName)
            {
                case "Integration":
                    await ExecuteIntegration(simplePaths[0], isDebug, mockSteps, cancellationToken);
                    return;
                case "Workbook":
                    await ExecuteWorksheets(cancellationToken);
                    return;
            }
        }
        if (!string.IsNullOrWhiteSpace(_settings.IntegrationPath))
        {
            await ExecuteIntegration(_settings.IntegrationPath, isDebug, mockSteps, cancellationToken);
            return;
        }

        await ExecuteWorksheets(cancellationToken);
        return;
    }

    private static List<string> GetPositionalArguments(string[] cmdArgs)
    {
        var result = new List<string>();
        for (var i = 1; i < cmdArgs.Length; i++)
        {
            var arg = cmdArgs[i];
            if (string.IsNullOrWhiteSpace(arg)) continue;
            if (arg.StartsWith('-') || arg.StartsWith('/'))
            {
                if (!IsDebugArgument(arg) && !arg.Contains('=') && i + 1 < cmdArgs.Length && !cmdArgs[i + 1].StartsWith('-') && !cmdArgs[i + 1].StartsWith('/'))
                {
                    i++;
                }
                continue;
            }
            result.Add(arg);
        }
        return result;
    }

    private static string? GetMockArgument(string[] cmdArgs)
    {
        for (var i = 1; i < cmdArgs.Length; i++)
        {
            var arg = cmdArgs[i];
            var equalIndex = arg.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = arg[..equalIndex];
                var val = arg[(equalIndex + 1)..];
                if (IsMockKey(key)) return val;
            }
            else if (IsMockKey(arg) && i + 1 < cmdArgs.Length)
            {
                return cmdArgs[i + 1];
            }
        }
        return null;
    }

    private static bool IsMockKey(string key) =>
        string.Equals(key, "--use-mock-data-for-steps", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--useMockDataForSteps", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-useMockDataForSteps", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "/useMockDataForSteps", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--useMockData", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-useMockData", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "/useMockData", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--mock-data", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--mock", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-m", StringComparison.OrdinalIgnoreCase);

    private string GetFirstXmlNodeName(string simplePath)
    {
        if (string.IsNullOrWhiteSpace(simplePath) || !File.Exists(simplePath))
            return string.Empty;

        using var reader = XmlReader.Create(simplePath, new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Prohibit
        });

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
                return reader.LocalName;
        }

        return string.Empty;
    }

    private async Task ExecuteWorksheets(CancellationToken cancellationToken)
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
                    workbookPaths = new[] { firstPositional };
                }
            }

            var inputValues = LoadInputValues();
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

    private async Task ExecuteIntegration(string path, bool isDebug, string? mockSteps, CancellationToken cancellationToken)
    {
        try
        {
            var definition = _integrationSerializer.DeserializeFile(path);
            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                Debug = isDebug,
                UseMockDataForSteps = mockSteps ?? string.Empty
            });
            var result = await runner.RunAsync(definition, cancellationToken);
            if (isDebug && result.DebugData is not null)
            {
                var json = JsonSerializer.Serialize(result.DebugData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine(json);
            }
            if (_logger is not null)
                _logger.LogInformation(
                    "Integration {IntegrationName} completed. Read: {Read}, Written: {Written}, Excluded: {Excluded}",
                    definition.Name, result.Read, result.Written, result.Excluded);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error executing integration at path: {Path}", path);
        }
    }

    private static bool IsDebugArgument(string arg) =>
        string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "-debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "/debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "-d", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "--Debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "debug", StringComparison.OrdinalIgnoreCase);

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
        _logger?.LogInformation("7. StopAsync has been called.");

        return Task.CompletedTask;
    }
}