using System.Globalization;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class CsvInputProvider(string path) : IInputProvider
{
    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        await csv.ReadAsync(); csv.ReadHeader();
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