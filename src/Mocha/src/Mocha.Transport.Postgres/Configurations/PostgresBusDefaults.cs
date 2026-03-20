namespace Mocha.Transport.Postgres;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned queues and topics
/// when they are created by topology conventions.
/// </summary>
public sealed class PostgresBusDefaults
{
    /// <summary>
    /// Gets or sets the default queue configuration that is applied to all auto-provisioned queues.
    /// Individual queue settings will override these defaults.
    /// </summary>
    public PostgresDefaultQueueOptions Queue { get; set; } = new();

    /// <summary>
    /// Gets or sets the default topic configuration that is applied to all auto-provisioned topics.
    /// Individual topic settings will override these defaults.
    /// </summary>
    public PostgresDefaultTopicOptions Topic { get; set; } = new();

    /// <summary>
    /// Gets or sets the default receive endpoint configuration that is applied to all auto-provisioned endpoints.
    /// Individual endpoint settings will override these defaults.
    /// </summary>
    public PostgresDefaultEndpointOptions Endpoint { get; set; } = new();
}
