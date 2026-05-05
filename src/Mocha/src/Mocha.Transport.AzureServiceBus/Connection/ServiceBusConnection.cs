using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Pairs a <see cref="ServiceBusClient"/> with its sibling
/// <see cref="ServiceBusAdministrationClient"/> so the rest of the transport only deals with a
/// single connection handle.
/// </summary>
internal sealed class ServiceBusConnection : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;

    private ServiceBusConnection(ServiceBusClient client, ServiceBusAdministrationClient adminClient)
    {
        _client = client;
        _adminClient = adminClient;
    }

    public ServiceBusSender CreateSender(string entityPath) => _client.CreateSender(entityPath);

    public ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
        => _client.CreateProcessor(queueName, options);

    public ServiceBusProcessor CreateProcessor(
        string topicName,
        string subscriptionName,
        ServiceBusProcessorOptions options)
        => _client.CreateProcessor(topicName, subscriptionName, options);

    public ServiceBusReceiver CreateReceiver(string queueName) => _client.CreateReceiver(queueName);

    public ServiceBusSessionProcessor CreateSessionProcessor(
        string queueName,
        ServiceBusSessionProcessorOptions options)
        => _client.CreateSessionProcessor(queueName, options);

    public Task CreateQueueAsync(CreateQueueOptions options, CancellationToken cancellationToken)
        => _adminClient.CreateQueueAsync(options, cancellationToken);

    public Task CreateTopicAsync(CreateTopicOptions options, CancellationToken cancellationToken)
        => _adminClient.CreateTopicAsync(options, cancellationToken);

    public Task CreateSubscriptionAsync(CreateSubscriptionOptions options, CancellationToken cancellationToken)
        => _adminClient.CreateSubscriptionAsync(options, cancellationToken);

    public ValueTask DisposeAsync() => _client.DisposeAsync();

    public static ServiceBusConnection Create(
        AzureServiceBusTransportConfiguration configuration,
        ServiceBusClientOptions clientOptions)
    {
        if (configuration.ConnectionString is not null)
        {
            return new ServiceBusConnection(
                new ServiceBusClient(configuration.ConnectionString, clientOptions),
                new ServiceBusAdministrationClient(configuration.ConnectionString));
        }

        if (configuration.FullyQualifiedNamespace is not null
            && configuration.Credential is not null)
        {
            return new ServiceBusConnection(
                new ServiceBusClient(configuration.FullyQualifiedNamespace, configuration.Credential, clientOptions),
                new ServiceBusAdministrationClient(configuration.FullyQualifiedNamespace, configuration.Credential));
        }

        throw new InvalidOperationException(
            "Either ConnectionString or FullyQualifiedNamespace + Credential must be provided");
    }
}
