using PuppyWorkbooks.Integration.Models;
using PuppyWorkbooks.Integration.Models.Output;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class MemoryOutputProvider : IOutputProvider
{
    private readonly Action<IntegrationRecord>? _action;

    public MemoryOutputProvider(Action<IntegrationRecord>? action = null)
    {
        _action = action;
    }

    public MemoryOutputProvider(MemoryOutputProviderOptions options)
        : this(options.Action)
    {
    }

    public ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _action?.Invoke(record);
        return ValueTask.FromResult(OutputStatus.Success("Record processed by memory output", 1));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
