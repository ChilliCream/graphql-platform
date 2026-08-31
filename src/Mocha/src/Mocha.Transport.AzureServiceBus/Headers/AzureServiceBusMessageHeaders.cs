namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Header keys used for Azure Service Bus application properties.
/// </summary>
public static class AzureServiceBusMessageHeaders
{
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
    /// Header key for the native Azure Service Bus <c>To</c> property.
    /// </summary>
    public const string To = "x-to";

    internal static bool IsFrameworkHeader(string key)
        => MessageHeaders.Transport.IsDefined(key)
            || key is SentAt
            or SessionId
            or PartitionKey
            or ReplyToSessionId
            or To;
}
