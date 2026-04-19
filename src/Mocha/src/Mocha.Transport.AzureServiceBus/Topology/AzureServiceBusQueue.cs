using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Represents a queue in the Azure Service Bus messaging topology. Queues are the delivery
/// destinations - messages are consumed by receive endpoints from these queues.
/// </summary>
public sealed class AzureServiceBusQueue
    : TopologyResource<AzureServiceBusQueueConfiguration>
    , IAzureServiceBusResource
{
    private ImmutableArray<AzureServiceBusSubscription> _subscriptions = [];

    /// <summary>
    /// Gets the queue name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets whether this queue should be automatically deleted when no longer in use.
    /// </summary>
    public bool? AutoDelete { get; private set; }

    /// <summary>
    /// Gets whether this queue should be auto-provisioned on the broker.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the idle window after which the broker may delete the queue.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; private set; }

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
    /// Gets the maximum queue size in megabytes.
    /// </summary>
    public long? MaxSizeInMegabytes { get; private set; }

    /// <summary>
    /// Gets whether the queue requires sessions.
    /// </summary>
    public bool? RequiresSession { get; private set; }

    /// <summary>
    /// Gets whether the queue is partitioned.
    /// </summary>
    public bool? EnablePartitioning { get; private set; }

    /// <summary>
    /// Gets the entity to which messages received on this queue are auto-forwarded.
    /// </summary>
    public string? ForwardTo { get; private set; }

    /// <summary>
    /// Gets the entity to which dead-lettered messages from this queue are auto-forwarded.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; private set; }

    /// <summary>
    /// Gets whether expired messages are moved to the dead-letter queue instead of being dropped.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; private set; }

    /// <summary>
    /// Gets the subscriptions targeting this queue.
    /// </summary>
    public IReadOnlyList<AzureServiceBusSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(AzureServiceBusQueueConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoDelete = configuration.AutoDelete;
        AutoProvision = configuration.AutoProvision;
        AutoDeleteOnIdle = configuration.AutoDeleteOnIdle;
        LockDuration = configuration.LockDuration;
        MaxDeliveryCount = configuration.MaxDeliveryCount;
        DefaultMessageTimeToLive = configuration.DefaultMessageTimeToLive;
        MaxSizeInMegabytes = configuration.MaxSizeInMegabytes;
        RequiresSession = configuration.RequiresSession;
        EnablePartitioning = configuration.EnablePartitioning;
        ForwardTo = configuration.ForwardTo;
        ForwardDeadLetteredMessagesTo = configuration.ForwardDeadLetteredMessagesTo;
        DeadLetteringOnMessageExpiration = configuration.DeadLetteringOnMessageExpiration;
    }

    protected override void OnComplete(AzureServiceBusQueueConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address)
        {
            Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/q/" + Name
        };
        Address = builder.Uri;
    }

    internal void AddSubscription(AzureServiceBusSubscription subscription)
    {
        ImmutableInterlocked.Update(ref _subscriptions, static (s, sub) => s.Add(sub), subscription);
    }

    internal void SetForwardDeadLetteredMessagesTo(string entityName)
    {
        ForwardDeadLetteredMessagesTo = entityName;
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

        var options = new CreateQueueOptions(Name);

        // Only assign properties the user explicitly set so SDK defaults remain in effect otherwise.
        if (AutoDeleteOnIdle is not null)
        {
            options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
        }

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

        if (MaxSizeInMegabytes is not null)
        {
            options.MaxSizeInMegabytes = MaxSizeInMegabytes.Value;
        }

        if (RequiresSession is not null)
        {
            options.RequiresSession = RequiresSession.Value;
        }

        if (EnablePartitioning is not null)
        {
            options.EnablePartitioning = EnablePartitioning.Value;
        }

        if (ForwardTo is not null)
        {
            options.ForwardTo = ForwardTo;
        }

        if (ForwardDeadLetteredMessagesTo is not null)
        {
            options.ForwardDeadLetteredMessagesTo = ForwardDeadLetteredMessagesTo;
        }

        if (DeadLetteringOnMessageExpiration is not null)
        {
            options.DeadLetteringOnMessageExpiration = DeadLetteringOnMessageExpiration.Value;
        }

        try
        {
            await adminClient.CreateQueueAsync(options, cancellationToken);
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
