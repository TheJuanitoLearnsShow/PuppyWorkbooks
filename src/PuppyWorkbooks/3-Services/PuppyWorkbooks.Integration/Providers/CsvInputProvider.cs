using System.Globalization;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class CsvInputProvider : IInputProvider
{
    private readonly Func<TextReader> _readerFactory;

    public CsvInputProvider(string path)
    {
        _readerFactory = () => new StreamReader(path);
    }

    public CsvInputProvider(Func<TextReader> readerFactory)
    {
        _readerFactory = readerFactory;
    }

    public static CsvInputProvider FromText(string csvText)
    {
        return new CsvInputProvider(() => new StringReader(csvText));
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = _readerFactory();
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        if (!await csv.ReadAsync()) yield break;
        csv.ReadHeader();
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in csv.HeaderRecord ?? []) values[header] = csv.GetField(header);
            yield return new IntegrationRecord(values);
        }
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}