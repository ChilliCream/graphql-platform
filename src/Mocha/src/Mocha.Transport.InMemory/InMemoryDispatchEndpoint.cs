using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.InMemory;

/// <summary>
/// A dispatch endpoint that sends messages to an in-memory queue or publishes them through
/// an in-memory topic.
/// </summary>
/// <remarks>
/// During completion the endpoint resolves its target resource from the topology. For reply
/// endpoints the destination is determined dynamically from the envelope's destination address
/// at dispatch time.
/// </remarks>
public sealed class InMemoryDispatchEndpoint(InMemoryMessagingTransport transport)
    : DispatchEndpoint<InMemoryDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue, or <c>null</c> if this endpoint dispatches to a topic.
    /// </summary>
    public InMemoryQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target topic, or <c>null</c> if this endpoint dispatches to a queue.
    /// </summary>
    public InMemoryTopic? Topic { get; private set; }

    private InMemoryMessagingTopology _topology = null!;
    private TimeProvider _timeProvider = TimeProvider.System;
    private InMemoryScheduler? _scheduler;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        InMemoryDispatchEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null && configuration.QueueName is null)
        {
            throw new InvalidOperationException("Exchange name or queue name is required");
        }
    }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var cancellationToken = context.CancellationToken;

        IInMemoryResource? resource = null;

        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "t" && name is var topicSegment)
                {
                    resource = _topology.GetTopic(topicSegment);
                }
                else if (kind is "q" && name is var queueSegment)
                {
                    resource = _topology.GetQueue(queueSegment);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot determine topic or queue name from destination address {destinationAddress}");
                }
            }

            if (resource is null)
            {
                throw new InvalidOperationException(
                    $"Cannot determine topic or queue name from destination address {destinationAddress}");
            }
        }
        else
        {
            if (Topic is not null)
            {
                resource = Topic;
            }
            else if (Queue is not null)
            {
                resource = Queue;
            }
        }

        if (resource is null)
        {
            throw new InvalidOperationException("Resource not found");
        }

        if (envelope.ScheduledTime is { } scheduledTime)
        {
            var delay = scheduledTime - _timeProvider.GetUtcNow();
            if (delay > TimeSpan.Zero && _scheduler is not null)
            {
                _scheduler.Schedule(envelope, resource, scheduledTime);
                return;
            }
        }

        await resource.SendAsync(envelope, cancellationToken);
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        InMemoryDispatchEndpointConfiguration configuration)
    {
        _topology = (InMemoryMessagingTopology)Transport.Topology;
        _timeProvider = context.Services.GetService<TimeProvider>() ?? TimeProvider.System;
        _scheduler = ((InMemoryMessagingTransport)Transport).Scheduler;

        if (configuration.TopicName is not null)
        {
            Topic =
                _topology.Topics.FirstOrDefault(e => e.Name == configuration.TopicName)
                ?? throw new InvalidOperationException("Exchange not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                _topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Topic as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
