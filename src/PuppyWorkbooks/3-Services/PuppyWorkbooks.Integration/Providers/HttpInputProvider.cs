using System.Runtime.CompilerServices;
using System.Text.Json;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class HttpInputProvider : HttpProviderBase, IInputProvider
{
    private readonly string _endpoint;
    private readonly HttpMethod _method;
    private readonly string _jsonPath;

    public HttpInputProvider(HttpProviderSettings settings, string endpoint, string method, string jsonPath, IHttpClientFactory? httpClientFactory = null)
        : base(settings, httpClientFactory)
    {
        _endpoint = endpoint;
        _method = new HttpMethod(method);
        _jsonPath = jsonPath;
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(_method, _endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
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
}
