using System.Collections.Immutable;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Represents a topic in the PostgreSQL messaging topology. Topics are the publishing
/// destinations - messages published to a topic are distributed to all subscribed queues.
/// </summary>
public sealed class PostgresTopic : TopologyResource<PostgresTopicConfiguration>, IPostgresResource
{
    private ImmutableArray<PostgresSubscription> _subscriptions = [];

    /// <summary>
    /// Gets the topic name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets whether this topic should be auto-provisioned in the database.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the subscriptions originating from this topic.
    /// </summary>
    public IReadOnlyList<PostgresSubscription> Subscriptions => _subscriptions;

    protected override void OnInitialize(PostgresTopicConfiguration configuration)
    {
        Name = configuration.Name!;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(PostgresTopicConfiguration configuration)
    {
        var builder = new UriBuilder(Topology.Address);
        builder.Path = Topology.Address.AbsolutePath.TrimEnd('/') + "/t/" + Name;
        Address = builder.Uri;
    }

    internal void AddSubscription(PostgresSubscription subscription)
    {
        ImmutableInterlocked.Update(ref _subscriptions, static (s, sub) => s.Add(sub), subscription);
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

        var naming = schemaOptions;
        await using var connection = await connectionManager.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            $"""
            INSERT INTO {naming.TopicTable} (name)
            VALUES (@name)
            ON CONFLICT (name)
            DO UPDATE SET updated = NOW()
            RETURNING id
            """;

        command.Parameters.AddWithValue("name", Name);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
