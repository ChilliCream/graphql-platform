using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocha.Middlewares;
using NATS.Client.Core;

namespace Mocha.Transport.Nats.Tests.Helpers;

/// <summary>
/// Projects NATS headers and a <see cref="MessageEnvelope"/> into a stable JSON view for snapshot
/// assertions.
/// </summary>
internal static class NatsEnvelopeSnapshot
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Renders the headers as they travel on the wire, ordered so the output is deterministic.
    /// </summary>
    public static string Create(NatsHeaders headers)
    {
        var projection = headers
            .OrderBy(h => h.Key, StringComparer.Ordinal)
            .ToDictionary(
                h => h.Key,
                h => h.Value.Count == 1 ? h.Value[0] : string.Join(" | ", h.Value.ToArray()),
                StringComparer.Ordinal);

        return JsonSerializer.Serialize(projection, s_jsonOptions);
    }

    /// <summary>
    /// Renders the envelope fields the transport is responsible for carrying.
    /// </summary>
    public static string Create(MessageEnvelope envelope)
    {
        var projection = new EnvelopeSnapshot(
            envelope.MessageId,
            envelope.CorrelationId,
            envelope.ConversationId,
            envelope.CausationId,
            envelope.SourceAddress,
            envelope.DestinationAddress,
            envelope.ResponseAddress,
            envelope.FaultAddress,
            envelope.MessageType,
            envelope.ContentType,
            envelope.SentAt,
            envelope.DeliverBy,
            envelope.ScheduledTime,
            envelope.DeliveryCount,
            envelope.EnclosedMessageTypes is { } enclosed ? [.. enclosed] : null,
            envelope.Headers?
                .OrderBy(h => h.Key, StringComparer.Ordinal)
                .ToDictionary(h => h.Key, Describe, StringComparer.Ordinal),
            Encoding.UTF8.GetString(envelope.Body.Span));

        return JsonSerializer.Serialize(projection, s_jsonOptions);
    }

    private static string? Describe(HeaderValue header) => header.Value switch
    {
        null => null,
        string text => text,
        string[] values => string.Join(" | ", values),
        var value => value.ToString()
    };

    private sealed record EnvelopeSnapshot(
        string? MessageId,
        string? CorrelationId,
        string? ConversationId,
        string? CausationId,
        string? SourceAddress,
        string? DestinationAddress,
        string? ResponseAddress,
        string? FaultAddress,
        string? MessageType,
        string? ContentType,
        DateTimeOffset? SentAt,
        DateTimeOffset? DeliverBy,
        DateTimeOffset? ScheduledTime,
        int? DeliveryCount,
        string[]? EnclosedMessageTypes,
        Dictionary<string, string?>? Headers,
        string Body);
}
