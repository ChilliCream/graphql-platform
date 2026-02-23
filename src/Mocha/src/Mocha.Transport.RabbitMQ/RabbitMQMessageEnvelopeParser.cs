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
            SentAt = props.Timestamp.UnixTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime) : null,
            DeliverBy = ParseExpiration(props.Expiration),
            // TODO quorum queues can use x-delivery-count instead of redelivered!
            DeliveryCount = eventArgs.Redelivered ? 1 : 0,
            Headers = BuildHeaders(props.Headers),
            EnclosedMessageTypes = props.Headers?.GetStringArray(RabbitMQMessageHeaders.EnclosedMessageTypes) ?? [],
            Body = eventArgs.Body
        };

        return envelope;
    }

    private static DateTimeOffset? ParseExpiration(string? expiration)
    {
        if (string.IsNullOrEmpty(expiration) || !long.TryParse(expiration, out var ms))
        {
            return null;
        }

        return DateTimeOffset.UtcNow.AddMilliseconds(ms);
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
            object? strValue = value switch
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
