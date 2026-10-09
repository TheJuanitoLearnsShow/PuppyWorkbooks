using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppyWorkbooks.Integration;
using PuppyWorkbooks.Integration.Engine;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.CLI;

public class PuppyIntegrationWorker
{
    private readonly ILogger<PuppyIntegrationWorker> _logger;
    private readonly ExecutionSettings _settings;
    private readonly IntegrationXmlSerializer _integrationSerializer = new();

    public PuppyIntegrationWorker(
        ILogger<PuppyIntegrationWorker> logger,
        IOptions<ExecutionSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public PuppyIntegrationWorker(ExecutionSettings settings)
    {
        _logger = null;
        _settings = settings;
    }
    
    public async Task ExecuteIntegration(string path, bool isDebug, string? mockSteps, string? scenario, CancellationToken cancellationToken)
    {
        try
        {
            var definition = _integrationSerializer.DeserializeFile(path);
            var inputValues = InputDataProvider.LoadInputValuesAsJson(_settings);

            var runner = new IntegrationRunner(new IntegrationRunnerOptions
            {
                Debug = isDebug,
                UseMockDataForSteps = mockSteps ?? string.Empty,
                Scenario = scenario,
                InputData = inputValues,
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
}