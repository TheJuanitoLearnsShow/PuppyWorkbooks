using System.Data;
using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlInputProvider : IInputProvider
{
    private readonly DbConnection _connection;
    private readonly string? _query;
    private readonly SqlCommandType _commandType;
    private readonly IReadOnlyList<SqlParameterOption> _parameters;

    public SqlInputProvider(DbConnection connection, string? query = null,
        SqlCommandType commandType = SqlCommandType.Text, IReadOnlyList<SqlParameterOption>? parameters = null)
    {
        _connection = connection;
        _query = query;
        _commandType = commandType;
        _parameters = parameters ?? [];
    }

    public SqlInputProvider(DbConnection connection, SqlInputProviderOptions options)
        : this(connection, options.Query, options.CommandType, options.SqlParams)
    {
    }
    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(IntegrationRecord? input = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_commandType == SqlCommandType.StoredProcedure && string.IsNullOrWhiteSpace(_query))
            throw new InvalidOperationException("SQL stored procedure input requires a procedure name in Query.");

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
        await using var command = _connection.CreateCommand();
        command.CommandText = _query;
        command.CommandType = _commandType == SqlCommandType.StoredProcedure
            ? CommandType.StoredProcedure
            : CommandType.Text;
        SqlParameterBinder.AddParameters(command, _parameters, input);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++) values[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            yield return new IntegrationRecord(values);
        }
    }
    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    public string ConnectionString => _connection.ConnectionString;
}