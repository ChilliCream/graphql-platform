using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Represents a topic in the Azure Service Bus messaging topology. Topics are the publishing
/// destinations - messages published to a topic are distributed to all subscribed queues.
/// </summary>
public sealed class AzureServiceBusTopic
    : TopologyResource<AzureServiceBusTopicConfiguration>
    , IAzureServiceBusResource
{
    private ImmutableArray<AzureServiceBusSubscription> _subscriptions = [];

    /// <summary>
    /// Gets the topic name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets whether this topic should be auto-provisioned on the broker.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the subscriptions originating from this topic.
    /// </summary>
    public IReadOnlyList<AzureServiceBusSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(AzureServiceBusTopicConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(AzureServiceBusTopicConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/t/" + Name;
        Address = builder.Uri;
    }

    internal void AddSubscription(AzureServiceBusSubscription subscription)
    {
        ImmutableInterlocked.Update(ref _subscriptions, static (s, sub) => s.Add(sub), subscription);
    }

    /// <inheritdoc />
    public async Task ProvisionAsync(
        AzureServiceBusClientManager clientManager,
        CancellationToken cancellationToken)
    {
        if (AutoProvision == false)
        {
            return;
        }

        var adminClient = clientManager.AdminClient;
        if (adminClient is null)
        {
            return;
        }

        try
        {
            await adminClient.CreateTopicAsync(Name, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            // Already provisioned by another instance — safe to ignore.
        }
        catch (Exception) when (AutoProvision is null or true)
        {
            // Best-effort provisioning — the entity may already exist or the admin API
            // may be unavailable (e.g. emulator with Docker port mapping).
        }
    }
}
