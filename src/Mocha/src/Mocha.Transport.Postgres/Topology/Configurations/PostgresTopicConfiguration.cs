namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL topic in the messaging topology.
/// </summary>
public sealed class PostgresTopicConfiguration : TopologyConfiguration<PostgresMessagingTopology>
{
    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this topic should be auto-provisioned.
    /// When true, the topic will be created in PostgreSQL during topology provisioning.
    /// Default is true.
    /// </summary>
    public bool? AutoProvision { get; set; }
}
