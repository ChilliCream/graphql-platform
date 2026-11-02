namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL queue in the messaging topology.
/// </summary>
public sealed class PostgresQueueConfiguration : TopologyConfiguration<PostgresMessagingTopology>
{
    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this queue should be auto-deleted.
    /// When true, the queue is automatically deleted when no longer in use.
    /// Default is false.
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets whether this queue should be auto-provisioned.
    /// When true, the queue will be created in PostgreSQL during topology provisioning.
    /// Default is true.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
