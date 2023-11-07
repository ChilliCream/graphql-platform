using Npgsql;

namespace HotChocolate.Subscriptions.Postgres;

/// <summary>
/// Represents the options for the the postgres subscription transport
/// </summary>
public sealed class PostgresSubscriptionOptions
{
    /// <summary>
    /// The connection factory that is used to create a new connection. The new connections are
    /// long lived connections and will stay open until the application shuts down.
    /// The connection should have the following configuration to work the best:
    /// <list type="bullet">
    ///     <item>
    ///         KeepAlive=30 - Set a keep alive interval to keep the connection alive
    ///     </item>
    ///     <item>
    ///         Pooling=false - The pooling should be disabled because it is not needed
    ///     </item>
    ///     <item>
    ///         Enlist=false - The subscriptions run in the background and should not be enlisted
    ///                        into any transaction
    ///     </item>
    /// </list>
    /// </summary>
    public Func<CancellationToken, ValueTask<NpgsqlConnection>> ConnectionFactory { get; set; } =
        default!;

    /// <summary>
    /// The name of the postgres channel that is used to send/receive messages.
    /// </summary>
    public string ChannelName { get; set; } = "hotchocolate_subscriptions";

    /// <summary>
    /// The maximum number of messages that are sent in one batch.
    /// </summary>
    public int MaxSendBatchSize { get; set; } = 256;

    /// <summary>
    /// The maximum number of messages that can be queued for sending. If the queue is full the
    /// subscription will wait until the queue has space again.
    /// </summary>
    public int MaxSendQueueSize { get; set; } = 2048;

    /// <summary>
    /// The maximum serialized size of a message that can be sent using postgres notify.
    /// </summary>
    public int MaxMessagePayloadSize { get; set; } = 8000;

    /// <summary>
    /// The subscription options that are used to configure the subscriptions.
    /// </summary>
    public SubscriptionOptions SubscriptionOptions { get; set; } = new();
}
