using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public interface IOutputProvider : IAsyncDisposable
{
    ValueTask<OutputStatus> WriteAsync(IntegrationRecord record, CancellationToken cancellationToken = default);
}