using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Represents a subscription linking a topic to a queue in the Azure Service Bus messaging topology.
/// When a message is published to the topic, it is forwarded to the destination queue via this subscription.
/// </summary>
public sealed class AzureServiceBusSubscription
    : TopologyResource<AzureServiceBusSubscriptionConfiguration>
    , IAzureServiceBusResource
{
    /// <summary>
    /// Gets the source topic for this subscription.
    /// </summary>
    public AzureServiceBusTopic Source { get; private set; } = null!;

    /// <summary>
    /// Gets the destination queue for this subscription.
    /// </summary>
    public AzureServiceBusQueue Destination { get; private set; } = null!;

    /// <summary>
    /// Gets whether this subscription should be auto-provisioned on the broker.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    protected override void OnInitialize(AzureServiceBusSubscriptionConfiguration configuration)
    {
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(AzureServiceBusSubscriptionConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/')
            + "/s/t/" + Source.Name
            + "/q/" + Destination.Name;
        Address = builder.Uri;
    }

    internal void SetSource(AzureServiceBusTopic source)
    {
        Source = source;
    }

    internal void SetDestination(AzureServiceBusQueue destination)
    {
        Destination = destination;
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
            // Azure SB SDK rejects ForwardTo == subscription name ("cannot forward to itself"),
            // even though subscriptions and queues are distinct entities. Use a prefixed name
            // to avoid the validation while still forwarding to the destination queue.
            var subscriptionName = "fwd-" + Destination.Name;
            if (subscriptionName.Length > 50)
            {
                subscriptionName = subscriptionName[..50];
            }

            var options = new CreateSubscriptionOptions(Source.Name, subscriptionName)
            {
                ForwardTo = Destination.Name
            };

            await adminClient.CreateSubscriptionAsync(options, cancellationToken);
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
