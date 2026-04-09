using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Provisions Event Hub entities and consumer groups via Azure Resource Manager.
/// This is used during startup when auto-provisioning is enabled.
/// </summary>
internal sealed class EventHubProvisioner
{
    private readonly EventHubsNamespaceResource _namespaceResource;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new provisioner for the specified Event Hubs namespace.
    /// </summary>
    /// <param name="namespaceResource">The ARM namespace resource to provision entities in.</param>
    /// <param name="logger">The logger for provisioning operations.</param>
    public EventHubProvisioner(EventHubsNamespaceResource namespaceResource, ILogger logger)
    {
        _namespaceResource = namespaceResource;
        _logger = logger;
    }

    /// <summary>
    /// Ensures an Event Hub entity exists with the specified configuration.
    /// If the hub already exists, the operation is a no-op.
    /// </summary>
    /// <param name="eventHubName">The name of the Event Hub entity to create.</param>
    /// <param name="partitionCount">
    /// The number of partitions, or <c>null</c> to use the namespace default.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask ProvisionTopicAsync(
        string eventHubName,
        int? partitionCount,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Provisioning Event Hub '{EventHubName}'.", eventHubName);

        var collection = _namespaceResource.GetEventHubs();

        // CreateOrUpdate is idempotent — if the hub already exists, this is a no-op.
        // When partitionCount is null or 0, we leave PartitionCount unset so the ARM API
        // uses the namespace default.
        var data = new EventHubData();

        if (partitionCount is > 0)
        {
            data.PartitionCount = partitionCount.Value;
        }

        await collection.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            eventHubName,
            data,
            cancellationToken);

        _logger.LogInformation("Event Hub '{EventHubName}' provisioned.", eventHubName);
    }

    /// <summary>
    /// Ensures a consumer group exists on the specified Event Hub.
    /// If the consumer group already exists, the operation is a no-op.
    /// The default <c>$Default</c> consumer group is skipped since it always exists.
    /// </summary>
    /// <param name="eventHubName">The name of the Event Hub entity.</param>
    /// <param name="consumerGroupName">The consumer group name to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask ProvisionSubscriptionAsync(
        string eventHubName,
        string consumerGroupName,
        CancellationToken cancellationToken)
    {
        // $Default consumer group always exists — skip provisioning.
        if (string.Equals(consumerGroupName, "$Default", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _logger.LogInformation(
            "Provisioning consumer group '{ConsumerGroup}' on Event Hub '{EventHubName}'.",
            consumerGroupName,
            eventHubName);

        var eventHubResponse = await _namespaceResource
            .GetEventHubAsync(eventHubName, cancellationToken);

        var consumerGroups = eventHubResponse.Value.GetEventHubsConsumerGroups();

        // CreateOrUpdate is idempotent.
        await consumerGroups.CreateOrUpdateAsync(
            Azure.WaitUntil.Completed,
            consumerGroupName,
            new EventHubsConsumerGroupData(),
            cancellationToken);

        _logger.LogInformation(
            "Consumer group '{ConsumerGroup}' on Event Hub '{EventHubName}' provisioned.",
            consumerGroupName,
            eventHubName);
    }

    /// <summary>
    /// Creates an <see cref="EventHubProvisioner"/> from transport configuration using the
    /// connection provider's credential for ARM authentication.
    /// </summary>
    /// <param name="configuration">The transport configuration containing ARM resource coordinates.</param>
    /// <param name="connectionProvider">The connection provider supplying the token credential.</param>
    /// <param name="logger">The logger for provisioning operations.</param>
    /// <returns>A configured provisioner instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when ARM resource coordinates are missing or the connection provider does not supply a credential.
    /// </exception>
    public static EventHubProvisioner Create(
        EventHubTransportConfiguration configuration,
        IEventHubConnectionProvider connectionProvider,
        ILogger logger)
    {
        if (configuration.SubscriptionId is null
            || configuration.ResourceGroupName is null
            || configuration.NamespaceName is null)
        {
            throw new InvalidOperationException(
                "Auto-provisioning requires SubscriptionId, ResourceGroupName, and NamespaceName. "
                + "Use .ResourceGroup(subscriptionId, resourceGroupName, namespaceName) to configure.");
        }

        var credential = connectionProvider.Credential
            ?? throw new InvalidOperationException(
                "Auto-provisioning requires a TokenCredential. "
                + "Connection string authentication does not support ARM operations. "
                + "Use .Namespace(fullyQualifiedNamespace) with Azure Identity instead.");

        var armClient = new ArmClient(credential);

        var namespaceResourceId = EventHubsNamespaceResource.CreateResourceIdentifier(
            configuration.SubscriptionId,
            configuration.ResourceGroupName,
            configuration.NamespaceName);

        var namespaceResource = armClient.GetEventHubsNamespaceResource(namespaceResourceId);

        return new EventHubProvisioner(namespaceResource, logger);
    }
}
