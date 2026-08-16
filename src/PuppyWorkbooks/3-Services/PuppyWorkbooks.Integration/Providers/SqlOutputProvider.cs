using System.Data.Common;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class SqlOutputProvider(DbConnection connection, string tableName, string? query = null) : IOutputProvider
{
    public async ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        if (!string.IsNullOrWhiteSpace(query)) command.CommandText = query;
        else
        {
            var names = record.Values.Keys.ToArray(); command.CommandText = $"INSERT INTO {Quote(tableName)} ({string.Join(",", names.Select(Quote))}) VALUES ({string.Join(",", names.Select((_, i) => "@p" + i))})";
            for (var i = 0; i < names.Length; i++) { var p = command.CreateParameter(); p.ParameterName = "@p" + i; p.Value = record.Values[names[i]] ?? DBNull.Value; command.Parameters.Add(p); }
        }
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return OutputStatus.Success("Record written to SQL", affected);
    }
    private static string Quote(string value) => "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    public ValueTask DisposeAsync() => connection.DisposeAsync();
}