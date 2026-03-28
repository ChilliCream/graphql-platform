namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS subject topology resource.
/// </summary>
public sealed class NatsSubjectConfiguration : TopologyConfiguration<NatsMessagingTopology>
{
    /// <summary>
    /// Gets or sets the name of the subject.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the stream that captures this subject.
    /// </summary>
    public string? StreamName { get; set; }
}
