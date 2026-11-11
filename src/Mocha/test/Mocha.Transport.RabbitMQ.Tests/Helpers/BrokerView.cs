using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// Reads the state the broker holds for one vhost, and waits for it to change.
/// </summary>
public sealed partial class BrokerView
{
    private readonly RabbitMQFixture _fixture;
    private readonly string _vhostName;

    internal BrokerView(RabbitMQFixture fixture, string vhostName)
    {
        _fixture = fixture;
        _vhostName = vhostName;
    }

    /// <summary>
    /// Gets the queues of the vhost by name.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, QueueInfo>> GetQueuesAsync()
    {
        var queues = await _fixture.ListAsync(
            "list_queues", "-p", _vhostName, "name", "consumers", "messages_ready", "messages_unacknowledged");

        return queues.ToDictionary(
            row => row["name"],
            row => new QueueInfo(
                ParseCount(row["consumers"]),
                ParseCount(row["messages_ready"]),
                ParseCount(row["messages_unacknowledged"])));
    }

    /// <summary>
    /// Gets the queue <paramref name="queueName"/>, or <see langword="null"/> when the queue does not exist.
    /// </summary>
    public async Task<QueueInfo?> GetQueueAsync(string queueName)
        => (await GetQueuesAsync()).GetValueOrDefault(queueName);

    /// <summary>
    /// Gets the consumers on all queues of the vhost.
    /// </summary>
    public async Task<IReadOnlyList<ConsumerInfo>> GetConsumersAsync()
    {
        var consumers = await _fixture.ListAsync(
            "list_consumers", "-p", _vhostName, "queue_name", "consumer_tag", "channel_pid", "prefetch_count");

        return consumers
            .Select(row => new ConsumerInfo(
                row["queue_name"],
                row["consumer_tag"],
                row["channel_pid"],
                ParseCount(row["prefetch_count"])))
            .ToArray();
    }

    /// <summary>
    /// Gets the consumer of the queue <paramref name="queueName"/>, and throws when the queue does not have
    /// exactly one consumer.
    /// </summary>
    public async Task<ConsumerInfo> GetConsumerAsync(string queueName)
    {
        var consumers = await GetConsumersOfAsync(queueName);

        return consumers is [var consumer]
            ? consumer
            : throw new InvalidOperationException(
                $"Expected queue '{queueName}' to have one consumer, but observed [{string.Join(", ", consumers)}].");
    }

    /// <summary>
    /// Gets the connections to the vhost.
    /// </summary>
    public async Task<IReadOnlyList<ConnectionInfo>> GetConnectionsAsync()
    {
        var connections = await _fixture.ListAsync("list_connections", "pid", "vhost", "state");

        return connections
            .Where(row => row["vhost"] == _vhostName)
            .Select(row => new ConnectionInfo(row["pid"], row["state"]))
            .ToArray();
    }

    /// <summary>
    /// Gets the number of open connections to the vhost.
    /// </summary>
    public async Task<int> GetConnectionCountAsync() => (await GetConnectionsAsync()).Count;

