using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration;

public interface IInputProvider : IAsyncDisposable
{
    IAsyncEnumerable<IntegrationRecord> ReadAsync(CancellationToken cancellationToken = default);
}

public interface IOutputProvider : IAsyncDisposable
{
    ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default);
}

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

public sealed class CsvOutputProvider(string path) : IOutputProvider
{
    private StreamWriter? _writer; private CsvWriter? _csv; private bool _headerWritten;
    public async ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        _writer ??= new StreamWriter(path); _csv ??= new CsvWriter(_writer, CultureInfo.InvariantCulture);
        if (!_headerWritten) { foreach (var key in record.Values.Keys) _csv.WriteField(key); await _csv.NextRecordAsync(); _headerWritten = true; }
        foreach (var value in record.Values.Values) _csv.WriteField(value); await _csv.NextRecordAsync(); await _writer.FlushAsync(cancellationToken);
        return OutputStatus.Success($"Record written to {path}");
    }
    public async ValueTask DisposeAsync() { if (_csv is not null) _csv.Dispose(); if (_writer is not null) await _writer.DisposeAsync(); }
}

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
