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
    /// Gets the default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; private set; }

    /// <summary>
    /// Gets the maximum topic size in megabytes.
    /// </summary>
    public long? MaxSizeInMegabytes { get; private set; }

    /// <summary>
    /// Gets whether the topic is partitioned.
    /// </summary>
    public bool? EnablePartitioning { get; private set; }

    /// <summary>
    /// Gets whether the topic enforces duplicate detection.
    /// </summary>
    public bool? RequiresDuplicateDetection { get; private set; }

    /// <summary>
    /// Gets the time window over which duplicate detection is performed.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; private set; }

    /// <summary>
    /// Gets the idle window after which the broker may delete the topic.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; private set; }

    /// <summary>
    /// Gets whether the topic preserves ordering across partitioned subscriptions.
    /// </summary>
    public bool? SupportOrdering { get; private set; }

    /// <summary>
    /// Gets the subscriptions originating from this topic.
    /// </summary>
    public IReadOnlyList<AzureServiceBusSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(AzureServiceBusTopicConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoProvision = configuration.AutoProvision;
        DefaultMessageTimeToLive = configuration.DefaultMessageTimeToLive;
        MaxSizeInMegabytes = configuration.MaxSizeInMegabytes;
        EnablePartitioning = configuration.EnablePartitioning;
        RequiresDuplicateDetection = configuration.RequiresDuplicateDetection;
        DuplicateDetectionHistoryTimeWindow = configuration.DuplicateDetectionHistoryTimeWindow;
        AutoDeleteOnIdle = configuration.AutoDeleteOnIdle;
        SupportOrdering = configuration.SupportOrdering;
    }

    protected override void OnComplete(AzureServiceBusTopicConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address)
        {
            Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/t/" + Name
        };
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

        var options = new CreateTopicOptions(Name);

        // Only assign properties the user explicitly set so SDK defaults remain in effect otherwise.
        if (DefaultMessageTimeToLive is not null)
        {
            options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
        }

        if (MaxSizeInMegabytes is not null)
        {
            options.MaxSizeInMegabytes = MaxSizeInMegabytes.Value;
        }

        if (EnablePartitioning is not null)
        {
            options.EnablePartitioning = EnablePartitioning.Value;
        }

        if (RequiresDuplicateDetection is not null)
        {
            options.RequiresDuplicateDetection = RequiresDuplicateDetection.Value;
        }

        if (DuplicateDetectionHistoryTimeWindow is not null)
        {
            options.DuplicateDetectionHistoryTimeWindow = DuplicateDetectionHistoryTimeWindow.Value;
        }

        if (AutoDeleteOnIdle is not null)
        {
            options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
        }

        if (SupportOrdering is not null)
        {
            options.SupportOrdering = SupportOrdering.Value;
        }

        try
        {
            await clientManager.CreateTopicAsync(options, cancellationToken);
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
