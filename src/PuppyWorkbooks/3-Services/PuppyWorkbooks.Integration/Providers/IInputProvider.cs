using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public interface IInputProvider : IAsyncDisposable
{
    IAsyncEnumerable<IntegrationRecord> ReadAsync(IntegrationRecord? input = null, CancellationToken cancellationToken = default);
}