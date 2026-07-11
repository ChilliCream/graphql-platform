using System.Text;
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
            var strValue = value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                AmqpTimestamp timestamp => DateTimeOffset.FromUnixTimeSeconds(timestamp.UnixTime),
                _ => value
            };

            result.Set(key, strValue);
        }

        return result;
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly RabbitMQMessageEnvelopeParser Instance = new();
}
