using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class MockOutputProvider : IOutputProvider
{
    public ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(OutputStatus.Success("Record ignored (mock)", 0));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
