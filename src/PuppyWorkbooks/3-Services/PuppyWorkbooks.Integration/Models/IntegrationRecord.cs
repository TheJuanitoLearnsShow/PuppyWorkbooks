namespace PuppyWorkbooks.Integration.Models;

public sealed record IntegrationRecord(Dictionary<string, object?> Values)
{
    public object? this[string key] { get => Values.TryGetValue(key, out var value) ? value : null; set => Values[key] = value; }
}