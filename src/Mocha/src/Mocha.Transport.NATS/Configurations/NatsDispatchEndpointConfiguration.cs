namespace Mocha.Transport.NATS;

/// <summary>
/// Configuration for a NATS dispatch endpoint, specifying the target subject for outbound messages.
/// </summary>
public sealed class NatsDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target NATS subject name for message dispatch.
    /// </summary>
    public string? SubjectName { get; set; }
}
