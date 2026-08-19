namespace Mocha.Transport.Nats;

/// <summary>
/// Represents a subject a dispatch endpoint publishes to.
/// </summary>
/// <remarks>
/// Subjects are not provisioned in their own right; they exist because a stream's subject filter
/// captures them, which is why this resource does not implement <see cref="INatsResource"/>.
/// </remarks>
public sealed class NatsSubject : TopologyResource<NatsSubjectConfiguration>
{
    /// <summary>
    /// Gets the subject, for example <c>order-service.order-created</c>.
    /// </summary>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// Gets the name of the stream expected to capture this subject.
    /// </summary>
    public string? StreamName { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this subject is used over core NATS rather than JetStream.
    /// </summary>
    public bool IsCore { get; private set; }

    /// <inheritdoc />
    protected override void OnInitialize(NatsSubjectConfiguration configuration)
    {
        Subject = configuration.Subject ?? throw new InvalidOperationException("Subject is required.");

        if (!NatsNaming.IsValidSubject(Subject))
        {
            throw new InvalidOperationException($"'{Subject}' is not a valid NATS subject.");
        }

        StreamName = configuration.StreamName;
        IsCore = configuration.IsCore;
    }

    /// <inheritdoc />
    protected override void OnComplete(NatsSubjectConfiguration configuration)
    {
        Address = NatsAddress.ForSubject(Topology.Address, StreamName ?? "_", Subject);
    }

    /// <summary>
    /// Binds this subject to the stream that captures it.
    /// </summary>
    /// <param name="streamName">The resolved stream name.</param>
    public void BindToStream(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        StreamName = streamName;
        Address = NatsAddress.ForSubject(Topology.Address, streamName, Subject);
    }
}
