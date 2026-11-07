namespace Mocha.Transport.Nats;

internal static class ThrowHelper
{
    // Connection
    public static Exception InvalidServerUrl(string url)
        => new InvalidOperationException($"'{url}' is not a valid NATS server URL.");

    // Descriptors
    public static Exception BackoffRequiresDelay(string paramName)
        => new ArgumentException("At least one delay is required.", paramName);

    public static Exception EndpointAddressNotAbsolute(string paramName)
        => new ArgumentException("The endpoint address must be an absolute URI.", paramName);

    public static Exception EndpointAddressNotASubject(Uri address, string paramName)
        => new ArgumentException(
            $"'{address}' does not resolve to a NATS subject. Use 'nats:s/<subject>' or "
            + "'subject:<subject>'.",
            paramName);

    // Endpoints
    public static Exception SubjectRequired()
        => new InvalidOperationException("Subject is required.");

    public static Exception SubjectNotFound(string? subject)
        => new InvalidOperationException($"Subject '{subject}' not found.");

    public static Exception EnvelopeNotSet()
        => new InvalidOperationException("Envelope is not set.");

    public static Exception ReplyDestinationNotSet()
        => new InvalidOperationException("Destination address is not set on the reply.");

    public static Exception ConsumerNameRequired()
        => new InvalidOperationException("Consumer name is required.");

    public static Exception ConsumerNotFound(string? consumerName)
        => new InvalidOperationException($"Consumer '{consumerName}' not found.");

    public static Exception ReplyEndpointHasNoSubject()
        => new InvalidOperationException("The reply endpoint has no subject.");

    public static Exception ReplySubjectNotFound(string subject)
        => new InvalidOperationException($"Reply subject '{subject}' not found.");

    // Dispatch
    public static Exception SchedulingNotEnabled(DateTimeOffset scheduledTime, string subject)
        => new InvalidOperationException(
            $"A message scheduled for {scheduledTime:O} was dispatched to subject "
            + $"'{subject}', but scheduling is not enabled on this transport. Call "
            + "EnableScheduling() so the stream allows schedules and captures the scheduling "
            + "subject.");

    public static Exception ServerVersionRequired(string feature, string minimumVersion)
        => new InvalidOperationException(
            $"This message requires {feature}, which needs NATS server {minimumVersion} or later.");

    public static Exception PublishFailed(string subject, Exception innerException)
        => new InvalidOperationException(
            $"Publishing to subject '{subject}' failed. A JetStream publish waits for an "
            + "acknowledgement from the stream capturing the subject, so this also happens when "
            + "no stream captures it or the server has JetStream disabled.",
            innerException);

    public static Exception PublishRejected(string subject, string reason)
        => new InvalidOperationException(
            $"The stream rejected a message published to subject '{subject}': {reason}.");

    public static Exception InvalidHeaderKey(string key)
        => new InvalidOperationException(
            $"Header '{key}' cannot be sent over NATS. Header keys cannot contain ':', "
            + "whitespace or control characters.");

    // Routing
    public static Exception UnsupportedRouteKind(OutboundRouteKind kind)
        => new ArgumentOutOfRangeException(nameof(kind), kind, null);

    // Scheduling
    public static Exception ScheduledStoreRequiresNatsEndpoint()
        => new InvalidOperationException(
            "The NATS scheduled message store requires a NATS dispatch endpoint.");

    public static Exception ScheduledTimeNotSet()
        => new InvalidOperationException("Scheduled time is not set on the envelope.");

    // Topology
    public static Exception TemporaryEndpointConsumerDeclaredPersistent(string? endpointName, string consumerName)
        => new InvalidOperationException(
            $"Endpoint '{endpointName}' is marked Temporary(), but consumer '{consumerName}' was already "
            + "declared without an inactive threshold. Remove the consumer declaration or Temporary() "
            + "from the endpoint.");

    public static Exception StreamSubjectsOverlap(string streamName, string detail, Exception innerException)
        => new InvalidOperationException(
            $"Stream '{streamName}' cannot be provisioned because its subjects overlap a stream "
            + $"that already exists: {detail}. JetStream requires every stream's subjects to be "
            + "disjoint. Either remove the overlapping subject from this declaration, delete the "
            + "stream that owns it, or drop the declaration and let the transport bind to the "
            + "existing stream.",
            innerException);

    public static Exception StreamRequiresServerVersion(
        string streamName,
        string feature,
        string minimumVersion,
        Version? serverVersion)
        => new InvalidOperationException(
            $"Stream '{streamName}' enables {feature}, which requires NATS server {minimumVersion} "
            + $"or later, but the server reports {serverVersion}.");

    public static Exception InvalidConsumerName(string name)
        => new InvalidOperationException(
            $"'{name}' is not a valid JetStream consumer name. Durable names cannot contain "
            + "'.', '*', '>', whitespace or path separators.");

    public static Exception ConsumerStreamNotResolved(string consumerName)
        => new InvalidOperationException(
            $"The stream for consumer '{consumerName}' has not been resolved. Either declare the stream "
            + "explicitly or ensure a stream capturing its subjects exists.");

    public static Exception StreamNameRequired()
        => new InvalidOperationException("Stream name is required.");

    public static Exception InvalidStreamName(string name)
        => new InvalidOperationException(
            $"'{name}' is not a valid JetStream stream name. Stream names cannot contain "
            + "'.', '*', '>', whitespace or path separators.");

    public static Exception InvalidSubject(string subject)
        => new InvalidOperationException($"'{subject}' is not a valid NATS subject.");

    public static Exception ConsumerHasNoSubjects(string consumerName)
        => new InvalidOperationException(
            $"Consumer '{consumerName}' has no subjects, so its stream cannot be resolved. "
            + "Declare the stream explicitly with FromStream.");

    public static Exception ConsumerSpansStreams(string consumerName, string first, string second)
        => new InvalidOperationException(
            $"Consumer '{consumerName}' subscribes to subjects captured by different "
            + $"streams ('{first}' and '{second}'). A consumer can only read from a "
            + "single stream, so split the handler or declare the stream with FromStream.");

    public static Exception NoStreamCapturesSubject(string subject, string context)
        => new InvalidOperationException($"No stream captures subject '{subject}', {context}.");

    public static Exception SubjectCapturedByManyStreams(string subject, IReadOnlyList<string> streams, string context)
        => new InvalidOperationException(
            $"Subject '{subject}' is captured by {streams.Count} streams "
            + $"({string.Join(", ", streams)}), {context}.");
}
