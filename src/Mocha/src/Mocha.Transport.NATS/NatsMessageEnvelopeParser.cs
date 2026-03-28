using Mocha.Middlewares;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Mocha.Transport.NATS;

/// <summary>
/// Parses raw <see cref="INatsJSMsg{T}"/> into a normalized <see cref="MessageEnvelope"/>,
/// extracting NATS headers, delivery metadata, and the message body.
/// </summary>
internal sealed class NatsMessageEnvelopeParser
{
    /// <summary>
    /// Converts a JetStream message into a <see cref="MessageEnvelope"/> by mapping NATS headers
    /// and metadata to envelope fields.
    /// </summary>
    /// <param name="msg">The JetStream message containing headers, body, and delivery metadata.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(INatsJSMsg<ReadOnlyMemory<byte>> msg)
    {
        var headers = msg.Headers;

        return new MessageEnvelope
        {
            MessageId = headers?.GetString(NatsMessageHeaders.MessageId),
            CorrelationId = headers?.GetString(NatsMessageHeaders.CorrelationId),
            ConversationId = headers?.GetString(NatsMessageHeaders.ConversationId),
            CausationId = headers?.GetString(NatsMessageHeaders.CausationId),
            SourceAddress = headers?.GetString(NatsMessageHeaders.SourceAddress),
            DestinationAddress = headers?.GetString(NatsMessageHeaders.DestinationAddress),
            ResponseAddress = headers?.GetString(NatsMessageHeaders.ResponseAddress),
            FaultAddress = headers?.GetString(NatsMessageHeaders.FaultAddress),
            ContentType = headers?.GetString(NatsMessageHeaders.ContentType),
            MessageType = headers?.GetString(NatsMessageHeaders.MessageType),
            SentAt = headers?.GetDateTimeOffset(NatsMessageHeaders.SentAt),
            DeliverBy = headers?.GetDateTimeOffset(NatsMessageHeaders.DeliverBy),
            DeliveryCount = GetDeliveryCount(msg),
            Headers = BuildHeaders(headers),
            EnclosedMessageTypes = headers?.GetStringArray(NatsMessageHeaders.EnclosedMessageTypes),
            Body = msg.Data
        };
    }

    private static int GetDeliveryCount(INatsJSMsg<ReadOnlyMemory<byte>> msg)
    {
        var metadata = msg.Metadata;
        return metadata is not null
            ? (int)Math.Min(metadata.Value.NumDelivered, int.MaxValue)
            : 0;
    }

    private static Headers BuildHeaders(NatsHeaders? natsHeaders)
    {
        if (natsHeaders is null || natsHeaders.Count == 0)
        {
            return Headers.Empty();
        }

        Headers? result = null;
        foreach (var (key, values) in natsHeaders)
        {
            if (key.StartsWith("x-mocha-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result ??= new Headers(natsHeaders.Count);
            result.Set(key, values.ToString());
        }

        return result ?? Headers.Empty();
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly NatsMessageEnvelopeParser Instance = new();
}
