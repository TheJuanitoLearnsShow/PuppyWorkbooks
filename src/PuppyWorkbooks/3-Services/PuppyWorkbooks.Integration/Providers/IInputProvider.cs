using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public interface IInputProvider : IAsyncDisposable
{
    IAsyncEnumerable<IntegrationRecord> ReadAsync(CancellationToken cancellationToken = default);
}