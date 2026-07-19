using System.Buffers;
using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Parses a raw Azure Service Bus <see cref="ServiceBusReceivedMessage"/> into a normalized
/// <see cref="MessageEnvelope"/>, extracting native AMQP properties, custom application properties,
/// and the message body.
/// </summary>
internal sealed class AzureServiceBusMessageEnvelopeParser
{
    /// <summary>
    /// Converts an Azure Service Bus received message into a <see cref="MessageEnvelope"/> by mapping
    /// native AMQP properties and custom application properties to envelope fields.
    /// </summary>
    /// <param name="message">The received message from the Azure Service Bus processor.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(ServiceBusReceivedMessage message)
    {
        // Use GetRawAmqpMessage().ApplicationProperties to avoid ReadOnlyDictionary wrapper allocation
        var amqp = message.GetRawAmqpMessage();
        var props = amqp.ApplicationProperties;

        var sentAt = props.TryGetValue(AzureServiceBusMessageHeaders.SentAt, out var sentAtValue)
            && sentAtValue is long sentAtMs
            ? DateTimeOffset.FromUnixTimeMilliseconds(sentAtMs)
            : (DateTimeOffset?)null;

        var envelope = new MessageEnvelope
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            ConversationId = props.GetString(AzureServiceBusMessageHeaders.ConversationId),
            CausationId = props.GetString(AzureServiceBusMessageHeaders.CausationId),
            SourceAddress = props.GetString(AzureServiceBusMessageHeaders.SourceAddress),
            DestinationAddress = props.GetString(AzureServiceBusMessageHeaders.DestinationAddress),
            ResponseAddress = message.ReplyTo,
            FaultAddress = props.GetString(AzureServiceBusMessageHeaders.FaultAddress),
            ContentType = message.ContentType,
            MessageType = message.Subject
                ?? props.GetString(AzureServiceBusMessageHeaders.MessageType),
            SentAt = sentAt ?? message.EnqueuedTime,
            DeliverBy = message.ExpiresAt != DateTimeOffset.MaxValue ? message.ExpiresAt : null,
            DeliveryCount = Math.Max(message.DeliveryCount - 1, 0), // Convention: first delivery = 0, matching RabbitMQ and core dispatch/retry semantics.
            Headers = BuildHeaders(props, message),
            EnclosedMessageTypes = ParseEnclosedMessageTypes(props),
            Body = message.Body.ToMemory()  // Zero-copy
        };

        return envelope;
    }

    private static ImmutableArray<string> ParseEnclosedMessageTypes(
        IDictionary<string, object?> props)
    {
        if (props.TryGetValue(AzureServiceBusMessageHeaders.EnclosedMessageTypes, out var value)
            && value is string encoded && !string.IsNullOrEmpty(encoded))
        {
            var span = encoded.AsSpan();
            var maximumRangeCount = 1;
            foreach (var character in span)
            {
                if (character == ';')
                {
                    maximumRangeCount++;
                }
            }

            Range[]? rentedRanges = null;
            Span<Range> ranges = maximumRangeCount <= 32
                ? stackalloc Range[32]
                : rentedRanges = ArrayPool<Range>.Shared.Rent(maximumRangeCount);

            try
            {
                var count = span.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries);
                var builder = ImmutableArray.CreateBuilder<string>(count);
                for (var i = 0; i < count; i++)
                {
                    builder.Add(new string(span[ranges[i]]));
                }

                return builder.MoveToImmutable();
            }
            finally
            {
                if (rentedRanges is not null)
                {
                    ArrayPool<Range>.Shared.Return(rentedRanges);
                }
            }
        }

        return [];
    }

    private static Headers BuildHeaders(
        IDictionary<string, object?> props,
        ServiceBusReceivedMessage message)
    {
        // Count non-framework keys to avoid allocation when all properties are framework-internal
        var userKeyCount = 0;
        foreach (var key in props.Keys)
        {
            if (!key.StartsWith("x-mocha-", StringComparison.Ordinal))
            {
                userKeyCount++;
            }
        }

        var nativePropertyCount = CountNativeProperties(message);
        if (userKeyCount == 0 && nativePropertyCount == 0)
        {
            return Headers.Empty();
        }

        var result = new Headers(userKeyCount + nativePropertyCount);
        foreach (var (key, value) in props)
        {
            if (key.StartsWith("x-mocha-", StringComparison.Ordinal))
            {
                continue;
            }

            // Date/time headers are normalized to DateTimeOffset and compared by instant/UTC.
            result.Set(key, value is DateTime dateTime ? new DateTimeOffset(dateTime) : value);
        }

        SetIfPresent(result, AzureServiceBusMessageHeaders.SessionId, message.SessionId);
        SetIfPresent(result, AzureServiceBusMessageHeaders.PartitionKey, message.PartitionKey);
        SetIfPresent(result, AzureServiceBusMessageHeaders.ReplyToSessionId, message.ReplyToSessionId);
        SetIfPresent(result, AzureServiceBusMessageHeaders.To, message.To);

        return result;
    }

    private static int CountNativeProperties(ServiceBusReceivedMessage message)
    {
        var count = 0;
        count += message.SessionId is null ? 0 : 1;
        count += message.PartitionKey is null ? 0 : 1;
        count += message.ReplyToSessionId is null ? 0 : 1;
        count += message.To is null ? 0 : 1;
        return count;
    }

    private static void SetIfPresent(Headers headers, string key, string? value)
    {
        if (value is not null)
        {
            headers.Set(key, value);
        }
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly AzureServiceBusMessageEnvelopeParser Instance = new();
}

/// <summary>
/// Extension methods for reading typed values from AMQP application properties.
/// </summary>
internal static class AzureServiceBusApplicationPropertyExtensions
{
    /// <summary>
    /// Extracts a string value from the AMQP application properties dictionary.
    /// </summary>
    /// <param name="props">The application properties dictionary.</param>
    /// <param name="key">The property key to read.</param>
    /// <returns>The string value, or <c>null</c> if the key is absent or not a string.</returns>
    public static string? GetString(this IDictionary<string, object?> props, string key)
    {
        if (props.TryGetValue(key, out var value) && value is string str)
        {
            return str;
        }

        return null;
    }
}
