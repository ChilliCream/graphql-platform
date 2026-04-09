namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Header keys used for Event Hub message application properties.
/// </summary>
internal static class EventHubMessageHeaders
{
    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public const string ConversationId = "x-conversation-id";

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public const string CausationId = "x-causation-id";

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public const string SourceAddress = "x-source-address";

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public const string DestinationAddress = "x-destination-address";

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public const string FaultAddress = "x-fault-address";

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public const string EnclosedMessageTypes = "x-enclosed-message-types";

    /// <summary>
    /// Header key for the timestamp when the message was sent, stored as Unix time in milliseconds.
    /// </summary>
    public const string SentAt = "x-sent-at";

    private static readonly HashSet<string> s_wellKnown =
    [
        ConversationId,
        CausationId,
        SourceAddress,
        DestinationAddress,
        FaultAddress,
        EnclosedMessageTypes,
        SentAt
    ];

    /// <summary>
    /// Determines whether the specified header key is a well-known Mocha header.
    /// </summary>
    /// <param name="key">The header key to check.</param>
    /// <returns><c>true</c> if the key is a well-known header; otherwise, <c>false</c>.</returns>
    public static bool IsWellKnown(string key) => s_wellKnown.Contains(key);
}
