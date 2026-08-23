using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Pairs a <see cref="ServiceBusClient"/> with its sibling
/// <see cref="ServiceBusAdministrationClient"/> so the rest of the transport only deals with a
/// single connection handle.
/// </summary>
internal sealed class ServiceBusConnection : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient? _administrationClient;
    private readonly bool _ownsClient;

    private ServiceBusConnection(
        ServiceBusClient client,
        ServiceBusAdministrationClient? administrationClient,
        bool ownsClient)
    {
        _client = client;
        _administrationClient = administrationClient;
        _ownsClient = ownsClient;
    }

    public string FullyQualifiedNamespace => _client.FullyQualifiedNamespace;

    public bool CanManageTopology => _administrationClient is not null;

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
        => AdministrationClient.CreateQueueAsync(options, cancellationToken);

    public Task CreateTopicAsync(CreateTopicOptions options, CancellationToken cancellationToken)
        => AdministrationClient.CreateTopicAsync(options, cancellationToken);

    public Task CreateSubscriptionAsync(CreateSubscriptionOptions options, CancellationToken cancellationToken)
        => AdministrationClient.CreateSubscriptionAsync(options, cancellationToken);

    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
        => AdministrationClient.DeleteQueueAsync(queueName, cancellationToken);

    public Task DeleteSubscriptionAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        => AdministrationClient.DeleteSubscriptionAsync(topicName, subscriptionName, cancellationToken);

    public ValueTask DisposeAsync() => _ownsClient ? _client.DisposeAsync() : ValueTask.CompletedTask;

    private ServiceBusAdministrationClient AdministrationClient
        => _administrationClient
            ?? throw new InvalidOperationException(
                "A ServiceBusAdministrationClient must be registered to manage Azure Service Bus topology.");

    public static string? ResolveAdministrationConnectionString(AzureServiceBusTransportConfiguration configuration)
    {
        if (configuration.ConnectionString is not null)
        {
            return configuration.AdministrationConnectionString ?? configuration.ConnectionString;
        }

        if (configuration.AdministrationConnectionString is not null)
        {
            throw new InvalidOperationException(
                "AdministrationConnectionString requires ConnectionString-based configuration.");
        }

        return null;
    }

    public static ServiceBusConnection Create(
        AzureServiceBusTransportConfiguration configuration,
        ServiceBusClientOptions clientOptions,
        IServiceProvider services)
    {
        if (configuration.ConnectionString is not null
            || configuration.FullyQualifiedNamespace is not null
            || configuration.Credential is not null)
        {
            return Create(configuration, clientOptions);
        }

        return new ServiceBusConnection(
            services.GetRequiredService<ServiceBusClient>(),
            configuration.AdministrationConnectionString is { } administrationConnectionString
                ? new ServiceBusAdministrationClient(administrationConnectionString)
                : services.GetService<ServiceBusAdministrationClient>(),
            ownsClient: false);
    }

    public static ServiceBusConnection Create(
        AzureServiceBusTransportConfiguration configuration,
        ServiceBusClientOptions clientOptions)
    {
        if (configuration.ConnectionString is not null
            && (configuration.FullyQualifiedNamespace is not null || configuration.Credential is not null))
        {
            throw ThrowHelper.ConnectionStringAndNamespaceCredentialMutuallyExclusive();
        }

        if (configuration.ConnectionString is not null)
        {
            return new ServiceBusConnection(
                new ServiceBusClient(configuration.ConnectionString, clientOptions),
                new ServiceBusAdministrationClient(ResolveAdministrationConnectionString(configuration)),
                ownsClient: true);
        }

        if (configuration.FullyQualifiedNamespace is not null || configuration.Credential is not null)
        {
            ResolveAdministrationConnectionString(configuration);

            if (configuration.FullyQualifiedNamespace is null || configuration.Credential is null)
            {
                throw ThrowHelper.ConnectionStringOrCredentialRequired();
            }

            return new ServiceBusConnection(
                new ServiceBusClient(configuration.FullyQualifiedNamespace, configuration.Credential, clientOptions),
                new ServiceBusAdministrationClient(
                    configuration.FullyQualifiedNamespace,
                    configuration.Credential),
                ownsClient: true);
        }

        ResolveAdministrationConnectionString(configuration);
        throw ThrowHelper.ConnectionStringOrCredentialRequired();
    }
}
