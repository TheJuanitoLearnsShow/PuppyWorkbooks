using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlInputProvider : IInputProvider
{
    private readonly DbConnection _connection;
    private readonly string? _query;

    public SqlInputProvider(DbConnection connection, string? query = null)
    {
        _connection = connection;
        _query = query;
        _connection.Open();
    }
    public async IAsyncEnumerable<IntegrationRecord> ReadAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var command = _connection.CreateCommand(); command.CommandText = _query;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) values[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            yield return new IntegrationRecord(values);
        }
    }
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}