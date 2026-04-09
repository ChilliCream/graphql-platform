namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Header keys used for Azure Service Bus application properties.
/// </summary>
internal static class AzureServiceBusMessageHeaders
{
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
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public const string FaultAddress = "x-mocha-fault-address";

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public const string MessageType = "x-mocha-message-type";

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public const string EnclosedMessageTypes = "x-mocha-enclosed-message-types";

    /// <summary>
    /// Header key for the timestamp when the message was sent, stored as Unix milliseconds.
    /// </summary>
    public const string SentAt = "x-mocha-sent-at";
}
