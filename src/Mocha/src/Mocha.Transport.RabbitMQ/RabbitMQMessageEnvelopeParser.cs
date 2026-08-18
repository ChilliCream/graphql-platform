using System.Collections.Immutable;
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
    /// <param name="timeProvider">The clock used when expiration has no send timestamp.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(BasicDeliverEventArgs eventArgs, TimeProvider timeProvider)
    {
        var props = eventArgs.BasicProperties;
        var sentAt =
            props.Timestamp.UnixTime > 0
                ? DateTimeOffset.FromUnixTimeSeconds(props.Timestamp.UnixTime)
                : (DateTimeOffset?)null;

        var headers = props.Headers.ParseHeaders();

        var envelope = new MessageEnvelope
        {
            MessageId = props.MessageId,
            CorrelationId = props.CorrelationId,
            ConversationId = headers.GetString(MessageHeaders.Transport.ConversationId),
            CausationId = headers.GetString(MessageHeaders.Transport.CausationId),
            SourceAddress = headers.GetString(MessageHeaders.Transport.SourceAddress),
            DestinationAddress = headers.GetString(MessageHeaders.Transport.DestinationAddress),
            ResponseAddress = props.ReplyTo,
            FaultAddress = headers.GetString(MessageHeaders.Transport.FaultAddress),
            ContentType = props.ContentType,
            MessageType = props.Type ?? headers.GetString(MessageHeaders.Transport.MessageType),
            SentAt = sentAt,
            DeliverBy = props.Expiration.ParseExpiration(sentAt, timeProvider),
            DeliveryCount = headers.GetDeliveryCount(eventArgs.Redelivered),
            Headers = headers,
            EnclosedMessageTypes = headers.GetStringArray(MessageHeaders.Transport.EnclosedMessageTypes),
            Body = eventArgs.Body
        };

        return envelope;
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly RabbitMQMessageEnvelopeParser Instance = new();
}

file static class Extensions
{
    public static Headers ParseHeaders(this IDictionary<string, object?>? fieldTable)
    {
        if (fieldTable is null || fieldTable.Count == 0)
        {
            return Headers.Empty();
        }

        var headers = new Headers(fieldTable.Count);
        foreach (var (key, value) in fieldTable)
        {
            headers.Set(key, value.FromFieldTableValue(key));
        }

        return headers;
    }

    public static string? GetString(this Headers headers, ContextDataKey<string> key)
    {
        var value = headers.GetValue(key.Key);
        return value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : value?.ToString();
    }

    public static ImmutableArray<string> GetStringArray(
        this Headers headers,
        ContextDataKey<ImmutableArray<string>> key)
    {
        if (headers.GetValue(key.Key) is not object?[] values)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<string>(values.Length);
        foreach (var value in values)
        {
            if (value is string text)
            {
                builder.Add(text);
            }
            else if (value is byte[] bytes)
            {
                builder.Add(Encoding.UTF8.GetString(bytes));
            }
        }

        return builder.ToImmutable();
    }

    public static int GetDeliveryCount(this Headers headers, bool redelivered)
    {
        if (headers.TryGet(RabbitMQMessageHeaders.DeliveryCount, out var count))
        {
            return count > int.MaxValue ? int.MaxValue : (int)count;
        }

        return redelivered ? 1 : 0;
    }

    public static DateTimeOffset? ParseExpiration(
        this string? expiration,
        DateTimeOffset? sentAt,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrEmpty(expiration) || !long.TryParse(expiration, out var milliseconds))
        {
            return null;
        }

        var origin = sentAt ?? timeProvider.GetUtcNow();
        return origin.AddMilliseconds(milliseconds);
    }
}
