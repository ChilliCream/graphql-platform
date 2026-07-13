using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Creates Azure Service Bus messages from Mocha envelopes for immediate and scheduled dispatch.
/// </summary>
internal static class AzureServiceBusMessageFactory
{
    private static readonly TimeSpan s_minimumTimeToLive = TimeSpan.FromMilliseconds(1);

    public static ServiceBusMessage Create(
        MessageEnvelope envelope,
        DateTimeOffset expectedEnqueueTime)
    {
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
            var timeToLive = deliverBy - expectedEnqueueTime;
            message.TimeToLive = timeToLive > TimeSpan.Zero
                ? timeToLive
                : s_minimumTimeToLive;
        }

        ApplyNativeProperties(message, envelope.Headers);
        ApplyEnvelopeProperties(message.ApplicationProperties, envelope);

        return message;
    }

    private static void ApplyNativeProperties(ServiceBusMessage message, IHeaders? headers)
    {
        if (headers is null)
        {
            return;
        }

        string? sessionId = null;
        if (headers.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out var sessionIdValue)
            && sessionIdValue is string sessionIdString)
        {
            sessionId = sessionIdString;
            message.SessionId = sessionIdString;
        }

        if (headers.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out var partitionKeyValue)
            && partitionKeyValue is string partitionKeyString)
        {
            if (sessionId is not null && partitionKeyString != sessionId)
            {
                throw ThrowHelper.PartitionKeyMustEqualSessionId();
            }

            message.PartitionKey = partitionKeyString;
        }
        else if (sessionId is not null)
        {
            message.PartitionKey = sessionId;
        }

        if (headers.TryGetValue(
                AzureServiceBusMessageHeaders.ReplyToSessionId,
                out var replyToSessionIdValue)
            && replyToSessionIdValue is string replyToSessionIdString)
        {
            message.ReplyToSessionId = replyToSessionIdString;
        }

        if (headers.TryGetValue(AzureServiceBusMessageHeaders.To, out var toValue)
            && toValue is string toString)
        {
            message.To = toString;
        }
    }

    private static void ApplyEnvelopeProperties(
        IDictionary<string, object> properties,
        MessageEnvelope envelope)
    {
        if (envelope.ConversationId is not null)
        {
            properties[AzureServiceBusMessageHeaders.ConversationId] = envelope.ConversationId;
        }

        if (envelope.CausationId is not null)
        {
            properties[AzureServiceBusMessageHeaders.CausationId] = envelope.CausationId;
        }

        if (envelope.SourceAddress is not null)
        {
            properties[AzureServiceBusMessageHeaders.SourceAddress] = envelope.SourceAddress;
        }

        if (envelope.DestinationAddress is not null)
        {
            properties[AzureServiceBusMessageHeaders.DestinationAddress] = envelope.DestinationAddress;
        }

        if (envelope.FaultAddress is not null)
        {
            properties[AzureServiceBusMessageHeaders.FaultAddress] = envelope.FaultAddress;
        }

        if (envelope.MessageType is not null)
        {
            properties[AzureServiceBusMessageHeaders.MessageType] = envelope.MessageType;
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosedTypes)
        {
            properties[AzureServiceBusMessageHeaders.EnclosedMessageTypes] =
                enclosedTypes.Length == 1 ? enclosedTypes[0] : string.Join(";", enclosedTypes);
        }

        if (envelope.SentAt is { } sentAt)
        {
            properties[AzureServiceBusMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds();
        }

        if (envelope.Headers is null)
        {
            return;
        }

        foreach (var header in envelope.Headers)
        {
            if (header.Key.StartsWith("x-mocha-", StringComparison.Ordinal))
            {
                continue;
            }

            if (header.Value is DateTimeOffset dateTimeOffset)
            {
                properties[header.Key] = dateTimeOffset.ToUnixTimeMilliseconds();
            }
            else if (header.Value is DateTime dateTime)
            {
                properties[header.Key] = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            else if (header.Value is not null)
            {
                properties[header.Key] = header.Value;
            }
        }
    }
}
