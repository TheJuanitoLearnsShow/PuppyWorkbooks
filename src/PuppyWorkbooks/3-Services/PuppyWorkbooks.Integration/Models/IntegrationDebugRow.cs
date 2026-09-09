namespace PuppyWorkbooks.Integration.Models;

public sealed class IntegrationDebugRow
{
    public Dictionary<string, object?> InputRow { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<IntegrationStepDebug> Steps { get; set; } = [];
}
