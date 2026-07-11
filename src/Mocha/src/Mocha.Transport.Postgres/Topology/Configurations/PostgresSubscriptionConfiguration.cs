namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL subscription (topic-to-queue binding) in the messaging topology.
/// </summary>
public sealed class PostgresSubscriptionConfiguration : TopologyConfiguration<PostgresMessagingTopology>
{
    /// <summary>
    /// Gets or sets the source topic name.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the destination queue name.
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets whether this subscription should be auto-provisioned.
    /// When true, the subscription will be created in PostgreSQL during topology provisioning.
    /// Default is true.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
