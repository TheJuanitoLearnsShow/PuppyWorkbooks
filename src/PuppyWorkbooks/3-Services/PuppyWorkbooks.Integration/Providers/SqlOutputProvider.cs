using System.Data;
using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlOutputProvider : IOutputProvider
{
    private readonly DbConnection _connection;
    private readonly string _tableName;
    private readonly string? _query;

    public SqlOutputProvider(DbConnection connection, string tableName, string? query = null)
    {
        _connection = connection;
        _tableName = tableName;
        _query = query;
    }
    public async ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
        await using var command = _connection.CreateCommand();
        var names = record.Values.Keys.ToArray(); 
        command.CommandText = !string.IsNullOrWhiteSpace(_query) ? _query : $"INSERT INTO {Quote(_tableName)} ({string.Join(",", names.Select(Quote))}) VALUES ({string.Join(",", names.Select((_, i) => "@p" + i))})";

        for (var i = 0; i < names.Length; i++)
        {
            var p = command.CreateParameter(); 
            p.ParameterName = "@p" + i; 
            p.Value = record.Values[names[i]] ?? DBNull.Value; 
            command.Parameters.Add(p);
        }
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return OutputStatus.Success("Record written to SQL", affected);
    }
    private static string Quote(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}