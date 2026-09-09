namespace PuppyWorkbooks.Integration.Models;

public sealed record IntegrationResult(long Read, long Written, long Excluded, IntegrationRecord? FinalState)
{
    public List<IntegrationDebugRow>? DebugData { get; init; }
}