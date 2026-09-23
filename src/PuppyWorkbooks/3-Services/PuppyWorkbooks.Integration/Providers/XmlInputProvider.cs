using System.Runtime.CompilerServices;
using System.Xml.Linq;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class XmlInputProvider : IInputProvider
{
    private readonly Func<Stream> _streamFactory;
    private readonly string? _itemElement;

    public XmlInputProvider(string filePath, string? itemElement = null)
    {
        _streamFactory = () => File.OpenRead(filePath);
        _itemElement = string.IsNullOrWhiteSpace(itemElement) ? null : itemElement;
    }

    public XmlInputProvider(Func<Stream> streamFactory, string? itemElement = null)
    {
        _streamFactory = streamFactory;
        _itemElement = string.IsNullOrWhiteSpace(itemElement) ? null : itemElement;
    }

    public static XmlInputProvider FromText(string xmlText, string? itemElement = null)
    {
        return new XmlInputProvider(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlText)), itemElement);
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = _streamFactory();
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var root = document.Root;
        if (root is null) yield break;

        var items = _itemElement is not null
            ? root.Descendants(_itemElement)
            : root.Elements();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in item.Attributes())
                values[attribute.Name.LocalName] = attribute.Value;
            foreach (var child in item.Elements())
                values[child.Name.LocalName] = child.HasElements ? child.ToString() : child.Value;
            if (values.Count == 0 && !item.HasElements)
                values[item.Name.LocalName] = item.Value;
            yield return new IntegrationRecord(values);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
