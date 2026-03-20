namespace Mocha.Transport.Postgres;

/// <summary>
/// Represents a subscription linking a topic to a queue in the PostgreSQL messaging topology.
/// When a message is published to the topic, it is delivered to all subscribed queues.
/// </summary>
public sealed class PostgresSubscription : TopologyResource<PostgresSubscriptionConfiguration>, IPostgresResource
{
    /// <summary>
    /// Gets the source topic for this subscription.
    /// </summary>
    public PostgresTopic Source { get; private set; } = null!;

    /// <summary>
    /// Gets the destination queue for this subscription.
    /// </summary>
    public PostgresQueue Destination { get; private set; } = null!;

    /// <summary>
    /// Gets whether this subscription should be auto-provisioned in the database.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    protected override void OnInitialize(PostgresSubscriptionConfiguration configuration)
    {
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(PostgresSubscriptionConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/b/t/" + Source.Name + "/q/" + Destination.Name;
        Address = builder.Uri;
    }

    internal void SetSource(PostgresTopic source)
    {
        Source = source;
    }

    internal void SetDestination(PostgresQueue destination)
    {
        Destination = destination;
    }

    /// <inheritdoc />
    public async Task ProvisionAsync(
        PostgresConnectionManager connectionManager,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        CancellationToken cancellationToken)
    {
        if (AutoProvision == false)
        {
            return;
        }

        await using var connection = await connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
             WITH topic_info AS (
                 SELECT id FROM {schemaOptions.TopicTable} WHERE name = @source_name LIMIT 1
             ),
             queue_info AS (
                 SELECT id FROM {schemaOptions.QueueTable} WHERE name = @destination_name LIMIT 1
             )
             INSERT INTO {schemaOptions.QueueSubscriptionTable} (source_id, destination_id)
             SELECT topic_info.id, queue_info.id
             FROM topic_info, queue_info
             ON CONFLICT (source_id, destination_id) DO NOTHING
             """;

        command.Parameters.AddWithValue("source_name", Source.Name);
        command.Parameters.AddWithValue("destination_name", Destination.Name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
