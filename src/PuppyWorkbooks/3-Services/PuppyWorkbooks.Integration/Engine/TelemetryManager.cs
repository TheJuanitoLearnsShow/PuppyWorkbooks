using System.Diagnostics;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class TelemetryManager
{
    public const string ActivitySourceName = "PuppyWorkbooks.Integration";
    private static readonly ActivitySource TelemetryActivitySource = new(ActivitySourceName);

    public Activity? StartActivity(string operationName, TelemetryOptions? telemetryOptions, IntegrationRecord? record)
    {
        var activity = TelemetryActivitySource.StartActivity(operationName, ActivityKind.Internal);
        if (activity is null) return null;

        ApplyTelemetryFields(activity, record, telemetryOptions);
        return activity;
    }

    public Activity? StartActivity(IntegrationStep step, IntegrationRecord? record)
    {
        var operationName = step switch
        {
            MapStep => $"Map:{step.Id}",
            FilterStep => $"Filter:{step.Id}",
            ReduceStep => $"Reduce:{step.Id}",
            SwitchStep => $"Switch:{step.Id}",
            RunIntegrationStep => $"RunIntegration:{step.Id}",
            InputStep => $"Input:{step.Id}",
            OutputStep => $"Output:{step.Id}",
            _ => $"Step:{step.Id}"
        };

        return StartActivity(operationName, step.TelemetryOptions, record);
    }

    public void ApplyTelemetryFields(Activity? activity, IntegrationRecord? record, TelemetryOptions? telemetryOptions)
    {
        if (activity is null || record is null || telemetryOptions is null || telemetryOptions.FieldNames.Count == 0)
            return;
        var validFieldNames = telemetryOptions.FieldNames.
            Select(field => field?.Trim()).
            Where(name => !string.IsNullOrWhiteSpace(name)).
            ToList();
        foreach (var name in validFieldNames)
        {
            if (!record.Values.TryGetValue(name!, out var value)) continue;
            activity.SetTag(name!, ValueBinder.FormatInputValue(value));
        }
    }
}