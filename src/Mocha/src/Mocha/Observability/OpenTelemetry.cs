using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mocha;

internal static class OpenTelemetry
{
    public static readonly ActivitySource Source = new("Mocha");
    public static readonly Meter Meter = new("Mocha");

    public static IHeaders WithActivity(this IHeaders headers)
    {
        if (Activity.Current is not { } activity)
        {
            return headers;
        }

        headers.TryAdd(MessageHeaders.Traceparent, activity.Id);

        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers.TryAdd(MessageHeaders.Tracestate, activity.TraceStateString);
        }

        return headers;
    }

    // TODO this needs to be adjusted - this is still leagcy
    public static class Meters
    {
        private static readonly Histogram<double> s_operationDuration =
            Meter.CreateHistogram<double>(
                "messaging.client.operation.duration",
                "s",
                "The duration of a messaging operation.");

        private static readonly Counter<long> s_sendMessages =
            Meter.CreateCounter<long>("messaging.client.sent.messages", "{message}", "The number of sent messages.");

        private static readonly Counter<long> s_consumedMessages =
            Meter.CreateCounter<long>(
                "messaging.client.consumed.messages",
                "{message}",
                "The number of consumed messages.");

        private static readonly Histogram<double> s_messageProcessDuration =
            Meter.CreateHistogram<double>("messaging.process.duration", "s", "The duration of processing a message.");

        private static readonly Gauge<long> s_queueLength =
            Meter.CreateGauge<long>("messaging.queue.length", "{message}", "The number of messages in the queue.");

        private static readonly Gauge<double> s_queueMessageOldestAge =
            Meter.CreateGauge<double>(
                "messaging.queue.message.oldest_age",
                "s",
                "The age of the oldest message in the queue.");

        private static readonly Gauge<double> s_queueMessageLatestAge =
            Meter.CreateGauge<double>(
                "messaging.queue.message.latest_age",
                "s",
                "The age of the latest message in the queue.");

        private static readonly Gauge<long> s_queueRefreshTimestamp =
            Meter.CreateGauge<long>(
                "messaging.queue.refresh.timestamp",
                "s",
                "The timestamp of the last queue refresh.");

        private static readonly Gauge<long> s_topicRefreshTimestamp =
            Meter.CreateGauge<long>(
                "messaging.topic.refresh.timestamp",
                "s",
                "The timestamp of the last topic refresh.");

        private static readonly Gauge<long> s_topicConsumerCount =
            Meter.CreateGauge<long>(
                "messaging.topic.consumer.count",
                "{consumer}",
                "The number of consumers in the topic.");

        public static void RecordQueueLength(
            long id,
            string name,
            long length,
            string state,
            string kind,
            bool isTemporary)
        {
            s_queueLength.Record(
                length,
                new(SemanticConventions.QueueId, id),
                new(SemanticConventions.QueueName, name),
                new("state", state),
                new(SemanticConventions.QueueKind, kind),
                new(SemanticConventions.QueueTemporary, isTemporary),
                new(SemanticConventions.QueueType, "postgres"));
        }

        public static void RecordQueueMessageOldestAge(
            long id,
            string name,
            double age,
            string state,
            string kind,
            bool isTemporary)
        {
            s_queueMessageOldestAge.Record(
                age,
                new(SemanticConventions.QueueId, id),
                new(SemanticConventions.QueueName, name),
                new("state", state),
                new(SemanticConventions.QueueKind, kind),
                new(SemanticConventions.QueueTemporary, isTemporary),
                new(SemanticConventions.QueueType, "postgres"));
        }

        public static void RecordQueueMessageLatestAge(
            long id,
            string name,
            double age,
            string state,
            string kind,
            bool isTemporary)
        {
            s_queueMessageLatestAge.Record(
                age,
                new(SemanticConventions.QueueId, id),
                new(SemanticConventions.QueueName, name),
                new("state", state),
                new(SemanticConventions.QueueKind, kind),
                new(SemanticConventions.QueueTemporary, isTemporary),
                new(SemanticConventions.QueueType, "postgres"));
        }

        public static void RecordQueueRefreshTimestamp(
            long id,
            string name,
            long timestamp,
            string state,
            string kind,
            bool isTemporary)
        {
            s_queueRefreshTimestamp.Record(
                timestamp,
                new(SemanticConventions.QueueId, id),
                new(SemanticConventions.QueueName, name),
                new("state", state),
                new(SemanticConventions.QueueKind, kind),
                new(SemanticConventions.QueueTemporary, isTemporary),
                new(SemanticConventions.QueueType, "postgres"));
        }

        public static void RecordTopicRefreshTimestamp(long id, string name, long timestamp)
        {
            s_topicRefreshTimestamp.Record(
                timestamp,
                new(SemanticConventions.TopicId, id),
                new(SemanticConventions.TopicName, name),
                new(SemanticConventions.TopicType, "postgres"));
        }

        public static void RecordTopicConsumerCount(long id, string name, long count)
        {
            s_topicConsumerCount.Record(
                count,
                new(SemanticConventions.TopicId, id),
                new(SemanticConventions.TopicName, name),
                new(SemanticConventions.TopicType, "postgres"));
        }

        public static void RecordOperationDuration(
            TimeSpan duration,
            string operationName,
            Uri? destinationName,
            MessagingOperationType messagingOperationType,
            string messageIdentity,
            string messagingSystem)
        {
            s_operationDuration.Record(
                duration.TotalSeconds,
                new(SemanticConventions.OperationName, operationName),
                new(SemanticConventions.MessagingDestinationAddress, destinationName),
                new(SemanticConventions.MessagingOperationType, messagingOperationType.ToTypeString()),
                new(SemanticConventions.MessagingType, messageIdentity),
                new(SemanticConventions.MessagingSystem, messagingSystem));
        }

        public static void RecordSendMessage(
            string operationName,
            Uri? destinationName,
            string messageIdentity,
            string messagingSystem)
        {
            s_sendMessages.Add(
                1,
                new(SemanticConventions.OperationName, operationName),
                new(SemanticConventions.MessagingType, messageIdentity),
                new(SemanticConventions.MessagingDestinationAddress, destinationName),
                new(SemanticConventions.MessagingSystem, messagingSystem));
        }

        public static void RecordConsumeMessage(
            string operationName,
            string destinationName,
            string messageIdentity,
            string messagingSystem,
            string? subscriptionName = null,
            string? consumerGroupName = null)
        {
            s_consumedMessages.Add(
                1,
                new(SemanticConventions.OperationName, operationName),
                new(SemanticConventions.MessagingDestinationAddress, destinationName),
                new(SemanticConventions.MessagingSystem, messagingSystem),
                new(SemanticConventions.MessagingType, messageIdentity),
                new(SemanticConventions.MessagingDestinationSubscriptionName, subscriptionName),
                new(SemanticConventions.MessagingConsumerGroupName, consumerGroupName));
        }

        public static void RecordProcessingDuration(
            TimeSpan duration,
            string operationName,
            string destinationName,
            string messagingSystem,
            string messageIdentity,
            string? subscriptionName = null,
            string? consumerGroupName = null)
        {
            s_messageProcessDuration.Record(
                duration.TotalSeconds,
                new(SemanticConventions.OperationName, operationName),
                new(SemanticConventions.MessagingDestinationAddress, destinationName),
                new(SemanticConventions.MessagingSystem, messagingSystem),
                new(SemanticConventions.MessagingType, messageIdentity),
                new(SemanticConventions.MessagingDestinationSubscriptionName, subscriptionName),
                new(SemanticConventions.MessagingConsumerGroupName, consumerGroupName));
        }
    }
}
