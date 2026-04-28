using System.Collections.Immutable;
using Azure.Core.Amqp;
using Azure.Messaging.EventHubs;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Parses raw Event Hub <see cref="EventData"/> into a normalized <see cref="MessageEnvelope"/>
/// extracting structured AMQP properties, application properties, and the message body.
/// </summary>
internal sealed class EventHubMessageEnvelopeParser
{
    /// <summary>
    /// Converts an Event Hub event into a <see cref="MessageEnvelope"/> by mapping AMQP structured properties
    /// and application properties to envelope fields.
    /// </summary>
    /// <param name="eventData">The raw Event Hub event data.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(EventData eventData)
    {
        var amqp = eventData.GetRawAmqpMessage();
        var hasAppProps = amqp.HasSection(AmqpMessageSection.ApplicationProperties);
        var appProps = hasAppProps ? amqp.ApplicationProperties : null;

        var envelope = new MessageEnvelope
        {
            // Body: zero-copy from EventBody
            Body = eventData.EventBody.ToMemory(),

            // Structured AMQP properties (no dictionary allocation)
            MessageId = amqp.Properties.MessageId?.ToString(),
            CorrelationId = amqp.Properties.CorrelationId?.ToString(),
            ContentType = amqp.Properties.ContentType,
            MessageType = amqp.Properties.Subject,
            ResponseAddress = amqp.Properties.ReplyTo?.ToString(),

            // ApplicationProperties (overflow headers)
            ConversationId = GetStringOrNull(appProps, EventHubMessageHeaders.ConversationId),
            CausationId = GetStringOrNull(appProps, EventHubMessageHeaders.CausationId),
            SourceAddress = GetStringOrNull(appProps, EventHubMessageHeaders.SourceAddress),
            DestinationAddress = GetStringOrNull(appProps, EventHubMessageHeaders.DestinationAddress),
            FaultAddress = GetStringOrNull(appProps, EventHubMessageHeaders.FaultAddress),

            // SentAt from ApplicationProperties
            SentAt = GetDateTimeOffsetOrNull(appProps, EventHubMessageHeaders.SentAt),

            // EnclosedMessageTypes
            EnclosedMessageTypes = ParseEnclosedMessageTypes(appProps),

            // Event Hubs doesn't track per-message delivery count
            DeliveryCount = 0,

            // Custom headers from remaining ApplicationProperties
            Headers = BuildHeaders(appProps)
        };

        return envelope;
    }

    private static string? GetStringOrNull(IDictionary<string, object?>? appProps, string key)
    {
        if (appProps is not null
            && appProps.TryGetValue(key, out var value)
            && value is string str)
        {
            return str;
        }

        return null;
    }

    private static DateTimeOffset? GetDateTimeOffsetOrNull(IDictionary<string, object?>? appProps, string key)
    {
        if (appProps is not null
            && appProps.TryGetValue(key, out var value))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                long ms => DateTimeOffset.FromUnixTimeMilliseconds(ms),
                _ => null
            };
        }

        return null;
    }

    private static ImmutableArray<string> ParseEnclosedMessageTypes(
        IDictionary<string, object?>? appProps)
    {
        if (appProps is null)
        {
            return [];
        }

        if (appProps.TryGetValue(EventHubMessageHeaders.EnclosedMessageTypes, out var value)
            && value is string typesStr
            && !string.IsNullOrEmpty(typesStr))
        {
            if (!typesStr.Contains(';'))
            {
                return [typesStr];
            }

            return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
        }

        return [];
    }

    private static Headers BuildHeaders(IDictionary<string, object?>? appProps)
    {
        if (appProps is null || appProps.Count == 0)
        {
            return Headers.Empty();
        }

        // Short-circuit: if all app properties are well-known transport headers
        // return empty without allocating a Headers instance.
        var hasCustomHeaders = false;
        foreach (var (key, _) in appProps)
        {
            if (!EventHubMessageHeaders.IsWellKnown(key))
            {
                hasCustomHeaders = true;
                break;
            }
        }

        if (!hasCustomHeaders)
        {
            return Headers.Empty();
        }

        var result = new Headers(appProps.Count);
        foreach (var (key, value) in appProps)
        {
            if (EventHubMessageHeaders.IsWellKnown(key))
            {
                continue;
            }

            result.Set(key, value);
        }

        return result;
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly EventHubMessageEnvelopeParser Instance = new();
}
