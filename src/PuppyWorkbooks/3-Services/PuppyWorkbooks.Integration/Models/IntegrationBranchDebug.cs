namespace PuppyWorkbooks.Integration.Models;

public sealed class IntegrationBranchDebug
{
    public string WorkCell { get; set; } = string.Empty;
    public bool Executed { get; set; }
    public List<IntegrationStepDebug> Steps { get; set; } = [];
}
