using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.CLI.Output;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Serialization;

namespace PuppyWorkbooks.CLI;

public sealed class WorkbooksWorker : IHostedService
{
    private readonly ILogger? _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly PuppyWorksheetWorker _worksheetWorker;
    private readonly PuppyIntegrationWorker _integrationWorker;
    private readonly ExecutionSettings _settings;

    public WorkbooksWorker(
        ILogger<WorkbooksWorker> logger,
        IOptions<ExecutionSettings> options,
        IHostApplicationLifetime appLifetime,
        PuppyWorksheetWorker worksheetWorker,
        PuppyIntegrationWorker integrationWorker)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _worksheetWorker = worksheetWorker;
        _integrationWorker = integrationWorker;
        _settings = options.Value;
    }

    public WorkbooksWorker(ExecutionSettings settings)
    {
        _logger = null;
        _settings = settings;
        _worksheetWorker = new PuppyWorksheetWorker(settings);
        _integrationWorker = new PuppyIntegrationWorker(settings);
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
        _settings.InputDataPath ??= GetInputDataPathArgument(cmdArgs);
        var isDebug = _settings.Debug || cmdArgs.Any(IsDebugArgument);
        var mockSteps = !string.IsNullOrWhiteSpace(_settings.UseMockDataForSteps)
            ? _settings.UseMockDataForSteps
            : GetMockArgument(cmdArgs);
        var scenario = !string.IsNullOrWhiteSpace(_settings.Scenario)
            ? _settings.Scenario
            : GetScenarioArgument(cmdArgs);
        var simplePaths = GetPositionalArguments(cmdArgs);
        if (simplePaths.Count == 1)
        {
            var rootName = GetFirstXmlNodeName(simplePaths[0]);
            switch (rootName)
            {
                case "Integration":
                    await _integrationWorker.ExecuteIntegration(simplePaths[0], isDebug, mockSteps, scenario, cancellationToken);
                    return;
                case "Workbook":
                    await _worksheetWorker.ExecuteWorksheets(cancellationToken);
                    return;
            }
        }
        if (!string.IsNullOrWhiteSpace(_settings.IntegrationPath))
        {
            await _integrationWorker.ExecuteIntegration(_settings.IntegrationPath, isDebug, mockSteps, scenario, cancellationToken);
            return;
        }

        await _worksheetWorker.ExecuteWorksheets(cancellationToken);
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

    private static string? GetScenarioArgument(string[] cmdArgs)
    {
        for (var i = 1; i < cmdArgs.Length; i++)
        {
            var arg = cmdArgs[i];
            var equalIndex = arg.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = arg[..equalIndex];
                var val = arg[(equalIndex + 1)..];
                if (IsScenarioKey(key)) return val;
            }
            else if (IsScenarioKey(arg) && i + 1 < cmdArgs.Length)
            {
                return cmdArgs[i + 1];
            }
        }
        return null;
    }

    private static bool IsScenarioKey(string key) =>
        string.Equals(key, "--scenario", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-scenario", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "/scenario", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--scenario-name", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--scenarioName", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-scenarioName", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "/scenarioName", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-s", StringComparison.OrdinalIgnoreCase);

    private static string? GetInputDataPathArgument(string[] cmdArgs)
    {
        for (var i = 1; i < cmdArgs.Length; i++)
        {
            var arg = cmdArgs[i];
            var equalIndex = arg.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = arg[..equalIndex];
                var val = arg[(equalIndex + 1)..];
                if (IsInputDataPathKey(key)) return val;
            }
            else if (IsInputDataPathKey(arg) && i + 1 < cmdArgs.Length)
            {
                return cmdArgs[i + 1];
            }
        }
        return null;
    }

    private static bool IsInputDataPathKey(string key) =>
        string.Equals(key, "--input-data-path", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--inputDataPath", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-inputDataPath", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "/inputDataPath", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--input-path", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--input", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "--input-json", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, "-i", StringComparison.OrdinalIgnoreCase);

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

    private static bool IsDebugArgument(string arg) =>
        string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "-debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "/debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "-d", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "--Debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "debug", StringComparison.OrdinalIgnoreCase);



    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("7. StopAsync has been called.");

        return Task.CompletedTask;
    }
}