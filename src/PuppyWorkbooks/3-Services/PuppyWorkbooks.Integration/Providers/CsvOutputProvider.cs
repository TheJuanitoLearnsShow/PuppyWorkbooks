using System.Globalization;
using CsvHelper;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

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