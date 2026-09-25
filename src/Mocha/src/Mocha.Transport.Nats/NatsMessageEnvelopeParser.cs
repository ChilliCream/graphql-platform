using System.Collections.Immutable;
using System.Globalization;
using Mocha.Middlewares;
using NATS.Client.Core;

namespace Mocha.Transport.Nats;

/// <summary>
/// Parses a received NATS message into a normalized <see cref="MessageEnvelope"/>.
/// </summary>
internal sealed class NatsMessageEnvelopeParser
{
    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly NatsMessageEnvelopeParser Instance = new();

    /// <summary>
    /// Converts the headers and payload of a delivery into a <see cref="MessageEnvelope"/>.
    /// </summary>
    /// <param name="headers">The headers of the received message, if any.</param>
    /// <param name="body">The raw message payload.</param>
    /// <param name="deliveryCount">
    /// The JetStream redelivery count taken from the message metadata, not from a header.
    /// </param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(NatsHeaders? headers, ReadOnlyMemory<byte> body, int deliveryCount)
    {
        return new MessageEnvelope
        {
            MessageId = Get(headers, NatsMessageHeaders.MessageId),
            CorrelationId = Get(headers, NatsMessageHeaders.CorrelationId),
            ConversationId = Get(headers, NatsMessageHeaders.ConversationId),
            CausationId = Get(headers, NatsMessageHeaders.CausationId),
            SourceAddress = Get(headers, NatsMessageHeaders.SourceAddress),
            DestinationAddress = Get(headers, NatsMessageHeaders.DestinationAddress),
            ResponseAddress = Get(headers, NatsMessageHeaders.ResponseAddress),
            FaultAddress = Get(headers, NatsMessageHeaders.FaultAddress),
            MessageType = Get(headers, NatsMessageHeaders.MessageType),
            ContentType = Get(headers, NatsMessageHeaders.ContentType),
            SentAt = GetTimestamp(headers, NatsMessageHeaders.SentAt),
            DeliverBy = GetTimestamp(headers, NatsMessageHeaders.DeliverBy),
            ScheduledTime = GetTimestamp(headers, NatsMessageHeaders.ScheduledTime),
            DeliveryCount = deliveryCount,
            EnclosedMessageTypes = GetEnclosedMessageTypes(headers),
            Headers = BuildHeaders(headers),
            Body = body
        };
    }

    private static string? Get(NatsHeaders? headers, string key)
        => headers is not null && headers.TryGetLastValue(key, out var value) ? value : null;

    private static DateTimeOffset? GetTimestamp(NatsHeaders? headers, string key)
    {
        var value = Get(headers, key);

        if (value is null)
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var timestamp)
            ? timestamp
            : null;
    }

    private static ImmutableArray<string> GetEnclosedMessageTypes(NatsHeaders? headers)
    {
        if (headers is null || !headers.TryGetValue(NatsMessageHeaders.EnclosedMessageTypes, out var values))
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<string>(values.Count);

        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
            {
                builder.Add(value);
            }
        }

        return builder.ToImmutable();
    }

    private static Headers BuildHeaders(NatsHeaders? headers)
    {
        if (headers is null || headers.Count == 0)
        {
            return Headers.Empty();
        }

        var result = new Headers(headers.Count);

        foreach (var (key, values) in headers)
        {
            if (NatsMessageHeaders.IsReserved(key))
            {
                continue;
            }

            if (values.Count == 1)
            {
                result.Set(key, values[0]);
            }
            else if (values.Count > 1)
            {
                result.Set(key, values.ToArray());
            }
        }

        return result;
    }
}
