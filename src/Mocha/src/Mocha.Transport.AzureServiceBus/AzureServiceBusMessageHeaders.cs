namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Header keys used for Azure Service Bus application properties.
/// </summary>
public static class AzureServiceBusMessageHeaders
{
    /// <summary>
    /// Header key for the conversation identifier that correlates a group of causally related messages.
    /// </summary>
    public const string ConversationId = MessageHeaders.Transport.ConversationId;

    /// <summary>
    /// Header key for the causation identifier linking a message to the command or event that triggered it.
    /// </summary>
    public const string CausationId = MessageHeaders.Transport.CausationId;

    /// <summary>
    /// Header key for the originating endpoint address of the message.
    /// </summary>
    public const string SourceAddress = MessageHeaders.Transport.SourceAddress;

    /// <summary>
    /// Header key for the intended destination endpoint address of the message.
    /// </summary>
    public const string DestinationAddress = MessageHeaders.Transport.DestinationAddress;

    /// <summary>
    /// Header key for the endpoint address where fault messages should be sent on processing failure.
    /// </summary>
    public const string FaultAddress = MessageHeaders.Transport.FaultAddress;

    /// <summary>
    /// Header key for the fully qualified type name of the message payload.
    /// </summary>
    public const string MessageType = MessageHeaders.Transport.MessageType;

    /// <summary>
    /// Header key for the list of message type names enclosed in the envelope, used for polymorphic deserialization.
    /// </summary>
    public const string EnclosedMessageTypes = MessageHeaders.Transport.EnclosedMessageTypes;

    /// <summary>
    /// Header key for the timestamp when the message was sent, stored as Unix milliseconds.
    /// </summary>
    public const string SentAt = "x-sent-at";

    /// <summary>
    /// Header key for the Azure Service Bus <c>SessionId</c> property, used for session-aware queues
    /// and subscriptions.
    /// </summary>
    public const string SessionId = "x-session-id";

    /// <summary>
    /// Header key for the Azure Service Bus <c>PartitionKey</c> property, used for partitioned entities.
    /// </summary>
    public const string PartitionKey = "x-partition-key";

    /// <summary>
    /// Header key for the Azure Service Bus <c>ReplyToSessionId</c> property, used for multiplexed
    /// request/reply over session-aware reply queues.
    /// </summary>
    public const string ReplyToSessionId = "x-reply-to-session-id";

    /// <summary>
    /// Header key for the Azure Service Bus <c>To</c> property, used for autoforward chaining.
    /// </summary>
    public const string To = "x-to";

    internal static bool IsFrameworkHeader(string key)
        => key is ConversationId
            or CausationId
            or SourceAddress
            or DestinationAddress
            or FaultAddress
            or MessageType
            or EnclosedMessageTypes
            or SentAt
            or SessionId
            or PartitionKey
            or ReplyToSessionId
            or To;
}
