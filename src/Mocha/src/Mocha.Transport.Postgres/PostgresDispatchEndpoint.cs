using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A dispatch endpoint that sends messages to a PostgreSQL queue or publishes them through
/// a PostgreSQL topic using the message store.
/// </summary>
/// <remarks>
/// During completion the endpoint resolves its target resource from the topology. For reply
/// endpoints the destination is determined dynamically from the envelope's destination address
/// at dispatch time.
/// </remarks>
public sealed class PostgresDispatchEndpoint(PostgresMessagingTransport transport)
    : DispatchEndpoint<PostgresDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue, or <c>null</c> if this endpoint dispatches to a topic.
    /// </summary>
    public PostgresQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target topic, or <c>null</c> if this endpoint dispatches to a queue.
    /// </summary>
    public PostgresTopic? Topic { get; private set; }

    private PostgresMessagingTopology _topology = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        PostgresDispatchEndpointConfiguration configuration)
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

        var cancellationToken = context.CancellationToken;
        var messageStore = transport.MessageStore;

        var feature = context.Features.GetOrSet<JsonHeadersFeature>();
        var headers = PostgresMessageHeadersWriter.Write(feature, envelope);
        var body = envelope.Body;
        var target = PostgresDispatchTargetResolver.Resolve(this, envelope);

        if (target.IsTopic)
        {
            await messageStore.PublishAsync(body, headers, target.Name, scheduledTime: null, cancellationToken);
        }
        else
        {
            await messageStore.SendAsync(body, headers, target.Name, scheduledTime: null, cancellationToken);
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        PostgresDispatchEndpointConfiguration configuration)
    {
        _topology = (PostgresMessagingTopology)Transport.Topology;

        if (configuration.TopicName is not null)
        {
            Topic =
                _topology.Topics.FirstOrDefault(e => e.Name == configuration.TopicName)
                ?? throw new InvalidOperationException("Topic not found");
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
