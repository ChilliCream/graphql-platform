namespace Mocha.Transport.Postgres;

/// <summary>
/// Configuration for a PostgreSQL messaging transport.
/// </summary>
public class PostgresTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name.
    /// </summary>
    public const string DefaultName = "postgres";

    /// <summary>
    /// The default URI schema.
    /// </summary>
    public const string DefaultSchema = "postgres";

    public PostgresTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the host for topology address construction.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the port for topology address construction.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the declared topics.
    /// </summary>
    public List<PostgresTopicConfiguration> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the declared queues.
    /// </summary>
    public List<PostgresQueueConfiguration> Queues { get; set; } = [];

    /// <summary>
    /// Gets or sets the declared subscriptions.
    /// </summary>
    public List<PostgresSubscriptionConfiguration> Subscriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether topology resources (queues, topics, subscriptions)
    /// should be automatically provisioned in the database. When <c>null</c>, defaults to <c>true</c>.
    /// Individual resources can override this setting.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to all auto-provisioned queues and topics.
    /// </summary>
    public PostgresBusDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Gets or sets the schema and table naming settings for the transport.
    /// </summary>
    public PostgresSchemaOptions SchemaOptions { get; set; } = new();
}
