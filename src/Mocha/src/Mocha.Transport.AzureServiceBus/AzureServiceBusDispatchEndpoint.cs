using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// A dispatch endpoint that sends messages to an Azure Service Bus queue or publishes them
/// through an Azure Service Bus topic using the client manager.
/// </summary>
public sealed class AzureServiceBusDispatchEndpoint(AzureServiceBusMessagingTransport transport)
    : DispatchEndpoint<AzureServiceBusDispatchEndpointConfiguration>(transport)
{
    private readonly SemaphoreSlim _provisionGate = new(1, 1);
    private volatile bool _provisioned;

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
            throw ThrowHelper.DispatchEndpointTopicOrQueueNameRequired();
        }
    }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw ThrowHelper.DispatchEndpointEnvelopeNotSet();
        }

        await EnsureProvisionedAsync(context.CancellationToken);

        var now = context.Services.GetTimeProvider().GetUtcNow();

        await EnqueueOrScheduleMessage(
            envelope,
            expectedEnqueueTime: now,
            scheduledTime: null,
            cancellationToken: context.CancellationToken);
    }

    internal async ValueTask<ScheduledDispatch> ScheduleEnvelopeAsync(
        MessageEnvelope envelope,
        DateTimeOffset scheduledTime,
        DateTimeOffset expectedEnqueueTime,
        CancellationToken cancellationToken)
    {
        (var entityPath, var sequenceNumber) = await EnqueueOrScheduleMessage(
            envelope,
            expectedEnqueueTime,
            scheduledTime,
            cancellationToken);

        return new ScheduledDispatch(entityPath, sequenceNumber);
    }

    private async ValueTask<(string, long)> EnqueueOrScheduleMessage(
        MessageEnvelope envelope,
        DateTimeOffset expectedEnqueueTime,
        DateTimeOffset? scheduledTime,
        CancellationToken cancellationToken)
    {
        await EnsureProvisionedAsync(cancellationToken);

        var entityPath = ResolveEntityPath(envelope);
        var message = AzureServiceBusMessageFactory.Create(envelope, expectedEnqueueTime);
        var senderLease = transport.ClientManager.AcquireSender(entityPath);
        var senderEntry = senderLease.Entry;

        try
        {
            if (scheduledTime is null)
            {
                await senderLease.Sender.SendMessageAsync(message, cancellationToken);
                return (entityPath, 0);
            }

            return (
                entityPath,
                await senderLease.Sender.ScheduleMessageAsync(message, scheduledTime.Value, cancellationToken));
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            // Retire the stale sender, reprovision the entity, and retry once.
        }
        finally
        {
            senderLease.Dispose();
        }

        InvalidateProvisioning();
        await transport.ClientManager.InvalidateSenderAsync(entityPath, senderEntry);
        await EnsureProvisionedAsync(cancellationToken);

        using var retryLease = transport.ClientManager.AcquireSender(entityPath);

        if (scheduledTime is null)
        {
            await retryLease.Sender.SendMessageAsync(message, cancellationToken);
            return (entityPath, 0);
        }

        return (
            entityPath,
            await retryLease.Sender.ScheduleMessageAsync(message, scheduledTime.Value, cancellationToken));
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpointConfiguration configuration)
    {
        var topology = (AzureServiceBusMessagingTopology)Transport.Topology;

        if (configuration.TopicName is not null)
        {
            Topic =
                topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName)
                ?? throw ThrowHelper.DispatchEndpointTopicNotFound();
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw ThrowHelper.DispatchEndpointQueueNotFound();
        }

        Destination =
            Topic as TopologyResource
            ?? Queue as TopologyResource
            ?? throw ThrowHelper.DispatchEndpointDestinationNotSet();
    }

    private async Task EnsureProvisionedAsync(CancellationToken cancellationToken)
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

            if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
            {
                await Queue.ProvisionAsync(transport.ClientManager, cancellationToken).ConfigureAwait(false);
            }

            if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
            {
                await Topic.ProvisionAsync(transport.ClientManager, cancellationToken).ConfigureAwait(false);
            }

            _provisioned = true;
        }
        finally
        {
            _provisionGate.Release();
        }
    }

    private void InvalidateProvisioning() => _provisioned = false;

    private string ResolveEntityPath(MessageEnvelope envelope)
    {
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw ThrowHelper.DispatchEndpointDestinationAddressInvalidUri();
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount != 2)
            {
                throw ThrowHelper.DispatchEndpointCannotDetermineDestinationName(destinationAddress);
            }

            var kind = path[ranges[0]];
            if (kind is not ("t" or "q"))
            {
                throw ThrowHelper.DispatchEndpointCannotDetermineDestinationName(destinationAddress);
            }

            return new string(path[ranges[1]]);
        }

        if (Topic is not null)
        {
            return Topic.Name;
        }

        if (Queue is not null)
        {
            return Queue.Name;
        }

        throw ThrowHelper.DispatchEndpointDestinationNotConfigured();
    }

    internal readonly record struct ScheduledDispatch(string EntityPath, long SequenceNumber);
}
