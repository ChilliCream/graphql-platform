namespace Mocha.Transport.Postgres;

/// <summary>
/// Provides read-only access to the resolved PostgreSQL schema and table names.
/// </summary>
public interface IReadOnlyPostgresSchemaOptions
{
    /// <summary>
    /// Gets the PostgreSQL schema name.
    /// </summary>
    string Schema { get; }

    /// <summary>
    /// Gets the table name prefix.
    /// </summary>
    string TablePrefix { get; }

    /// <summary>
    /// Gets the fully qualified topic table name.
    /// </summary>
    string TopicTable { get; }

    /// <summary>
    /// Gets the fully qualified queue table name.
    /// </summary>
    string QueueTable { get; }

    /// <summary>
    /// Gets the fully qualified queue subscription table name.
    /// </summary>
    string QueueSubscriptionTable { get; }

    /// <summary>
    /// Gets the fully qualified message table name.
    /// </summary>
    string MessageTable { get; }

    /// <summary>
    /// Gets the fully qualified consumers table name.
    /// </summary>
    string ConsumersTable { get; }

    /// <summary>
    /// Gets the fully qualified migrations table name.
    /// </summary>
    string MigrationsTable { get; }

    /// <summary>
    /// Gets the fully qualified topology sequence name.
    /// </summary>
    string TopologySequence { get; }

    /// <summary>
    /// Gets the PostgreSQL LISTEN/NOTIFY channel name.
    /// </summary>
    string NotificationChannel { get; }
}

/// <summary>
/// Configures the PostgreSQL schema and table naming used by the transport.
/// Allows customization of the database schema and table name prefix to support
/// multi-tenant scenarios or coexistence with other applications.
/// </summary>
public sealed class PostgresSchemaOptions : IReadOnlyPostgresSchemaOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL schema name. Defaults to <c>"public"</c>.
    /// </summary>
    public string Schema { get; set; } = "public";

    /// <summary>
    /// Gets or sets the table name prefix. Defaults to <c>"mocha_"</c>.
    /// </summary>
    public string TablePrefix { get; set; } = "mocha_";

    /// <inheritdoc />
    public string TopicTable => Qualify("topic");

    /// <inheritdoc />
    public string QueueTable => Qualify("queue");

    /// <inheritdoc />
    public string QueueSubscriptionTable => Qualify("queue_subscription");

    /// <inheritdoc />
    public string MessageTable => Qualify("message");

    /// <inheritdoc />
    public string ConsumersTable => Qualify("consumers");

    /// <inheritdoc />
    public string MigrationsTable => Qualify("migrations");

    /// <inheritdoc />
    public string TopologySequence => Qualify("topology_seq");

    /// <inheritdoc />
    public string NotificationChannel => TablePrefix + "queue_changed";

    private string Qualify(string suffix) => Schema + "." + TablePrefix + suffix;
}
