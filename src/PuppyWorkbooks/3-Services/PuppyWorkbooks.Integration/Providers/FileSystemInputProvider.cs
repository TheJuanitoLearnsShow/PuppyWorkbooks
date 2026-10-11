using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class FileSystemInputProvider : IInputProvider
{
    private readonly string _directoryPath;
    private readonly bool _addFileSize;
    private readonly bool _addCreatedOnDate;
    private readonly bool _addLastModifiedDate;
    private readonly string _mask;

    public FileSystemInputProvider(string directoryPath, bool addFileSize, bool addCreatedOnDate, bool addLastModifiedDate
    , string mask = "*")
    {
        _directoryPath = directoryPath;
        _addFileSize = addFileSize;
        _addCreatedOnDate = addCreatedOnDate;
        _addLastModifiedDate = addLastModifiedDate;
        _mask = mask;
    }

    public FileSystemInputProvider(FileSystemInputProviderOptions options)
        : this(options.FilePath, options.AddFileSizeField, options.AddCreatedOnDate, options.AddLastModifiedDateField)
    {
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(IntegrationRecord? input = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO, maybe log warning if folder not found?
        if (!Directory.Exists(_directoryPath)) yield break;

        var directoryInfo = new DirectoryInfo(_directoryPath);
        
        foreach (var file in directoryInfo.EnumerateFiles(_mask, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["FileName"] = file.Name,
                ["FileExtension"] = file.Extension,
                ["FilePath"] = file.FullName,
                ["FileDirectoryName"] = file.DirectoryName
            };
            
            if (_addFileSize) values["FileSizeInBytes"] = file.Length;
            if (_addCreatedOnDate) values["FileCreatedDate"] = file.CreationTime;
            if (_addLastModifiedDate) values["FileLastModifiedDate"] = file.LastWriteTime;

            yield return new IntegrationRecord(values);
        }
        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
