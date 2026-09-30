using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Providers;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class MockManager
{
    private readonly IntegrationRunnerOptions _options;

    public MockManager(IntegrationRunnerOptions options)
    {
        _options = options;
    }

    public bool ShouldUseMock(IntegrationStep step)
    {
        if (string.IsNullOrWhiteSpace(_options.UseMockDataForSteps))
            return false;

        var setting = _options.UseMockDataForSteps.Trim();
        if (string.Equals(setting, "ALL", StringComparison.OrdinalIgnoreCase))
            return true;

        var stepIds = setting.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return stepIds.Any(id => string.Equals(id, step.Id, StringComparison.OrdinalIgnoreCase));
    }

    public IInputProvider CreateMockInput(InputStep step)
    {
        var mockSource = SelectMockDataSource(step);
        if (mockSource is null)
        {
            throw new InvalidOperationException($"Mock data was requested for input step '{step.Id}', but no mock CSV data or file path was defined in the step.");
        }

        if (step.Kind == InputKind.HttpReader)
        {
            if (!string.IsNullOrWhiteSpace(mockSource.FilePath))
            {
                return JsonInputProvider.FromFile(mockSource.FilePath, step.JsonPath);
            }

            var inlineJson = !string.IsNullOrWhiteSpace(mockSource.Content) ? mockSource.Content : mockSource.RawText;
            if (!string.IsNullOrWhiteSpace(inlineJson))
            {
                return JsonInputProvider.FromText(inlineJson.Trim(), step.JsonPath);
            }

            throw new InvalidOperationException($"Mock data was requested for HTTP input step '{step.Id}', but no mock JSON data or file path was defined in the step.");
        }

        if (!string.IsNullOrWhiteSpace(mockSource.FilePath))
        {
            return new CsvInputProvider(mockSource.FilePath);
        }

        var inlineCsv = !string.IsNullOrWhiteSpace(mockSource.Content) ? mockSource.Content : mockSource.RawText;
        if (!string.IsNullOrWhiteSpace(inlineCsv))
        {
            return CsvInputProvider.FromText(inlineCsv.Trim());
        }

        throw new InvalidOperationException($"Mock data was requested for input step '{step.Id}', but no mock CSV data or file path was defined in the step.");
    }

    private MockDataSource? SelectMockDataSource(InputStep step)
    {
        if (step.MockDataSources.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(_options.Scenario))
            {
                var scenarioKey = _options.Scenario;
                if (step.MockDataSources.TryGetValue(scenarioKey, out var matchedSource))
                {
                    return matchedSource;
                }
            }

            return step.MockDataSources.Values.FirstOrDefault();
        }

        if (!string.IsNullOrWhiteSpace(step.MockCsvFilePath))
        {
            return new MockDataSource { FilePath = step.MockCsvFilePath };
        }

        var legacyInline = !string.IsNullOrWhiteSpace(step.MockCsv) ? step.MockCsv : step.MockData;
        if (!string.IsNullOrWhiteSpace(legacyInline))
        {
            return new MockDataSource { Content = legacyInline };
        }

        return null;
    }
}