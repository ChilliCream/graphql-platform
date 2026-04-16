using System.Collections.Immutable;
using System.Text;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Header keys used for RabbitMQ message properties.
/// </summary>
internal static class RabbitMQMessageHeaders
{
    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public static readonly ContextDataKey<string> ConversationId = new("x-conversation-id");

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public static readonly ContextDataKey<string> CausationId = new("x-causation-id");

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public static readonly ContextDataKey<string> SourceAddress = new("x-source-address");

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public static readonly ContextDataKey<string> DestinationAddress = new("x-destination-address");

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public static readonly ContextDataKey<string> FaultAddress = new("x-fault-address");

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public static readonly ContextDataKey<string> MessageType = new("x-message-type");

    /// <summary>
    /// Header key for the MIME content type of the serialized message body.
    /// </summary>
    public static readonly ContextDataKey<string> ContentType = new("x-content-type");

    /// <summary>
    /// Header key for the AMQP routing key, used to route messages to the correct exchange binding.
    /// </summary>
    public static readonly ContextDataKey<string> RoutingKey = new("x-routing-key");

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public static readonly ContextDataKey<ImmutableArray<string>> EnclosedMessageTypes = new(
        "x-enclosed-message-types");
}

/// <summary>
/// Extension methods for reading typed values from RabbitMQ message headers.
/// </summary>
internal static class RabbitMQMessageHeaderExtensions
{
    /// <summary>
    /// Extracts a string value from the headers dictionary, decoding from UTF-8 bytes if the raw value is a byte array.
    /// </summary>
    /// <param name="headers">The RabbitMQ message headers dictionary.</param>
    /// <param name="key">The context data key identifying the header to read.</param>
    /// <returns>The decoded string value, or <c>null</c> if the header is absent or cannot be converted.</returns>
    public static string? GetString(this IDictionary<string, object?> headers, ContextDataKey<string> key)
    {
        if (headers.TryGetValue(key.Key, out var value) && value is byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return value?.ToString();
    }

    /// <summary>
    /// Extracts an immutable array of strings from a header value stored as a <c>List&lt;object&gt;</c> of UTF-8 byte arrays.
    /// </summary>
    /// <param name="headers">The RabbitMQ message headers dictionary.</param>
    /// <param name="key">The context data key identifying the header to read.</param>
    /// <returns>An immutable array of decoded strings, or an empty array if the header is absent or not in the expected format.</returns>
    public static ImmutableArray<string> GetStringArray(
        this IDictionary<string, object?> headers,
        ContextDataKey<ImmutableArray<string>> key)
    {
        if (headers.TryGetValue(key.Key, out var value) && value is List<object> listOfObjects)
        {
            var builder = ImmutableArray.CreateBuilder<string>(listOfObjects.Count);
            foreach (var obj in listOfObjects)
            {
                if (obj is byte[] bytes)
                {
                    builder.Add(Encoding.UTF8.GetString(bytes));
                }
            }
            return builder.ToImmutableArray();
        }

        return [];
    }
}
