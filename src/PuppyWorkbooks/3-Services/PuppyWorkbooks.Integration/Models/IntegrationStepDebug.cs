using System.Text.Json.Serialization;

namespace PuppyWorkbooks.Integration.Models;

public sealed class IntegrationStepDebug
{
    public string Id { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public Dictionary<string, object?> Cells { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<IntegrationBranchDebug>? Branches { get; set; }

}
