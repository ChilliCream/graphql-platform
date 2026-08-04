namespace Mocha.Transport.Postgres;

/// <summary>
/// Default options for queues created by topology conventions.
/// </summary>
public sealed class PostgresDefaultQueueOptions
{
    /// <summary>
    /// Gets or sets whether queues are auto-deleted by default.
    /// Default is null (uses the queue default of false).
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets whether queues are auto-provisioned by default.
    /// Default is null (uses the queue default of true).
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Applies these defaults to a queue configuration, without overriding explicitly set values.
    /// Temporary queues (e.g. reply queues) are skipped for auto-delete defaults since they
    /// already have their own lifecycle management.
    /// </summary>
    internal void ApplyTo(PostgresQueueConfiguration configuration)
    {
        configuration.AutoProvision ??= AutoProvision;
        configuration.AutoDelete ??= AutoDelete;
    }
}
