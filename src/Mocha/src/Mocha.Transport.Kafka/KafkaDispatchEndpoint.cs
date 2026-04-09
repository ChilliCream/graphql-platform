using System.Runtime.InteropServices;
using System.Text;
using Confluent.Kafka;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Kafka dispatch endpoint that publishes outbound messages to a target topic
/// using the transport's shared producer.
/// </summary>
/// <param name="transport">The owning Kafka transport instance.</param>
public sealed class KafkaDispatchEndpoint(KafkaMessagingTransport transport)
    : DispatchEndpoint<KafkaDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target topic for this endpoint, or <c>null</c> if the topic has not been resolved.
    /// </summary>
    public KafkaTopic? Topic { get; private set; }

    private bool _isProvisioned;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        KafkaDispatchEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null)
        {
            throw new InvalidOperationException("Topic name is required");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        KafkaDispatchEndpointConfiguration configuration)
    {
        var topology = (KafkaMessagingTopology)Transport.Topology;

        Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName)
            ?? throw new InvalidOperationException($"Topic '{configuration.TopicName}' not found");

        Destination = Topic;
    }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        // Ensure the topic exists (lazy provisioning for dynamically-created endpoints)
        await EnsureProvisionedAsync(context.CancellationToken);

        var cancellationToken = context.CancellationToken;
        var connectionManager = transport.ConnectionManager;
        var producer = connectionManager.Producer;
        var topicName = ResolveTopicName(envelope);

        // Build Kafka message
        var key = SelectKey(envelope);
        var headers = BuildKafkaHeaders(envelope);

        // Use MemoryMarshal.TryGetArray to skip ToArray() when body is already backed by byte[]
        byte[] body;
        if (MemoryMarshal.TryGetArray(envelope.Body, out var segment)
            && segment.Offset == 0
            && segment.Count == segment.Array!.Length)
        {
            body = segment.Array;
        }
        else
        {
            body = envelope.Body.ToArray();
        }

        var message = new Message<byte[], byte[]>
        {
            Key = key!,
            Value = body,
            Headers = headers
        };

        // Use Produce() with callback for performance (avoids Task allocation from ProduceAsync)
        // TODO: consider IValueTaskSource pooling for high-throughput scenarios
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connectionManager.TrackInflight(tcs);

        // Link caller's cancellation token to TCS so cancellation unblocks the await
        await using var ctr = cancellationToken.Register(static state =>
        {
            var t = (TaskCompletionSource)state!;
            t.TrySetCanceled();
        }, tcs);

        try
        {
            producer.Produce(topicName, message, report =>
            {
                // This callback runs on librdkafka's delivery-report thread.
                // Do NOT access context or features here -- context may be pooled/recycled.
                if (report.Error.IsError)
                {
                    tcs.TrySetException(new KafkaException(report.Error));
                }
                else
                {
                    tcs.TrySetResult();
                }

                connectionManager.UntrackInflight(tcs);
            });
        }
        catch (ProduceException<byte[], byte[]> ex)
        {
            tcs.TrySetException(ex);
            connectionManager.UntrackInflight(tcs);
        }

        await tcs.Task;
    }

    private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var autoProvision = ((KafkaMessagingTopology)transport.Topology).AutoProvision;
        if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
        {
            await transport.ConnectionManager.ProvisionTopologyAsync([Topic], cancellationToken);
        }

        _isProvisioned = true;
    }

    private string ResolveTopicName(MessageEnvelope envelope)
    {
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount == 2 && path[ranges[0]] is "t")
            {
                return new string(path[ranges[1]]);
            }

            throw new InvalidOperationException(
                $"Cannot determine topic name from destination address {destinationAddress}");
        }

        return Topic!.Name;
    }

    private static byte[]? SelectKey(MessageEnvelope envelope)
    {
        var keySource = envelope.CorrelationId ?? envelope.MessageId;
        return keySource is not null ? Encoding.UTF8.GetBytes(keySource) : null;
    }

    private static Confluent.Kafka.Headers BuildKafkaHeaders(MessageEnvelope envelope)
    {
        var headers = new Confluent.Kafka.Headers();

        // Map well-known envelope fields to Kafka headers
        if (envelope.MessageId is not null)
        {
            headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes(envelope.MessageId));
        }

        if (envelope.CorrelationId is not null)
        {
            headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes(envelope.CorrelationId));
        }

        if (envelope.ConversationId is not null)
        {
            headers.Add(KafkaMessageHeaders.ConversationId, Encoding.UTF8.GetBytes(envelope.ConversationId));
        }

        if (envelope.CausationId is not null)
        {
            headers.Add(KafkaMessageHeaders.CausationId, Encoding.UTF8.GetBytes(envelope.CausationId));
        }

        if (envelope.SourceAddress is not null)
        {
            headers.Add(KafkaMessageHeaders.SourceAddress, Encoding.UTF8.GetBytes(envelope.SourceAddress));
        }

        if (envelope.DestinationAddress is not null)
        {
            headers.Add(KafkaMessageHeaders.DestinationAddress, Encoding.UTF8.GetBytes(envelope.DestinationAddress));
        }

        if (envelope.ResponseAddress is not null)
        {
            headers.Add(KafkaMessageHeaders.ResponseAddress, Encoding.UTF8.GetBytes(envelope.ResponseAddress));
        }

        if (envelope.FaultAddress is not null)
        {
            headers.Add(KafkaMessageHeaders.FaultAddress, Encoding.UTF8.GetBytes(envelope.FaultAddress));
        }

        if (envelope.ContentType is not null)
        {
            headers.Add(KafkaMessageHeaders.ContentType, Encoding.UTF8.GetBytes(envelope.ContentType));
        }

        if (envelope.MessageType is not null)
        {
            headers.Add(KafkaMessageHeaders.MessageType, Encoding.UTF8.GetBytes(envelope.MessageType));
        }

        if (envelope.SentAt is not null)
        {
            headers.Add(KafkaMessageHeaders.SentAt, Encoding.UTF8.GetBytes(envelope.SentAt.Value.ToString("O")));
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosed)
        {
            headers.Add(KafkaMessageHeaders.EnclosedMessageTypes,
                Encoding.UTF8.GetBytes(string.Join(",", enclosed)));
        }

        // Map custom headers
        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value.ToString()!));
                }
            }
        }

        return headers;
    }
}
