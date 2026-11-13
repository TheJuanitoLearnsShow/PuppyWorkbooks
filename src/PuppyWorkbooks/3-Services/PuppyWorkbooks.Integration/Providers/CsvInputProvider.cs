using System.Globalization;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Models.Input;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class CsvInputProvider : IInputProvider
{
    private readonly string _csvFilePath;
    private readonly char _csvSeparator;
    private readonly bool _csvHasHeaderRecord;
    private readonly Func<TextReader> _readerFactory;

    public CsvInputProvider(string path)
    {
        _readerFactory = () => new StreamReader(path);
    }

    public CsvInputProvider(CsvInputProviderOptions options)
        : this(options.FilePath)
    {
    }

    public CsvInputProvider(Func<TextReader> readerFactory)
    {
        _readerFactory = readerFactory;
    }

    public CsvInputProvider(string csvFilePath, char csvSeparator, bool csvHasHeaderRecord)
    {
        _csvSeparator = csvSeparator;
        _csvHasHeaderRecord = csvHasHeaderRecord;
        _readerFactory = () => new StreamReader(csvFilePath);
    }

    public static CsvInputProvider FromText(string csvText)
    {
        return new CsvInputProvider(() => new StringReader(csvText));
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(IntegrationRecord? input = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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