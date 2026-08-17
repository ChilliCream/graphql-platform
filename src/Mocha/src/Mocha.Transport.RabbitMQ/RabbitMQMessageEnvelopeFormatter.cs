using System.Globalization;
using Mocha.Middlewares;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

internal static class RabbitMQMessageEnvelopeFormatter
{
    public static BasicProperties Format(MessageEnvelope envelope, TimeProvider timeProvider)
    {
        var sentAt = timeProvider.GetUtcNow();
        var headers = FormatHeaders(envelope);
        var messageType = envelope.MessageType ?? headers.Get(MessageHeaders.Transport.MessageType);

        return new BasicProperties
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Type = messageType,
            Timestamp = sentAt.ToAmqpTimestamp(),
            Expiration = envelope.DeliverBy.ToExpiration(sentAt),
            ReplyTo = envelope.ResponseAddress,
            Headers = headers,
            ContentType = envelope.ContentType,
            DeliveryMode = DeliveryModes.Persistent
        };
    }

    private static IDictionary<string, object?> FormatHeaders(MessageEnvelope envelope)
    {
        var headerCount =
            (envelope.ConversationId is not null ? 1 : 0)
            + (envelope.CausationId is not null ? 1 : 0)
            + (envelope.SourceAddress is not null ? 1 : 0)
            + (envelope.DestinationAddress is not null ? 1 : 0)
            + (envelope.FaultAddress is not null ? 1 : 0)
            + (envelope.EnclosedMessageTypes is { Length: > 0 } ? 1 : 0)
            + (envelope.MessageType is not null ? 1 : 0)
            + (envelope.Headers?.Count ?? 0);

        var headers = new Dictionary<string, object?>(headerCount);

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                headers[header.Key] = header.Value.ToFieldTableValue(header.Key);
            }
        }

        if (envelope.ConversationId is not null)
        {
            headers.Set(MessageHeaders.Transport.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            headers.Set(MessageHeaders.Transport.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            headers.Set(MessageHeaders.Transport.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            headers.Set(MessageHeaders.Transport.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            headers.Set(MessageHeaders.Transport.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 })
        {
            headers.Set(MessageHeaders.Transport.EnclosedMessageTypes, envelope.EnclosedMessageTypes.Value);
        }

        if (envelope.MessageType is not null)
        {
            headers.Set(MessageHeaders.Transport.MessageType, envelope.MessageType);
        }

        return headers;
    }
}

file static class Extensions
{
    public static string? ToExpiration(this DateTimeOffset? deliverBy, DateTimeOffset sentAt)
    {
        if (deliverBy is null)
        {
            return null;
        }

        var timeToLive = deliverBy.Value - sentAt;
        if (timeToLive <= TimeSpan.Zero)
        {
            return "0";
        }

        return ((long)Math.Ceiling(timeToLive.TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);
    }
}
