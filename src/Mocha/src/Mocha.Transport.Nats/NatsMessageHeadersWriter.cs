using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;
using Mocha.Middlewares;
using NATS.Client.Core;

namespace Mocha.Transport.Nats;

/// <summary>
/// Writes a <see cref="MessageEnvelope"/> into a <see cref="NatsHeaders"/> instance for publishing.
/// </summary>
internal sealed class NatsMessageHeadersWriter
{
    /// <summary>
    /// Shared singleton instance of the writer.
    /// </summary>
    public static readonly NatsMessageHeadersWriter Instance = new();

    /// <summary>
    /// Projects the envelope metadata and user-defined headers onto a fresh header collection.
    /// </summary>
    /// <param name="envelope">The envelope to write.</param>
    /// <returns>A new <see cref="NatsHeaders"/> instance owned by the caller.</returns>
    // NATS.Net 3.0 headers stay mutable after publishing, so an instance is never shared.
    public NatsHeaders Write(MessageEnvelope envelope)
    {
        var headers = new NatsHeaders();

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is null || NatsMessageHeaders.IsReserved(header.Key))
                {
                    continue;
                }

                headers.Add(ValidateKey(header.Key), FormatValues(header.Value));
            }
        }

        Set(headers, NatsMessageHeaders.MessageId, envelope.MessageId);
        Set(headers, NatsMessageHeaders.CorrelationId, envelope.CorrelationId);
        Set(headers, NatsMessageHeaders.ConversationId, envelope.ConversationId);
        Set(headers, NatsMessageHeaders.CausationId, envelope.CausationId);
        Set(headers, NatsMessageHeaders.SourceAddress, envelope.SourceAddress);
        Set(headers, NatsMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        Set(headers, NatsMessageHeaders.ResponseAddress, envelope.ResponseAddress);
        Set(headers, NatsMessageHeaders.FaultAddress, envelope.FaultAddress);
        Set(headers, NatsMessageHeaders.MessageType, envelope.MessageType);
        Set(headers, NatsMessageHeaders.ContentType, envelope.ContentType);
        Set(headers, NatsMessageHeaders.SentAt, Format(envelope.SentAt));
        Set(headers, NatsMessageHeaders.DeliverBy, Format(envelope.DeliverBy));
        Set(headers, NatsMessageHeaders.ScheduledTime, Format(envelope.ScheduledTime));

        if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosedMessageTypes)
        {
            headers.Add(NatsMessageHeaders.EnclosedMessageTypes, new StringValues([.. enclosedMessageTypes]));
        }

        return headers;
    }

    private static void Set(NatsHeaders headers, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            headers.Add(key, Sanitize(value));
        }
    }

    /// <summary>
    /// Returns the header key, rejecting one that cannot be expressed in a NATS header.
    /// </summary>
    /// <param name="key">The header key to validate.</param>
    /// <returns>The key, when it is legal.</returns>
    /// <exception cref="InvalidOperationException">
    /// The key contains a colon, whitespace or a control character.
    /// </exception>
    private static string ValidateKey(string key)
    {
        foreach (var character in key)
        {
            if (character is ':' || char.IsWhiteSpace(character) || char.IsControl(character))
            {
                throw ThrowHelper.InvalidHeaderKey(key);
            }
        }

        return key;
    }

    /// <summary>
    /// Replaces line breaks and control characters so a value is legal in a NATS header.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The value with line breaks and control characters collapsed to spaces.</returns>
    // The NATS wire protocol is line-based and rejects a header value containing CRLF.
    private static string Sanitize(string value)
    {
        var needsSanitizing = false;

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                needsSanitizing = true;
                break;
            }
        }

        if (!needsSanitizing)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return builder.ToString();
    }

    private static string? Format(DateTimeOffset? value)
        => value?.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a user-defined header value, writing a multi-valued header as several values.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The values to write under the header key.</returns>
    private static StringValues FormatValues(object value)
    {
        if (value is string text)
        {
            return Sanitize(text);
        }

        if (value is IEnumerable<string> values)
        {
            var formatted = values.Select(static v => v is null ? null : Sanitize(v)).ToArray();

            return formatted.Length == 1 ? formatted[0] : new StringValues(formatted);
        }

        return Sanitize(Format(value));
    }

    private static string Format(object value) => value switch
    {
        string text => text,
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTime => new DateTimeOffset(dateTime).ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
