using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class FileSystemInputProvider : IInputProvider
{
    private readonly string _directoryPath;
    private readonly bool _addFileSize;
    private readonly bool _addCreatedOnDate;
    private readonly bool _addLastModifiedDate;

    public FileSystemInputProvider(string directoryPath, bool addFileSize, bool addCreatedOnDate, bool addLastModifiedDate)
    {
        _directoryPath = directoryPath;
        _addFileSize = addFileSize;
        _addCreatedOnDate = addCreatedOnDate;
        _addLastModifiedDate = addLastModifiedDate;
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(IntegrationRecord? input = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directoryPath)) yield break;

        var directoryInfo = new DirectoryInfo(_directoryPath);
        foreach (var file in directoryInfo.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["File Name"] = file.Name,
                ["file extension"] = file.Extension,
                ["file path"] = file.FullName,
                ["file directory name"] = file.DirectoryName
            };
            
            if (_addFileSize) values["file size in bytes"] = file.Length;
            if (_addCreatedOnDate) values["file created date"] = file.CreationTime;
            if (_addLastModifiedDate) values["file last modified date"] = file.LastWriteTime;

            yield return new IntegrationRecord(values);
        }
        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