    /// <summary>
    /// Gets the client-provided names of the connections to the vhost.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetConnectionNamesAsync()
    {
        var connections = await _fixture.ListAsync("list_connections", "vhost", "client_properties");

        return connections
            .Where(row => row["vhost"] == _vhostName)
            .Select(row => ConnectionNameProperty().Match(row["client_properties"]))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Gets the connections, channels and consumers the broker holds for the vhost.
    /// </summary>
    public async Task<BrokerState> GetStateAsync()
    {
        var output = await _fixture.EvalAsync(
            $$"""
            VHost = <<"{{_vhostName}}">>,
            QueueCounts = [proplists:get_value(consumers, Info, 0)
                           || Q <- rabbit_amqqueue:list(VHost),
                              Info <- [catch rabbit_amqqueue:info(Q, [consumers])], is_list(Info)],
            Connections = [C || C <- rabbit_networking:connections(),
                                (catch rabbit_reader:info(C, [vhost])) =:= [{vhost, VHost}]],
            ChannelCounts = [proplists:get_value(consumer_count, Info, 0)
                             || C <- rabbit_channel:list(),
                                Info <- [catch rabbit_channel:info(C, [vhost, consumer_count])],
                                is_list(Info), proplists:get_value(vhost, Info) =:= VHost],
            [length(Connections),
             length([N || N <- ChannelCounts, N > 0]),
             length([N || N <- ChannelCounts, N =:= 0]),
             lists:sum(QueueCounts),
             length([N || N <- QueueCounts, N > 0])].
            """);
        var counts = JsonSerializer.Deserialize<int[]>(output)!;

        return new BrokerState(counts[0], counts[1], counts[2], counts[3], counts[4]);
    }

    /// <summary>
    /// Waits until the broker counts the given number of connections against the connection limit of the
    /// vhost. The count follows an open connection with a delay.
    /// </summary>
    public Task WaitForCountedConnectionsAsync(int count)
        => Wait.UntilAsync(
            async () => int.Parse(
                await _fixture.EvalAsync(
                    $$"""rabbit_connection_tracking:count_tracked_items_in({vhost, <<"{{_vhostName}}">>})."""),
                CultureInfo.InvariantCulture),
            counted => counted == count,
            $"the broker to count {count} connections against the limit of vhost '{_vhostName}'");

    /// <summary>
    /// Gets the name of the reply queue of the bus.
    /// </summary>
    public async Task<string> GetReplyQueueNameAsync()
        => (await GetQueuesAsync()).Keys.Single(name => name.StartsWith("response-", StringComparison.Ordinal));

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> has exactly one consumer, and returns it.
    /// </summary>
    public async Task<ConsumerInfo> WaitForConsumerAsync(string queueName)
    {
        var consumers = await Wait.UntilAsync(
            () => GetConsumersOfAsync(queueName),
            found => found.Count == 1,
            $"queue '{queueName}' to have one consumer");

        return consumers[0];
    }

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> has exactly one consumer and that consumer is not
    /// <paramref name="otherThan"/>, and returns it.
    /// </summary>
    public async Task<ConsumerInfo> WaitForConsumerAsync(string queueName, ConsumerInfo otherThan)
    {
        var consumers = await Wait.UntilAsync(
            () => GetConsumersOfAsync(queueName),
            found => found is [var consumer] && consumer.ConsumerTag != otherThan.ConsumerTag,
            $"queue '{queueName}' to have one consumer other than {otherThan.ConsumerTag}");

        return consumers[0];
    }

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> has no consumer. A queue that does not exist has none.
    /// </summary>
    public Task WaitForNoConsumerAsync(string queueName)
        => Wait.UntilAsync(
            () => GetQueueAsync(queueName),
            queue => queue is null or { Consumers: 0 },
            $"queue '{queueName}' to have no consumer");

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> exists.
    /// </summary>
    public Task WaitForQueueAsync(string queueName)
        => Wait.UntilAsync(
            () => GetQueueAsync(queueName),
            queue => queue is not null,
            $"queue '{queueName}' to exist");

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> does not exist.
    /// </summary>
    public Task WaitForQueueDeletionAsync(string queueName)
        => Wait.UntilAsync(
            () => GetQueueAsync(queueName),
            queue => queue is null,
            $"queue '{queueName}' to be deleted");

    /// <summary>
    /// Waits until no exchange is bound to the queue <paramref name="queueName"/>.
    /// </summary>
    public Task WaitForNoBindingsAsync(string queueName)
        => Wait.UntilAsync(
            async () =>
            {
                var bindings = await _fixture.ListAsync(
                    "list_bindings", "-p", _vhostName, "source_name", "destination_name");

                // every queue has a binding to the default exchange, whose name is empty
                return bindings
                    .Where(row => row["source_name"].Length > 0 && row["destination_name"] == queueName)
                    .Select(row => row["source_name"])
                    .ToArray();
            },
            sources => sources.Length == 0,
            $"no exchange to be bound to queue '{queueName}'");

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> holds <paramref name="count"/> messages that are
    /// ready for delivery.
    /// </summary>
    public Task WaitForMessagesAsync(string queueName, int count)
        => Wait.UntilAsync(
            () => GetQueueAsync(queueName),
            queue => queue?.Ready == count,
            $"queue '{queueName}' to hold {count} ready messages");

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> holds no message, neither ready nor delivered but
    /// not yet acknowledged.
    /// </summary>
    public Task WaitForEmptyQueueAsync(string queueName)
        => Wait.UntilAsync(
            () => GetQueueAsync(queueName),
            queue => queue is { Ready: 0, Unacknowledged: 0 },
            $"queue '{queueName}' to hold no messages");

    /// <summary>
    /// Waits until the broker blocks a publishing connection to the vhost.
    /// </summary>
    public Task WaitForBlockedPublisherAsync()
        => Wait.UntilAsync(
            GetConnectionsAsync,
            connections => connections.Any(connection => connection.State == "blocked"),
            $"a blocked connection to vhost '{_vhostName}'");

    /// <summary>
    /// Waits until a resource alarm of the broker applies to a connection to the vhost. Such a connection is
    /// blocked, or is blocked as soon as it publishes.
    /// </summary>
    public Task WaitForAlarmAsync()
        => Wait.UntilAsync(
            GetConnectionsAsync,
            connections => connections.Any(connection => connection.State is "blocking" or "blocked"),
            $"a resource alarm to apply to a connection to vhost '{_vhostName}'");

    /// <summary>
    /// Waits until the vhost has no open connection.
    /// </summary>
    public Task WaitForNoConnectionAsync()
        => Wait.UntilAsync(
            GetConnectionsAsync,
            connections => connections.Count == 0,
            $"no connection to vhost '{_vhostName}'");

    private async Task<IReadOnlyList<ConsumerInfo>> GetConsumersOfAsync(string queueName)
        => (await GetConsumersAsync()).Where(consumer => consumer.Queue == queueName).ToArray();

    private static int ParseCount(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    [GeneratedRegex("\\{\"connection_name\",\"(?<name>[^\"]*)\"\\}")]
    private static partial Regex ConnectionNameProperty();
}

/// <summary>
/// A queue as the broker reports it.
/// </summary>
/// <param name="Consumers">The number of consumers on the queue.</param>
/// <param name="Ready">The number of messages that are ready for delivery.</param>
/// <param name="Unacknowledged">The number of messages that are delivered but not yet acknowledged.</param>
public sealed record QueueInfo(int Consumers, int Ready, int Unacknowledged)
{
    /// <summary>
    /// Gets the number of messages in the queue, both ready and delivered but not yet acknowledged.
    /// </summary>
    public int Messages => Ready + Unacknowledged;
}

/// <summary>
/// A consumer as the broker reports it. Every attach gets a new consumer tag from the broker.
/// </summary>
/// <param name="Queue">The name of the queue the consumer consumes from.</param>
/// <param name="ConsumerTag">The consumer tag.</param>
/// <param name="ChannelPid">The broker's identifier of the channel the consumer is on.</param>
/// <param name="Prefetch">The prefetch count of the consumer.</param>
public sealed record ConsumerInfo(string Queue, string ConsumerTag, string ChannelPid, int Prefetch);

/// <summary>
/// A connection as the broker reports it.
/// </summary>
/// <param name="Pid">The broker's identifier of the connection.</param>
/// <param name="State">The state of the connection, such as <c>running</c> or <c>blocked</c>.</param>
public sealed record ConnectionInfo(string Pid, string State);

/// <summary>
/// The connections, channels and consumers the broker holds for one vhost.
/// </summary>
/// <param name="Connections">The number of open connections.</param>
/// <param name="ConsumerChannels">The number of open channels that have a consumer.</param>
/// <param name="IdleChannels">The number of open channels without a consumer, such as pooled publish channels.</param>
/// <param name="Consumers">The number of consumers across all queues.</param>
/// <param name="ConsumedQueues">The number of queues that have at least one consumer.</param>
public sealed record BrokerState(
    int Connections,
    int ConsumerChannels,
    int IdleChannels,
    int Consumers,
    int ConsumedQueues);
