namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ satellite queue (error or skipped message queue) attached to a receive endpoint.
/// </summary>
public sealed class RabbitMQSatelliteConfiguration
{
    /// <summary>
    /// Gets or sets the verbatim queue name for this satellite.
    /// When set, the satellite queue uses this name exactly as provided; otherwise the name is derived by convention.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this satellite queue is disabled.
    /// When true, no satellite queue is created or used for this endpoint.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets whether this satellite queue is automatically provisioned.
    /// When unset, the satellite inherits the resolved value of the endpoint's queue; when set,
    /// the satellite overrides that value independently.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
