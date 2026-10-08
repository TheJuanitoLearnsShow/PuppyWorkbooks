using System.Text.Json;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class MemoryInputProvider : IInputProvider
{
    private readonly string _jsonData;

    public MemoryInputProvider(string jsonData)
    {
        _jsonData = jsonData;
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        List<Dictionary<string, object?>>? listData = null;
        try
        {
            listData = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(_jsonData, options);
        }
        catch (JsonException) { }

        if (listData is not null)
        {
            foreach (var item in listData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new IntegrationRecord(item);
            }
            yield break;
        }

        Dictionary<string, object?>? singleData = null;
        try
        {
            singleData = JsonSerializer.Deserialize<Dictionary<string, object?>>(_jsonData, options);
        }
        catch (JsonException) { }

        if (singleData is not null)
        {
            yield return new IntegrationRecord(singleData);
        }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
