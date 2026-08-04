using System.Text;
using System.Text.Unicode;
using Mocha.Middlewares;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Parses raw RabbitMQ <see cref="BasicDeliverEventArgs"/> into a normalized <see cref="MessageEnvelope"/>,
/// extracting standard message properties, custom headers, and the message body.
/// </summary>
internal sealed class RabbitMQMessageEnvelopeParser
{
    /// <summary>
    /// The greatest depth to which a header value is read. A value nested deeper is rejected.
    /// </summary>
    private const int MaxHeaderDepth = 64;

    /// <summary>
    /// The range a <see cref="DateTimeOffset"/> can express, which bounds the timestamps mapped to one.
    /// </summary>
    private static readonly long s_minTimestampSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();

    private static readonly long s_maxTimestampSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

    /// <summary>
    /// Converts a RabbitMQ delivery into a <see cref="MessageEnvelope"/> by mapping AMQP basic properties
    /// and custom headers to envelope fields.
    /// </summary>
    /// <param name="eventArgs">The delivery event args containing the message body, properties, and metadata.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(BasicDeliverEventArgs eventArgs)
    {
        var props = eventArgs.BasicProperties;
        var sentAt = props.Timestamp.UnixTime > 0
            ? DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime)
            : (DateTimeOffset?)null;

        var envelope = new MessageEnvelope
        {
            MessageId = props.MessageId,
            CorrelationId = props.CorrelationId,
            ConversationId = props.Headers?.GetString(RabbitMQMessageHeaders.ConversationId),
            CausationId = props.Headers?.GetString(RabbitMQMessageHeaders.CausationId),
            SourceAddress = props.Headers?.GetString(RabbitMQMessageHeaders.SourceAddress),
            DestinationAddress = props.Headers?.GetString(RabbitMQMessageHeaders.DestinationAddress),
            ResponseAddress = props.ReplyTo,
            FaultAddress = props.Headers?.GetString(RabbitMQMessageHeaders.FaultAddress),
            ContentType = props.ContentType,
            MessageType = props.Type ?? props.Headers?.GetString(RabbitMQMessageHeaders.MessageType),
            SentAt = sentAt,
            DeliverBy = ParseExpiration(props.Expiration, sentAt),
            DeliveryCount = GetDeliveryCount(props.Headers, eventArgs.Redelivered),
            Headers = BuildHeaders(props.Headers),
            EnclosedMessageTypes = props.Headers?.GetStringArray(RabbitMQMessageHeaders.EnclosedMessageTypes) ?? [],
            Body = eventArgs.Body
        };

        return envelope;
    }

    /// <summary>
    /// Returns the delivery count from the quorum queue <c>x-delivery-count</c> header when
    /// available; otherwise falls back to the classic queue <c>Redelivered</c> flag.
    /// </summary>
    private static int GetDeliveryCount(IDictionary<string, object?>? headers, bool redelivered)
    {
        if (headers is not null
            && headers.TryGetValue("x-delivery-count", out var value)
            && value is long count)
        {
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        return redelivered ? 1 : 0;
    }

    private static DateTimeOffset? ParseExpiration(string? expiration, DateTimeOffset? sentAt)
    {
        if (string.IsNullOrEmpty(expiration) || !long.TryParse(expiration, out var ms))
        {
            return null;
        }

        // AMQP expiration is a per-message TTL in milliseconds set at publish time.
        // Compute deliver-by relative to the send timestamp when available.
        var origin = sentAt ?? DateTimeOffset.UtcNow;
        return origin.AddMilliseconds(ms);
    }

    private static Headers BuildHeaders(IDictionary<string, object?>? headers)
    {
        if (headers is null || headers.Count == 0)
        {
            return Headers.Empty();
        }

        var result = new Headers(headers.Count);
        foreach (var (key, value) in headers)
        {
            result.Set(key, NormalizeValue(value, key, 0));
        }

        return result;
    }

    /// <summary>
    /// Maps AMQP wire types onto the CLR types the envelope serializer understands, at every level of
    /// a nested value. Throws when a value is nested deeper than <see cref="MaxHeaderDepth"/>.
    /// </summary>
    private static object? NormalizeValue(object? value, string key, int depth)
    {
        if (depth > MaxHeaderDepth)
        {
            throw new InvalidOperationException(
                $"The header '{key}' is nested more than {MaxHeaderDepth} levels deep and cannot be "
                    + "read as a message header.");
        }

        switch (value)
        {
            // a binary field is kept as bytes without being tested for text
            case BinaryTableValue binary:
                return binary.Bytes;

            // a long string carries text and binary alike, so content is the only signal
            case byte[] bytes:
                return Utf8.IsValid(bytes) ? Encoding.UTF8.GetString(bytes) : bytes;

            // a value outside the range a date can express is kept as the number it is
            case AmqpTimestamp timestamp:
                return timestamp.UnixTime >= s_minTimestampSeconds
                    && timestamp.UnixTime <= s_maxTimestampSeconds
                        ? DateTimeOffset.FromUnixTimeSeconds(timestamp.UnixTime)
                        : timestamp.UnixTime;

            case IDictionary<string, object?> table:
                var mappedTable = new Dictionary<string, object?>(table.Count);
                foreach (var (entryKey, item) in table)
                {
                    mappedTable[entryKey] = NormalizeValue(item, key, depth + 1);
                }

                return mappedTable;

            case IList<object?> array:
                var mappedArray = new object?[array.Count];
                for (var i = 0; i < array.Count; i++)
                {
                    mappedArray[i] = NormalizeValue(array[i], key, depth + 1);
                }

                return mappedArray;

            default:
                return value;
        }
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly RabbitMQMessageEnvelopeParser Instance = new();
}
