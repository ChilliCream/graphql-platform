using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Builds <see cref="ServiceBusMessage"/> instances from <see cref="MessageEnvelope"/> values
/// for both the send-now path and the scheduled-store path. Pure function, no I/O.
/// </summary>
internal static class AzureServiceBusMessageFactory
{
    public static ServiceBusMessage Create(MessageEnvelope envelope)
    {
        // Zero-copy: ReadOnlyMemory<byte> passed directly to ServiceBusMessage constructor
        var message = new ServiceBusMessage(envelope.Body)
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            ContentType = envelope.ContentType,
            Subject = envelope.MessageType,
            ReplyTo = envelope.ResponseAddress
        };

        if (envelope.DeliverBy is { } deliverBy)
        {
            var ttl = deliverBy - DateTimeOffset.UtcNow;
            if (ttl > TimeSpan.Zero)
            {
                message.TimeToLive = ttl;
            }
        }

        string? sessionId = null;
        if (envelope.Headers is { } envelopeHeaders)
        {
            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out var sessionIdValue)
                && sessionIdValue is string sessionIdString)
            {
                sessionId = sessionIdString;
                message.SessionId = sessionIdString;
            }

            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out var partitionKeyValue)
                && partitionKeyValue is string partitionKeyString)
            {
                if (sessionId is not null && partitionKeyString != sessionId)
                {
                    throw new InvalidOperationException(
                        "PartitionKey must equal SessionId when both are set on an Azure Service Bus message.");
                }

                message.PartitionKey = partitionKeyString;
            }
            else if (sessionId is not null)
            {
                // Default PartitionKey to SessionId for partitioned + session-aware entities.
                message.PartitionKey = sessionId;
            }

            if (envelopeHeaders.TryGetValue(
                    AzureServiceBusMessageHeaders.ReplyToSessionId,
                    out var replyToSessionIdValue) && replyToSessionIdValue is string replyToSessionIdString)
            {
                message.ReplyToSessionId = replyToSessionIdString;
            }

            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.To, out var toValue)
                && toValue is string toString)
            {
                message.To = toString;
            }
        }

        var props = message.ApplicationProperties;

        if (envelope.ConversationId is not null)
        {
            props[AzureServiceBusMessageHeaders.ConversationId] = envelope.ConversationId;
        }

        if (envelope.CausationId is not null)
        {
            props[AzureServiceBusMessageHeaders.CausationId] = envelope.CausationId;
        }

        if (envelope.SourceAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.SourceAddress] = envelope.SourceAddress;
        }

        if (envelope.DestinationAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.DestinationAddress] = envelope.DestinationAddress;
        }

        if (envelope.FaultAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.FaultAddress] = envelope.FaultAddress;
        }

        if (envelope.MessageType is not null)
        {
            props[AzureServiceBusMessageHeaders.MessageType] = envelope.MessageType;
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
        {
            props[AzureServiceBusMessageHeaders.EnclosedMessageTypes] =
                types.Length == 1 ? types[0] : string.Join(";", types);
        }

        if (envelope.SentAt is { } sentAt)
        {
            props[AzureServiceBusMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds();
        }

        // User-defined headers. Framework-internal keys in the x-mocha-* namespace are already
        // mapped to native SDK properties above (and stripped on receive), so they must not leak
        // into ApplicationProperties.
        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Key.StartsWith("x-mocha-", StringComparison.Ordinal))
                {
                    continue;
                }

                if (header.Value is DateTimeOffset dto)
                {
                    props[header.Key] = dto.ToUnixTimeMilliseconds();
                }
                else if (header.Value is DateTime dt)
                {
                    props[header.Key] = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                }
                else if (header.Value is not null)
                {
                    props[header.Key] = header.Value;
                }
            }
        }

        return message;
    }
}
