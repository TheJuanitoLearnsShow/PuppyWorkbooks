using System.Text.Json;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class JsonOutputProvider(string path) : IOutputProvider
{
    private readonly List<Dictionary<string, object?>> _records = new();

    public ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _records.Add(new Dictionary<string, object?>(record.Values, StringComparer.OrdinalIgnoreCase));
        return ValueTask.FromResult(OutputStatus.Success($"Record written to {path}"));
    }

    public async ValueTask DisposeAsync()
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, _records, new JsonSerializerOptions { WriteIndented = true });
    }
}
