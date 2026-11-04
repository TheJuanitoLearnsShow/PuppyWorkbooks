using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Providers;

public sealed class ServiceBusInputProvider : IInputProvider
{
    private static readonly TimeSpan ReceiveWaitTime = TimeSpan.FromSeconds(1);
    private readonly ServiceBusClient _client;
    private readonly ServiceBusReceiver _receiver;

    public ServiceBusInputProvider(string connectionString, string topicName, string subscriptionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);

        _client = new ServiceBusClient(connectionString);
        _receiver = _client.CreateReceiver(topicName, subscriptionName);
    }

    public ServiceBusInputProvider(ServiceBusInputProviderOptions options)
        : this(options.ConnectionString, options.TopicName, options.SubscriptionName)
    {
    }

    public async IAsyncEnumerable<IntegrationRecord> ReadAsync(
        IntegrationRecord? input = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var messages = await _receiver.ReceiveMessagesAsync(1, ReceiveWaitTime, cancellationToken);
            if (messages.Count == 0)
                yield break;

            var message = messages[0];
            yield return ParseMessage(message);
            await _receiver.CompleteMessageAsync(message, cancellationToken);
        }
    }

    private static IntegrationRecord ParseMessage(ServiceBusReceivedMessage message)
    {
        using var document = JsonDocument.Parse(message.Body.ToMemory());
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Azure Service Bus input messages must have a JSON object body.");

        var values = document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => ToValue(property.Value),
                StringComparer.OrdinalIgnoreCase);
        return new IntegrationRecord(values);
    }

    private static object? ToValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        _ => value.Clone()
    };

    public async ValueTask DisposeAsync()
    {
        await _receiver.DisposeAsync();
        await _client.DisposeAsync();
    }
}