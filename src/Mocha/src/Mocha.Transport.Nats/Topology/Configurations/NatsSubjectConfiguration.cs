namespace Mocha.Transport.Nats;

/// <summary>
/// Configuration for a subject a dispatch endpoint publishes to.
/// </summary>
public sealed class NatsSubjectConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the subject, for example <c>order-service.order-created</c>.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the name of the stream expected to capture this subject.
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this subject is used over core NATS rather than
    /// JetStream, as reply inboxes are. Core subjects are excluded from stream subject filters and
    /// from start-up subject verification.
    /// </summary>
    // Capturing an ephemeral reply inbox in a stream would persist every response for the stream's
    // whole retention period.
    public bool IsCore { get; set; }
}
