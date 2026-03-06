using System.Diagnostics;

namespace Mocha;

internal static class SemanticConventions
{
    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the messaging operation name.
    /// </summary>
    public const string OperationName = "messaging.operation.name";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the messaging system identifier.
    /// </summary>
    public const string MessagingSystem = "messaging.system";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the message type.
    /// </summary>
    public const string MessagingType = "messaging.message.type";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the messaging operation type (send, receive, process, settle).
    /// </summary>
    public const string MessagingOperationType = "messaging.operation.type";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the destination address of the message.
    /// </summary>
    public const string MessagingDestinationAddress = "messaging.destination.address";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the name of the consumer handling the message.
    /// </summary>
    public const string MessagingHandlerName = "messaging.handler.name";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key indicating whether the destination is temporary.
    /// </summary>
    public const string MessagingDestinationTemporary = "messaging.destination.temporary";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the messaging instance identifier.
    /// </summary>
    public const string MessagingInstanceId = "messaging.instance.id";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the conversation identifier that groups related messages.
    /// </summary>
    public const string MessagingMessageConversationId = "messaging.message.conversation_id";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the unique message identifier.
    /// </summary>
    public const string MessagingMessageId = "messaging.message.id";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the message body size in bytes.
    /// </summary>
    public const string MessageBodySize = "messaging.message.body.size";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the queue identifier.
    /// </summary>
    public const string QueueId = "queue.id";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the queue name.
    /// </summary>
    public const string QueueName = "queue.name";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the queue type.
    /// </summary>
    public const string QueueType = "queue.type";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the queue kind (main, reply, or fault).
    /// </summary>
    public const string QueueKind = "queue.kind";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key indicating whether the queue is temporary.
    /// </summary>
    public const string QueueTemporary = "queue.temporary";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the topic identifier.
    /// </summary>
    public const string TopicId = "topic.id";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the topic name.
    /// </summary>
    public const string TopicName = "topic.name";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the topic type.
    /// </summary>
    public const string TopicType = "topic.type";

    /// <summary>
    /// Well-known values for the <see cref="QueueKind"/> attribute, classifying a queue by its role in the messaging topology.
    /// </summary>
    public static class QueueKinds
    {
        /// <summary>
        /// Identifies the primary processing queue for an endpoint.
        /// </summary>
        public const string Main = "main";

        /// <summary>
        /// Identifies a reply queue used for request-reply message exchanges.
        /// </summary>
        public const string Reply = "reply";

        /// <summary>
        /// Identifies a fault (dead-letter) queue that receives messages that could not be processed successfully.
        /// </summary>
        public const string Fault = "fault";
    }

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the destination subscription name.
    /// </summary>
    public const string MessagingDestinationSubscriptionName = "messaging.destination.subscription.name";

    /// <summary>
    /// OpenTelemetry semantic convention attribute key for the consumer group name.
    /// </summary>
    public const string MessagingConsumerGroupName = "messaging.consumer.group.name";

    /// <summary>
    /// Sets the <see cref="OperationName"/> tag on the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The operation name value to record.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetOperationName(this Activity activity, string value)
    {
        activity.SetTag(OperationName, value);
        return activity;
    }

    /// <summary>
    /// Applies default messaging enrichment tags to the activity.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity EnrichMessageDefault(this Activity activity) => activity.SetMessagingSystem();

    /// <summary>
    /// Sets the <see cref="MessagingSystem"/> tag on the activity to identify the underlying messaging system.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetMessagingSystem(this Activity activity)
    {
        // activity.SetTag(MessagingSystem, "postgresql");
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingOperationType"/> tag on the activity to classify the operation (send, receive, process, or settle).
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The messaging operation type to record.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetOperationType(this Activity activity, MessagingOperationType value)
    {
        activity.SetTag(MessagingOperationType, value.ToTypeString());
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingHandlerName"/> tag on the activity to identify which consumer handled the message.
    /// </summary>
    /// <remarks>
    /// If <paramref name="value"/> is <see langword="null"/>, the activity is returned unchanged.
    /// </remarks>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The consumer name, or <see langword="null"/> to skip tagging.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetConsumerName(this Activity activity, string? value)
    {
        if (value is null)
        {
            return activity;
        }

        activity.SetTag(MessagingHandlerName, value);

        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingDestinationAddress"/> tag on the activity to record the destination endpoint address.
    /// </summary>
    /// <remarks>
    /// If <paramref name="value"/> is <see langword="null"/>, the activity is returned unchanged.
    /// </remarks>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The destination URI, or <see langword="null"/> to skip tagging.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetDestinationAddress(this Activity activity, Uri? value)
    {
        if (value is null)
        {
            return activity;
        }

        activity.SetTag(MessagingDestinationAddress, value);
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingDestinationTemporary"/> tag on the activity to indicate whether the destination is ephemeral.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value"><see langword="true"/> if the destination is temporary; otherwise, <see langword="false"/>.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetDestinationTemporary(this Activity activity, bool value)
    {
        activity.SetTag(MessagingDestinationTemporary, value);
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingInstanceId"/> tag on the activity to record the messaging instance identifier.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The instance identifier to record.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetInstanceId(this Activity activity, Guid value)
    {
        activity.SetTag(MessagingInstanceId, value.ToString());
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingMessageConversationId"/> tag on the activity to correlate related messages within a conversation.
    /// </summary>
    /// <remarks>
    /// If <paramref name="value"/> is <see langword="null"/>, the activity is returned unchanged.
    /// </remarks>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The conversation identifier, or <see langword="null"/> to skip tagging.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetConversationId(this Activity activity, string? value)
    {
        if (value is null)
        {
            return activity;
        }

        activity.SetTag(MessagingMessageConversationId, value);
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessagingMessageId"/> tag on the activity to record the unique message identifier.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The message identifier to record.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetMessageId(this Activity activity, string value)
    {
        activity.SetTag(MessagingMessageId, value);
        return activity;
    }

    /// <summary>
    /// Sets the <see cref="MessageBodySize"/> tag on the activity to record the message body size in bytes.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="value">The body size in bytes.</param>
    /// <returns>The same <see cref="Activity"/> instance for fluent chaining.</returns>
    public static Activity SetBodySize(this Activity activity, long value)
    {
        activity.SetTag(MessageBodySize, value);
        return activity;
    }
}
