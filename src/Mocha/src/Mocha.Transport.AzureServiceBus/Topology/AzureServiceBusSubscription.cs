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

    /// <summary>
    /// Gets the lock duration applied by the broker when a message is delivered to a receiver.
    /// </summary>
    public TimeSpan? LockDuration { get; private set; }

    /// <summary>
    /// Gets the maximum delivery attempts before a message is dead-lettered.
    /// </summary>
    public int? MaxDeliveryCount { get; private set; }

    /// <summary>
    /// Gets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; private set; }

    /// <summary>
    /// Gets whether the subscription requires sessions.
    /// </summary>
    public bool? RequiresSession { get; private set; }

    /// <summary>
    /// Gets the entity to which messages received on this subscription are auto-forwarded.
    /// When null, the subscription forwards to its destination queue by convention.
    /// </summary>
    public string? ForwardTo { get; private set; }

    /// <summary>
    /// Gets the entity to which dead-lettered messages from this subscription are auto-forwarded.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; private set; }

    /// <summary>
    /// Gets whether expired messages are moved to the dead-letter queue instead of being dropped.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; private set; }

    /// <summary>
    /// Gets the idle window after which the broker may delete the subscription.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; private set; }

    protected override void OnInitialize(AzureServiceBusSubscriptionConfiguration configuration)
    {
        AutoProvision = configuration.AutoProvision;
        LockDuration = configuration.LockDuration;
        MaxDeliveryCount = configuration.MaxDeliveryCount;
        DefaultMessageTimeToLive = configuration.DefaultMessageTimeToLive;
        RequiresSession = configuration.RequiresSession;
        ForwardTo = configuration.ForwardTo;
        ForwardDeadLetteredMessagesTo = configuration.ForwardDeadLetteredMessagesTo;
        DeadLetteringOnMessageExpiration = configuration.DeadLetteringOnMessageExpiration;
        AutoDeleteOnIdle = configuration.AutoDeleteOnIdle;
    }

    protected override void OnComplete(AzureServiceBusSubscriptionConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address)
        {
            Path = Topology.Address.AbsolutePath.TrimEnd('/')
                + "/s/t/" + Source.Name
                + "/q/" + Destination.Name
        };
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
                ForwardTo = ForwardTo ?? Destination.Name
            };

            // Only assign properties the user explicitly set so SDK defaults remain in effect otherwise.
            if (LockDuration is not null)
            {
                options.LockDuration = LockDuration.Value;
            }

            if (MaxDeliveryCount is not null)
            {
                options.MaxDeliveryCount = MaxDeliveryCount.Value;
            }

            if (DefaultMessageTimeToLive is not null)
            {
                options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
            }

            if (RequiresSession is not null)
            {
                options.RequiresSession = RequiresSession.Value;
            }

            if (ForwardDeadLetteredMessagesTo is not null)
            {
                options.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;
            }

            if (DeadLetteringOnMessageExpiration is not null)
            {
                options.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
            }

            if (AutoDeleteOnIdle is not null)
            {
                options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
            }

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
