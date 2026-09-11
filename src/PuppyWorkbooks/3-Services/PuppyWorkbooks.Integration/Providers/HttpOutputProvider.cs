using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class HttpOutputProvider : HttpProviderBase, IOutputProvider
{
    private readonly string _endpoint;
    private readonly HttpMethod _method;
    private readonly HttpPayloadFormat _payloadFormat;

    public HttpOutputProvider(HttpProviderSettings settings, string endpoint, string method, HttpPayloadFormat payloadFormat, IHttpClientFactory? httpClientFactory = null)
        : base(settings, httpClientFactory)
    {
        _endpoint = endpoint;
        _method = new HttpMethod(method);
        _payloadFormat = payloadFormat;
    }

    public async ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(_method, _endpoint, CreateContent(record), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.IsSuccessStatusCode
            ? OutputStatus.Success($"Record sent to HTTP endpoint ({(int)response.StatusCode}).")
            : new OutputStatus(false, $"HTTP endpoint returned {(int)response.StatusCode}: {body}", 0);
    }

    private HttpContent CreateContent(IntegrationRecord record) => _payloadFormat switch
    {
        HttpPayloadFormat.Json => new StringContent(JsonSerializer.Serialize(record.Values), Encoding.UTF8, "application/json"),
        HttpPayloadFormat.Xml => new StringContent(new XElement("Record", record.Values.Select(value => new XElement(value.Key, value.Value))).ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "application/xml"),
        HttpPayloadFormat.Csv => new StringContent(CreateCsv(record), Encoding.UTF8, "text/csv"),
        _ => throw new InvalidOperationException($"Unsupported HTTP payload format '{_payloadFormat}'.")
    };

    private static string CreateCsv(IntegrationRecord record)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        foreach (var key in record.Values.Keys) csv.WriteField(key);
        csv.NextRecord();
        foreach (var value in record.Values.Values) csv.WriteField(value);
        csv.NextRecord();
        return writer.ToString();
    }
}
