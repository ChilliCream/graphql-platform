using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using NATS.Client.Core;

namespace Mocha.Transport.NATS;

/// <summary>
/// Header key constants for NATS message properties, using the <c>x-mocha-</c> prefix.
/// </summary>
internal static class NatsMessageHeaders
{
    /// <summary>
    /// Header key for the unique message identifier.
    /// </summary>
    public const string MessageId = "x-mocha-message-id";

    /// <summary>
    /// Header key for the correlation identifier linking related messages.
    /// </summary>
    public const string CorrelationId = "x-mocha-correlation-id";

    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public const string ConversationId = "x-mocha-conversation-id";

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public const string CausationId = "x-mocha-causation-id";

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public const string SourceAddress = "x-mocha-source-address";

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public const string DestinationAddress = "x-mocha-destination-address";

    /// <summary>
    /// Header key for the endpoint address where replies should be sent.
    /// </summary>
    public const string ResponseAddress = "x-mocha-response-address";

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public const string FaultAddress = "x-mocha-fault-address";

    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public const string ContentType = "x-mocha-content-type";

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public const string MessageType = "x-mocha-message-type";

    /// <summary>
    /// Header key for the UTC timestamp when the message was sent, in round-trip ("O") format.
    /// </summary>
    public const string SentAt = "x-mocha-sent-at";

    /// <summary>
    /// Header key for the deadline by which the message must be delivered, in round-trip ("O") format.
    /// </summary>
    public const string DeliverBy = "x-mocha-deliver-by";

    /// <summary>
    /// Header key for the semicolon-separated list of enclosed message type URNs.
    /// </summary>
    public const string EnclosedMessageTypes = "x-mocha-enclosed-message-types";
}

/// <summary>
/// Extension methods for reading typed values from <see cref="NatsHeaders"/>.
/// </summary>
internal static class NatsHeaderExtensions
{
    /// <summary>
    /// Extracts a string value from the NATS headers.
    /// </summary>
    /// <param name="headers">The NATS message headers.</param>
    /// <param name="key">The header key to read.</param>
    /// <returns>The string value, or <c>null</c> if the header is absent.</returns>
    public static string? GetString(this NatsHeaders headers, string key)
    {
        if (headers.TryGetValue(key, out var values) && values.Count > 0)
        {
            return values[0];
        }

        return null;
    }

    /// <summary>
    /// Extracts a <see cref="DateTimeOffset"/> value from the NATS headers, parsing with invariant culture
    /// and round-trip format.
    /// </summary>
    /// <param name="headers">The NATS message headers.</param>
    /// <param name="key">The header key to read.</param>
    /// <returns>The parsed <see cref="DateTimeOffset"/>, or <c>null</c> if the header is absent or cannot be parsed.</returns>
    public static DateTimeOffset? GetDateTimeOffset(this NatsHeaders headers, string key)
    {
        var value = headers.GetString(key);
        if (value is not null
            && DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Extracts an immutable array of strings from a semicolon-separated NATS header value.
    /// </summary>
    /// <param name="headers">The NATS message headers.</param>
    /// <param name="key">The header key to read.</param>
    /// <returns>An immutable array of strings, or an empty array if the header is absent.</returns>
    public static ImmutableArray<string> GetStringArray(this NatsHeaders headers, string key)
    {
        var value = headers.GetString(key);
        if (value is null)
        {
            return [];
        }

        var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        return ImmutableCollectionsMarshal.AsImmutableArray(parts);
    }
}
