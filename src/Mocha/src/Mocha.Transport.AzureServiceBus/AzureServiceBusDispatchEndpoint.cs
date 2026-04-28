using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// A dispatch endpoint that sends messages to an Azure Service Bus queue or publishes them
/// through an Azure Service Bus topic using the client manager.
/// </summary>
/// <remarks>
/// During completion the endpoint resolves its target resource from the topology. For reply
/// endpoints the destination is determined dynamically from the envelope's destination address
/// at dispatch time. Scheduled dispatches do not flow through this endpoint — the scheduling
/// middleware short-circuits the pipeline before the endpoint is invoked and routes them to
/// <see cref="Scheduling.AzureServiceBusScheduledMessageStore"/>.
/// </remarks>
public sealed class AzureServiceBusDispatchEndpoint(AzureServiceBusMessagingTransport transport)
    : DispatchEndpoint<AzureServiceBusDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue, or <c>null</c> if this endpoint dispatches to a topic.
    /// </summary>
    public AzureServiceBusQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target topic, or <c>null</c> if this endpoint dispatches to a queue.
    /// </summary>
    public AzureServiceBusTopic? Topic { get; private set; }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null && configuration.QueueName is null)
        {
            throw new InvalidOperationException("Topic name or queue name is required");
        }
    }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var clientManager = transport.ClientManager;
        var cancellationToken = context.CancellationToken;

        await EnsureProvisionedAsync(cancellationToken);

        var entityPath = AzureServiceBusEntityPathResolver.Resolve(this, envelope);
        var message = AzureServiceBusMessageFactory.Create(envelope);

        await AzureServiceBusEntityNotFoundRetry.ExecuteAsync(
            clientManager,
            this,
            entityPath,
            (sender, ct) => sender.SendMessageAsync(message, ct),
            cancellationToken);
    }

    private readonly SemaphoreSlim _provisionGate = new(1, 1);
    private volatile bool _provisioned;

    internal async Task EnsureProvisionedAsync(CancellationToken cancellationToken)
    {
        if (_provisioned)
        {
            return;
        }

        await _provisionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_provisioned)
            {
                return;
            }

            var autoProvision = ((AzureServiceBusMessagingTopology)transport.Topology).AutoProvision;

            // Provisioning runs detached from the caller's token so a cancelled caller does
            // not tear down provisioning for other dispatchers waiting on the gate.
            if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
            {
                await Queue.ProvisionAsync(transport.ClientManager, CancellationToken.None).ConfigureAwait(false);
            }

            if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
            {
                await Topic.ProvisionAsync(transport.ClientManager, CancellationToken.None).ConfigureAwait(false);
            }

            _provisioned = true;
        }
        finally
        {
            _provisionGate.Release();
        }
    }

    internal void InvalidateProvisioning() => _provisioned = false;

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpointConfiguration configuration)
    {
        var topology = (AzureServiceBusMessagingTopology)Transport.Topology;

        if (configuration.TopicName is not null)
        {
            Topic =
                topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName)
                ?? throw new InvalidOperationException("Topic not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Topic as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
