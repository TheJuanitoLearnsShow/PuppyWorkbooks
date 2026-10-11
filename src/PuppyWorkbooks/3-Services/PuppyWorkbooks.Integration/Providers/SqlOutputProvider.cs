using System.Data;
using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlOutputProvider : IOutputProvider
{
    private readonly DbConnection _connection;
    private readonly string _tableName;
    private readonly string? _query;
    private readonly SqlCommandType _commandType;
    private readonly IReadOnlyList<SqlParameterOption> _parameters;

    public SqlOutputProvider(DbConnection connection, string tableName, string? query = null,
        SqlCommandType commandType = SqlCommandType.Text, IReadOnlyList<SqlParameterOption>? parameters = null)
    {
        _connection = connection;
        _tableName = tableName;
        _query = query;
        _commandType = commandType;
        _parameters = parameters ?? [];
    }

    public SqlOutputProvider(DbConnection connection, SqlOutputProviderOptions options)
        : this(connection, options.TableName, options.Query, options.CommandType, options.SqlParams)
    {
    }
    public async ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        if (_commandType == SqlCommandType.StoredProcedure && string.IsNullOrWhiteSpace(_query))
            throw new InvalidOperationException("SQL stored procedure output requires a procedure name in Query.");

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
        await using var command = _connection.CreateCommand();
        var names = record.Values.Keys.ToArray();
        command.CommandText = !string.IsNullOrWhiteSpace(_query)
            ? _query
            : $"INSERT INTO {Quote(_tableName)} ({string.Join(",", names.Select(Quote))}) VALUES ({string.Join(",", names.Select((_, i) => "@p" + i))})";
        command.CommandType = _commandType == SqlCommandType.StoredProcedure
            ? CommandType.StoredProcedure
            : CommandType.Text;

        if (_commandType == SqlCommandType.Text)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@p" + i;
                parameter.Value = record.Values[names[i]] ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        SqlParameterBinder.AddParameters(command, _parameters, record);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return OutputStatus.Success("Record written to SQL", affected);
    }
    private static string Quote(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}