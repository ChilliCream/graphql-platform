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
    /// Gets the subscriptions targeting this queue.
    /// </summary>
    public IReadOnlyList<AzureServiceBusSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(AzureServiceBusQueueConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoDelete = configuration.AutoDelete;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(AzureServiceBusQueueConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/q/" + Name;
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

        var options = new CreateQueueOptions(Name);

        if (AutoDelete == true)
        {
            options.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
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
