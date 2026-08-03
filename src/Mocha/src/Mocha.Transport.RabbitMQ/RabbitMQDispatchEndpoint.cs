using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocha.Middlewares;
using RabbitMQ.Client;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ dispatch endpoint that publishes outbound messages to a target queue or exchange
/// using pooled channels from the transport's dispatcher.
/// </summary>
/// <param name="transport">The owning RabbitMQ transport instance.</param>
public sealed class RabbitMQDispatchEndpoint(RabbitMQMessagingTransport transport)
    : DispatchEndpoint<RabbitMQDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue for this endpoint, or <c>null</c> if the endpoint targets an exchange.
    /// </summary>
    public RabbitMQQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target exchange for this endpoint, or <c>null</c> if the endpoint targets a queue.
    /// </summary>
    public RabbitMQExchange? Exchange { get; private set; }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var dispatcher = transport.Dispatcher;
        var cancellationToken = context.CancellationToken;
        var channel = await dispatcher.RentChannelAsync(cancellationToken);
        try
        {
            await EnsureProvisionedAsync(channel, cancellationToken);
            await DispatchAsync(channel, envelope, cancellationToken);
        }
        finally
        {
            await dispatcher.ReturnChannelAsync(channel);
        }
    }

    private async ValueTask DispatchAsync(
        IChannel channel,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var exchangeName = CachedString.Empty;
        var routingKey = CachedString.Empty;
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[3];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            int kindIndex;
            int nameIndex;

            if (segmentCount == 3)
            {
                // vhost/kind/name - vhost adds an extra leading segment
                kindIndex = 1;
                nameIndex = 2;
            }
            else if (segmentCount == 2)
            {
                // kind/name - default vhost "/" disappears with RemoveEmptyEntries
                kindIndex = 0;
                nameIndex = 1;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine exchange or queue name from destination address {destinationAddress}");
            }

            var kind = path[ranges[kindIndex]];
            var name = path[ranges[nameIndex]];

            if (kind is "e" && name is var exchangeSegment)
            {
                exchangeName = new CachedString(new string(exchangeSegment));
                if (destinationAddress.TryGetRoutingKey(out var routingKeyValue))
                {
                    routingKey = new CachedString(routingKeyValue);
                }
            }
            else if (kind is "q" && name is var queueSegment)
            {
                routingKey = new CachedString(new string(queueSegment));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine exchange or queue name from destination address {destinationAddress}");
            }
        }
        else
        {
            if (Exchange is not null)
            {
                exchangeName = Exchange.CachedName;

                if (envelope.Headers is not null
                    && envelope.Headers.TryGet(RabbitMQMessageHeaders.RoutingKey, out var rk))
                {
                    routingKey = new CachedString(rk);
                }
            }
            else if (Queue is not null)
            {
                routingKey = Queue.CachedName;
            }
        }

        var headers = envelope.BuildHeaders();

        var messageType = envelope.MessageType ?? headers.Get(RabbitMQMessageHeaders.MessageType);

        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Type = messageType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ReplyTo = envelope.ResponseAddress,
            Headers = headers,
            ContentType = envelope.ContentType,
            DeliveryMode = DeliveryModes.Persistent
            // TODO wire up durable
            // TODO expiration
            // TODO priority
        };

        await channel.BasicPublishAsync(exchangeName, routingKey, true, properties, envelope.Body, cancellationToken);
    }

    private bool _isProvisioned;

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var autoProvision = ((RabbitMQMessagingTopology)transport.Topology).AutoProvision;

        if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
        {
            await Queue.ProvisionAsync(channel, cancellationToken);
        }

        if (Exchange is not null && (Exchange.AutoProvision ?? autoProvision))
        {
            await Exchange.ProvisionAsync(channel, cancellationToken);
        }

        _isProvisioned = true;
    }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        if (configuration.ExchangeName is null && configuration.QueueName is null)
        {
            throw new InvalidOperationException("Exchange name or queue name is required");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        var topology = (RabbitMQMessagingTopology)Transport.Topology;
        if (configuration.ExchangeName is not null)
        {
            Exchange =
                topology.Exchanges.FirstOrDefault(e => e.Name == configuration.ExchangeName)
                ?? throw new InvalidOperationException("Exchange not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Exchange as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}

/// <summary>
/// Extension methods for building RabbitMQ-specific message headers from a <see cref="MessageEnvelope"/>.
/// </summary>
public static class RabbitMQDispatchContextExtensions
{
    internal static IDictionary<string, object?> BuildHeaders(this MessageEnvelope envelope)
    {
        var headerCount =
            (envelope.ConversationId is not null ? 1 : 0)
            + (envelope.CausationId is not null ? 1 : 0)
            + (envelope.SourceAddress is not null ? 1 : 0)
            + (envelope.DestinationAddress is not null ? 1 : 0)
            + (envelope.FaultAddress is not null ? 1 : 0)
            + (envelope.Headers?.Count ?? 0);

        var headers = new Dictionary<string, object?>(headerCount);

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                headers[header.Key] = ToTableValue(header.Value, header.Key, 0);
            }
        }

        if (envelope.ConversationId is not null)
        {
            headers.Set(RabbitMQMessageHeaders.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            headers.Set(RabbitMQMessageHeaders.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 })
        {
            headers.Set(RabbitMQMessageHeaders.EnclosedMessageTypes, envelope.EnclosedMessageTypes.Value);
        }

        if (envelope.MessageType is not null)
        {
            headers.Set(RabbitMQMessageHeaders.MessageType, envelope.MessageType);
        }

        return headers;
    }

    /// <summary>
    /// The greatest depth to which a header value is mapped. A value nested deeper, or one that
    /// contains itself, is rejected.
    /// </summary>
    private const int MaxTableDepth = 64;

    /// <summary>
    /// Maps a header value onto the CLR types an AMQP field table accepts, at every level of a nested
    /// value. The inverse of the mapping the receive side applies.
    /// </summary>
    private static object? ToTableValue(object? value, string key, int depth)
    {
        if (depth > MaxTableDepth)
        {
            throw new InvalidOperationException(
                $"The header '{key}' is nested more than {MaxTableDepth} levels deep, or holds a "
                    + "value that contains itself, and cannot be written as an AMQP field table.");
        }

        switch (value)
        {
            // must precede the collection cases
            case string:
                return value;

            case DateTimeOffset dateTimeOffset:
                return new AmqpTimestamp(dateTimeOffset.ToUnixTimeSeconds());

            case DateTime dateTime:
                return ToTimestamp(dateTime);

            // a field table has no unsigned 64 bit type
            case ulong unsigned when unsigned > long.MaxValue:
                return unsigned.ToString(CultureInfo.InvariantCulture);

            case ulong unsigned:
                return (long)unsigned;

            // a field table decimal holds a 32 bit mantissa
            case decimal number when !FitsTableDecimal(number):
                return number.ToString(CultureInfo.InvariantCulture);

            // the text forms every transport shares
            case char character:
                return character.ToString();

            case Guid guid:
                return guid.ToString();

            case Uri uri:
                return HeaderValueText.From(uri);

            case TimeSpan timeSpan:
                return HeaderValueText.From(timeSpan);

            case DateOnly date:
                return HeaderValueText.From(date);

            case TimeOnly time:
                return HeaderValueText.From(time);

            case Enum enumeration:
                return HeaderValueText.From(enumeration);

            // the table's own shapes rather than JSON text
            case JsonElement element:
                return ToTableValue(HeadersJsonConverter.ReadHeaderValue(element), key, depth);

            case JsonDocument document:
                return ToTableValue(HeadersJsonConverter.ReadHeaderValue(document.RootElement), key, depth);

            case JsonNode node:
                using (var parsed = JsonDocument.Parse(node.ToJsonString()))
                {
                    return ToTableValue(HeadersJsonConverter.ReadHeaderValue(parsed.RootElement), key, depth);
                }

            // the field table's byte array type, not a long string
            case byte[] bytes:
                return new BinaryTableValue(bytes);

            // an empty segment has no array to copy, and a whole one needs no copy
            case ArraySegment<byte> { Array: null }:
                return new BinaryTableValue([]);

            case ArraySegment<byte> { Offset: 0 } whole when whole.Count == whole.Array!.Length:
                return new BinaryTableValue(whole.Array);

            case ArraySegment<byte> segment:
                return new BinaryTableValue(segment.ToArray());

            case ReadOnlyMemory<byte> memory:
                return new BinaryTableValue(memory.ToArray());

            case Memory<byte> writableMemory:
                return new BinaryTableValue(writableMemory.ToArray());

            case IReadOnlyHeaders nested:
                var mappedHeaders = new Dictionary<string, object?>();
                foreach (var header in nested)
                {
                    mappedHeaders[header.Key] = ToTableValue(header.Value, key, depth + 1);
                }

                return mappedHeaders;

            case IDictionary table:
                return ToTable(table, key, depth);

            case IEnumerable sequence:
                var mappedSequence = sequence is ICollection collection
                    ? new List<object?>(collection.Count)
                    : [];
                foreach (var item in sequence)
                {
                    mappedSequence.Add(ToTableValue(item, key, depth + 1));
                }

                return mappedSequence;

            default:
                return value;
        }
    }

    private static Dictionary<string, object?> ToTable(IDictionary table, string key, int depth)
    {
        var mapped = new Dictionary<string, object?>(table.Count);

        foreach (DictionaryEntry entry in table)
        {
            // a field table names its entries with text; the client stringifies any other key
            if (entry.Key is not { } entryKey)
            {
                continue;
            }

            var name = entryKey as string ?? entryKey.ToString();

            if (name is null)
            {
                continue;
            }

            mapped[name] = ToTableValue(entry.Value, key, depth + 1);
        }

        return mapped;
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to the instant a field table carries. A local time is
    /// converted from this host; a time with no zone is read as UTC.
    /// </summary>
    private static AmqpTimestamp ToTimestamp(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;

        return new AmqpTimestamp(new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    private static bool FitsTableDecimal(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);

        return bits[1] == 0 && bits[2] == 0 && (uint)bits[0] <= int.MaxValue;
    }
}
