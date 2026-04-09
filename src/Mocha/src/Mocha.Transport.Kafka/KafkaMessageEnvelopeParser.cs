using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Confluent.Kafka;
using Mocha.Middlewares;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Converts a raw Kafka <see cref="ConsumeResult{TKey, TValue}"/> into a normalized <see cref="MessageEnvelope"/>,
/// extracting standard message properties, custom headers, and the message body.
/// </summary>
internal sealed class KafkaMessageEnvelopeParser
{
    /// <summary>
    /// Converts a Kafka consume result into a <see cref="MessageEnvelope"/> by mapping Kafka headers
    /// to envelope fields.
    /// </summary>
    /// <param name="consumeResult">The consume result containing the message body and headers.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(ConsumeResult<byte[], byte[]> consumeResult)
    {
        var kafkaHeaders = consumeResult.Message.Headers;

        var envelope = new MessageEnvelope
        {
            MessageId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.MessageId),
            CorrelationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CorrelationId),
            ConversationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ConversationId),
            CausationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CausationId),
            SourceAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.SourceAddress),
            DestinationAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.DestinationAddress),
            ResponseAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ResponseAddress),
            FaultAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.FaultAddress),
            ContentType = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ContentType),
            MessageType = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.MessageType),
            SentAt = ParseSentAt(kafkaHeaders),
            EnclosedMessageTypes = ParseEnclosedMessageTypes(kafkaHeaders),
            Headers = BuildHeaders(kafkaHeaders),
            Body = consumeResult.Message.Value ?? Array.Empty<byte>()
        };

        return envelope;
    }

    private static string? GetHeaderString(Confluent.Kafka.Headers? headers, string key)
    {
        if (headers is null)
        {
            return null;
        }

        if (headers.TryGetLastBytes(key, out var bytes))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }

    private static DateTimeOffset? ParseSentAt(Confluent.Kafka.Headers? headers)
    {
        var value = GetHeaderString(headers, KafkaMessageHeaders.SentAt);
        if (value is not null && DateTimeOffset.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// EnclosedMessageTypes is serialized as a comma-separated string.
    /// Split on commas to reconstruct the list.
    /// </summary>
    private static ImmutableArray<string>? ParseEnclosedMessageTypes(Confluent.Kafka.Headers? headers)
    {
        var value = GetHeaderString(headers, KafkaMessageHeaders.EnclosedMessageTypes);
        if (value is null || value.Length == 0)
        {
            return null;
        }

        var span = value.AsSpan();
        Span<Range> ranges = stackalloc Range[16];
        var count = span.Split(ranges, ',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (count == 0)
        {
            return null;
        }

        var builder = ImmutableArray.CreateBuilder<string>(count);
        for (var i = 0; i < count; i++)
        {
            builder.Add(new string(span[ranges[i]]));
        }

        return builder.MoveToImmutable();
    }

    private static Mocha.Headers BuildHeaders(Confluent.Kafka.Headers? kafkaHeaders)
    {
        if (kafkaHeaders is null || kafkaHeaders.Count == 0)
        {
            return Mocha.Headers.Empty();
        }

        // First pass: count non-well-known headers to avoid allocation when all are well-known
        var customCount = 0;
        foreach (var header in kafkaHeaders)
        {
            if (!IsWellKnownHeader(header.Key))
            {
                customCount++;
            }
        }

        if (customCount == 0)
        {
            return Mocha.Headers.Empty();
        }

        // Second pass: populate custom headers
        var result = new Mocha.Headers(customCount);
        foreach (var header in kafkaHeaders)
        {
            if (IsWellKnownHeader(header.Key))
            {
                continue;
            }

            if (header.GetValueBytes() is { } bytes)
            {
                result.Set(header.Key, Encoding.UTF8.GetString(bytes));
            }
        }

        return result;
    }

    private static readonly FrozenSet<string> _wellKnownHeaders = FrozenSet.ToFrozenSet(
        [
            KafkaMessageHeaders.MessageId,
            KafkaMessageHeaders.CorrelationId,
            KafkaMessageHeaders.ConversationId,
            KafkaMessageHeaders.CausationId,
            KafkaMessageHeaders.SourceAddress,
            KafkaMessageHeaders.DestinationAddress,
            KafkaMessageHeaders.ResponseAddress,
            KafkaMessageHeaders.FaultAddress,
            KafkaMessageHeaders.ContentType,
            KafkaMessageHeaders.MessageType,
            KafkaMessageHeaders.SentAt,
            KafkaMessageHeaders.EnclosedMessageTypes
        ],
        StringComparer.Ordinal);

    private static bool IsWellKnownHeader(string key) => _wellKnownHeaders.Contains(key);

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly KafkaMessageEnvelopeParser Instance = new();
}
