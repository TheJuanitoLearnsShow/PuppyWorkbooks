using System.Globalization;
using System.Xml.Linq;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class XmlOutputProvider : IOutputProvider
{
    private readonly string _path;
    private readonly XName _rootName;
    private readonly XName _recordName;
    private readonly List<XElement> _records = new();

    public XmlOutputProvider(string path, string? rootElement = null, string? recordElement = null)
    {
        _path = path;
        _rootName = string.IsNullOrWhiteSpace(rootElement) ? "Records" : rootElement;
        _recordName = string.IsNullOrWhiteSpace(recordElement) ? "Record" : recordElement;
    }

    public ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var element = new XElement(_recordName);
        foreach (var pair in record.Values)
            element.Add(new XElement(pair.Key, FormatValue(pair.Value)));
        _records.Add(element);
        return ValueTask.FromResult(OutputStatus.Success($"Record written to {_path}"));
    }

    public async ValueTask DisposeAsync()
    {
        var document = new XDocument(new XElement(_rootName, _records));
        await using var stream = File.Create(_path);
        await document.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}
