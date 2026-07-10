using System.Collections.Immutable;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Represents a queue in the PostgreSQL messaging topology. Queues are the delivery
/// destinations - messages are stored in the database and consumed by receive endpoints.
/// </summary>
public sealed class PostgresQueue : TopologyResource<PostgresQueueConfiguration>, IPostgresResource
{
    private ImmutableArray<PostgresSubscription> _subscriptions = [];

    /// <summary>
    /// Gets the queue name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets whether this queue should be automatically deleted when the consumer disconnects.
    /// </summary>
    public bool? AutoDelete { get; private set; }

    /// <summary>
    /// Gets whether this queue should be auto-provisioned in the database.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the subscriptions targeting this queue.
    /// </summary>
    public IReadOnlyList<PostgresSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(PostgresQueueConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoDelete = configuration.AutoDelete;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(PostgresQueueConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/q/" + Name;
        Address = builder.Uri;
    }

    internal void AddSubscription(PostgresSubscription subscription)
    {
        ImmutableInterlocked.Update(ref _subscriptions, static (s, sub) => s.Add(sub), subscription);
    }

    /// <inheritdoc />
    public Task ProvisionAsync(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        CancellationToken cancellationToken)
        => ProvisionAsync(connectionManager, schemaOptions, consumerManager: null, cancellationToken);

    /// <summary>
    /// Provisions this queue in the PostgreSQL database. When the queue has <see cref="AutoDelete"/>
    /// enabled and a consumer manager is provided, the queue is linked to the consumer so that
    /// CASCADE deletion occurs when the consumer is cleaned up.
    /// </summary>
    internal async Task ProvisionAsync(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        PostgresConsumerManager? consumerManager,
        CancellationToken cancellationToken)
    {
        if (AutoProvision == false)
        {
            return;
        }

        var naming = schemaOptions;
        await using var connection = await connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        if (AutoDelete == true && consumerManager is not null)
        {
            command.CommandText =
                $"""
                INSERT INTO {naming.QueueTable} (name, consumer_id)
                VALUES (@name, @consumerId)
                ON CONFLICT (name)
                DO UPDATE SET updated = NOW(), consumer_id = @consumerId
                RETURNING id
                """;

            command.Parameters.AddWithValue("name", Name);
            command.Parameters.AddWithValue("consumerId", consumerManager.ConsumerId);
        }
        else
        {
            command.CommandText =
                $"""
                INSERT INTO {naming.QueueTable} (name)
                VALUES (@name)
                ON CONFLICT (name)
                DO UPDATE SET updated = NOW()
                RETURNING id
                """;

            command.Parameters.AddWithValue("name", Name);
        }

        await command.ExecuteScalarAsync(cancellationToken);
    }
}
