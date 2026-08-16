using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlInputProvider(DbConnection connection, string query) : IInputProvider
{
    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandText = query;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) values[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            yield return new IntegrationRecord(values);
        }
    }
    public ValueTask DisposeAsync() => connection.DisposeAsync();
}