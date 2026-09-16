using System.Runtime.CompilerServices;
using System.Text.Json;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class JsonInputProvider : IInputProvider
{
    private readonly Func<Stream> _streamFactory;
    private readonly string _jsonPath;

    public JsonInputProvider(string jsonText, string? jsonPath = null)
    {
        _streamFactory = () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonText));
        _jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? "$" : jsonPath;
    }

    public JsonInputProvider(Func<Stream> streamFactory, string? jsonPath = null)
    {
        _streamFactory = streamFactory;
        _jsonPath = string.IsNullOrWhiteSpace(jsonPath) ? "$" : jsonPath;
    }

    public static JsonInputProvider FromFile(string filePath, string? jsonPath = null)
    {
        return new JsonInputProvider(() => File.OpenRead(filePath), jsonPath);
    }

    public static JsonInputProvider FromText(string jsonText, string? jsonPath = null)
    {
        return new JsonInputProvider(jsonText, jsonPath);
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = _streamFactory();
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        foreach (var element in JsonPath.Select(document.RootElement, _jsonPath))
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"JSON path '{_jsonPath}' must select objects.");
            var values = element.EnumerateObject().ToDictionary(property => property.Name, property => ToValue(property.Value), StringComparer.OrdinalIgnoreCase);
            yield return new IntegrationRecord(values);
        }
    }

    private static object? ToValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        _ => value.Clone()
    };

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
