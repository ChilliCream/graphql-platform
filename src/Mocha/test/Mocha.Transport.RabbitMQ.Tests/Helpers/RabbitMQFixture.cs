using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CookieCrumble.Resources;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQFixture : IAsyncLifetime
{
    private readonly RabbitMQResource _resource = new();

    public async ValueTask InitializeAsync()
    {
        await _resource.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _resource.DisposeAsync();
    }

    public string ConnectionString => _resource.ConnectionString;

    public async Task<VhostContext> CreateVhostAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var vhostName = GenerateVhostName(testName, filePath);
        await _resource.InvokeCommandAsync(["rabbitmqctl", "add_vhost", vhostName]);
        await _resource.InvokeCommandAsync([
            "rabbitmqctl", "set_permissions", "-p", vhostName, "guest", ".*", ".*", ".*"
        ]);
        return new VhostContext(this, vhostName);
    }

    internal async Task<string> InvokeCommandAsync(string[] command)
    {
        return await _resource.InvokeCommandAsync(command);
    }

    internal async Task CloseAllConnectionsAsync(string reason = "test")
    {
        // Without --global or a vhost, the command only closes connections to the default vhost.
        await _resource.InvokeCommandAsync(["rabbitmqctl", "close_all_connections", "--global", reason]);
    }

    internal async Task DeleteVhostAsync(string vhostName)
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "delete_vhost", vhostName]);
    }

    /// <summary>
    /// Makes the broker close consumer channels that hold a delivery longer than <paramref name="timeout"/>,
    /// for channels opened until the returned handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> SetConsumerTimeoutAsync(TimeSpan timeout)
    {
        // RabbitMQ 3.11 has no consumer-timeout policy, and it checks the timeout on the channel tick
        await EvalAsync(
            $"application:set_env(rabbit, consumer_timeout, {(int)timeout.TotalMilliseconds}), "
            + "application:set_env(rabbit, channel_tick_interval, 250).");

        return new AsyncDisposable(() => EvalAsync(
            "application:set_env(rabbit, consumer_timeout, 1800000), "
            + "application:set_env(rabbit, channel_tick_interval, 60000)."));
    }

    /// <summary>
    /// Raises a memory alarm so the broker blocks every publishing connection until the returned handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> BlockPublishersAsync()
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "set_vm_memory_high_watermark", "0"]);

        return new AsyncDisposable(() => _resource.InvokeCommandAsync(
            ["rabbitmqctl", "set_vm_memory_high_watermark", "0.4"]));
    }

    /// <summary>
    /// Stops the broker application, which closes every connection and refuses new ones until the returned
    /// handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> StopBrokerAsync()
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "stop_app"]);

        return new AsyncDisposable(async () =>
        {
            await _resource.InvokeCommandAsync(["rabbitmqctl", "start_app"]);
            await _resource.InvokeCommandAsync(["rabbitmqctl", "await_startup"]);
        });
    }

    /// <summary>
    /// Lowers the largest message the broker accepts, for channels opened until the returned handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> SetMaxMessageSizeAsync(int bytes)
    {
        // The broker reads the limit when a channel opens, so the bus must start after this call.
        await EvalAsync($"application:set_env(rabbit, max_message_size, {bytes}).");

        return new AsyncDisposable(() => EvalAsync("application:set_env(rabbit, max_message_size, 134217728)."));
    }

    /// <summary>
    /// Puts the node into maintenance mode, which closes every client connection and refuses new ones until the
    /// returned handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> DrainNodeAsync()
    {
        try
        {
            await _resource.InvokeCommandAsync(["rabbitmq-upgrade", "drain"]);
        }
        catch (InvalidOperationException)
        {
            // The command fails when a connection exits while the node closes it, and the node is in maintenance
            // mode anyway, so the returned handle must still revive it.
        }

        return new AsyncDisposable(() => _resource.InvokeCommandAsync(["rabbitmq-upgrade", "revive"]));
    }

    internal Task<string> EvalAsync(string expression)
        => _resource.InvokeCommandAsync(["rabbitmqctl", "eval", expression]);

    private static string GenerateVhostName(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"{testName}_{hash}";
    }
}

public sealed partial class VhostContext : IAsyncDisposable
{
    private readonly RabbitMQFixture _fixture;

    public VhostContext(RabbitMQFixture fixture, string vhostName)
    {
        _fixture = fixture;
        VhostName = vhostName;
        ConnectionSettings = new ConnectionFactory
        {
            Uri = new Uri(fixture.ConnectionString),
            VirtualHost = vhostName
        };
    }

    public string VhostName { get; }

    public IConnectionFactory ConnectionFactory => ConnectionSettings;

    /// <summary>
    /// Gets the connection factory as its concrete type, so a test can shorten heartbeats and timeouts.
    /// </summary>
    public ConnectionFactory ConnectionSettings { get; }

    /// <summary>
    /// Gets the number of consumers on each queue of the vhost.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, int>> GetConsumerCountsAsync()
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "-p", VhostName, "name", "consumers", "--quiet", "--no-table-headers"]);

        return ParseRows(output).ToDictionary(row => row[0], row => int.Parse(row[1]));
    }

    /// <summary>
    /// Gets the number of messages in a queue, both ready and delivered but not yet acknowledged.
    /// </summary>
    public async Task<int> GetMessageCountAsync(string queueName)
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_queues", "-p", VhostName, "name", "messages", "--quiet", "--no-table-headers"]);

        return ParseRows(output).Where(row => row[0] == queueName).Sum(row => int.Parse(row[1]));
    }

    /// <summary>
    /// Deletes a queue the way an operator would, together with its bindings and messages.
    /// </summary>
    public async Task DeleteQueueAsync(string queueName)
    {
        await _fixture.InvokeCommandAsync(["rabbitmqctl", "delete_queue", "-p", VhostName, queueName]);
    }

    /// <summary>
    /// Gets the number of open connections to the vhost.
    /// </summary>
    public async Task<int> GetConnectionCountAsync()
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_connections", "vhost", "--quiet", "--no-table-headers"]);

        return ParseRows(output).Count(row => row[0] == VhostName);
    }

    /// <summary>
    /// Gets the connections, channels and consumers the broker holds for the vhost.
    /// </summary>
    public async Task<BrokerState> GetStateAsync()
    {
        var consumers = await GetConsumerCountsAsync();
        var channels = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_channels", "vhost", "consumer_count", "--quiet", "--no-table-headers"]);
        var channelConsumers = ParseRows(channels)
            .Where(row => row[0] == VhostName)
            .Select(row => int.Parse(row[1]))
            .ToList();

        return new BrokerState(
            await GetConnectionCountAsync(),
            channelConsumers.Count(count => count > 0),
            channelConsumers.Count(count => count == 0),
            consumers.Where(queue => queue.Value > 0).Sum(queue => queue.Value),
            consumers.Count(queue => queue.Value > 0));
    }

    /// <summary>
    /// Polls the broker until its state for the vhost matches <paramref name="expected"/>, and returns the last
    /// observed state.
    /// </summary>
    public async Task<BrokerState> WaitForStateAsync(BrokerState expected, TimeSpan timeout)
    {
        var state = await GetStateAsync();
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (!state.Matches(expected) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            state = await GetStateAsync();
        }

        return state;
    }

    /// <summary>
    /// Force-closes every connection of the vhost that has a consumer, and leaves the others open.
    /// </summary>
    public async Task<int> CloseConsumerConnectionsAsync()
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_channels", "vhost", "connection", "consumer_count", "--quiet", "--no-table-headers"]);

        var connections = ParseRows(output)
            .Where(row => row[0] == VhostName && int.Parse(row[2]) > 0)
            .Select(row => row[1])
            .Distinct()
            .ToList();

        foreach (var connection in connections)
        {
            await _fixture.InvokeCommandAsync(["rabbitmqctl", "close_connection", connection, "test"]);
        }

        return connections.Count;
    }

    /// <summary>
    /// Freezes the broker side of every connection to the vhost, so that the broker neither reads nor sends
    /// frames, including heartbeats, until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> FreezeConnectionsAsync()
    {
        // A suspension ends when the suspending process exits, so a process that waits for "resume" holds it.
        // Connections of other tests can close during the scan, so lookups on them are allowed to fail.
        var suspender = $"mocha_suspender_{VhostName}";
        await _fixture.EvalAsync(
            $$"""
            Tree = fun Walk(Sup) ->
                case catch supervisor:which_children(Sup) of
                    Children when is_list(Children) ->
                        lists:append([case Type of supervisor -> Walk(Pid); _ -> [{Id, Pid}] end
                                      || {Id, Pid, Type, _} <- Children, is_pid(Pid)]);
                    _ -> []
                end
            end,
            Readers = [R || R <- rabbit_networking:connections(),
                            (catch rabbit_reader:info(R, [vhost])) =:= [{vhost, <<"{{VhostName}}">>}]],
            Sups = [Sup || R <- Readers,
                           {dictionary, Dictionary} <- [process_info(R, dictionary)],
                           [Sup | _] <- [proplists:get_value('$ancestors', Dictionary, [])]],
            Pids = [Pid || Sup <- Sups,
                           {Id, Pid} <- Tree(Sup),
                           lists:member(Id, [reader, writer, heartbeat_sender, heartbeat_receiver])],
            Holder = spawn(fun() -> [erlang:suspend_process(P) || P <- Pids], receive resume -> ok end end),
            register('{{suspender}}', Holder),
            length(Pids).
            """);

        return new AsyncDisposable(() => _fixture.EvalAsync($"'{suspender}' ! resume, ok."));
    }

    /// <summary>
    /// Removes the guest user's access to the vhost, so that new connections are refused, until the returned
    /// handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> RevokeAccessAsync()
    {
        await _fixture.InvokeCommandAsync(["rabbitmqctl", "clear_permissions", "-p", VhostName, "guest"]);

        return new AsyncDisposable(() => _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "set_permissions", "-p", VhostName, "guest", ".*", ".*", ".*"]));
    }

    /// <summary>
    /// Creates a user with full permissions on the vhost, and deletes it when the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> AddUserAsync(string userName, string password)
    {
        await _fixture.InvokeCommandAsync(["rabbitmqctl", "add_user", userName, password]);
        await SetPermissionsAsync(userName, ".*", ".*", ".*");

        return new AsyncDisposable(async () =>
        {
            try
            {
                await DeleteUserAsync(userName);
            }
            catch (InvalidOperationException)
            {
                // The test deleted the user already.
            }
        });
    }

    /// <summary>
    /// Deletes a user the way an operator would, which closes every connection of the user.
    /// </summary>
    public Task DeleteUserAsync(string userName)
        => _fixture.InvokeCommandAsync(["rabbitmqctl", "delete_user", userName]);

    /// <summary>
    /// Changes the password of a user. Open connections of the user stay open.
    /// </summary>
    public Task ChangePasswordAsync(string userName, string password)
        => _fixture.InvokeCommandAsync(["rabbitmqctl", "change_password", userName, password]);

    /// <summary>
    /// Sets the configure, write and read permissions of a user on the vhost, each a regular expression of
    /// resource names.
    /// </summary>
    public Task SetPermissionsAsync(string userName, string configure, string write, string read)
        => _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "set_permissions", "-p", VhostName, userName, configure, write, read]);

    /// <summary>
    /// Gets the prefetch count of the consumer on each queue of the vhost.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, int>> GetConsumerPrefetchAsync()
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_consumers", "-p", VhostName, "queue_name", "prefetch_count", "--quiet", "--no-table-headers"]);

        return ParseRows(output).ToDictionary(row => row[0], row => int.Parse(row[1]));
    }

    /// <summary>
    /// Gets the client-provided names of the connections to the vhost.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetConnectionNamesAsync()
    {
        var output = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_connections", "vhost", "client_properties", "--quiet", "--no-table-headers"]);

        return ParseRows(output)
            .Where(row => row[0] == VhostName)
            .Select(row => ConnectionNameProperty().Match(row[1]))
            .Where(match => match.Success)
            .Select(match => match.Groups["name"].Value)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Force-closes the connection that consumes <paramref name="queueName"/>, and leaves the others open.
    /// </summary>
    public async Task CloseConnectionOfConsumerAsync(string queueName)
    {
        var consumers = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_consumers", "-p", VhostName, "queue_name", "channel_pid", "--quiet", "--no-table-headers"]);
        var channelPid = ParseRows(consumers).Single(row => row[0] == queueName)[1];

        var channels = await _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "list_channels", "pid", "connection", "--quiet", "--no-table-headers"]);
        var connectionPid = ParseRows(channels).Single(row => row[0] == channelPid)[1];

        await _fixture.InvokeCommandAsync(["rabbitmqctl", "close_connection", connectionPid, "test"]);
    }

    public async ValueTask DisposeAsync() => await _fixture.DeleteVhostAsync(VhostName);

    [GeneratedRegex("\\{\"connection_name\",\"(?<name>[^\"]*)\"\\}")]
    private static partial Regex ConnectionNameProperty();

    private static IEnumerable<string[]> ParseRows(string output)
        => output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('\t'));
}

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
    int ConsumedQueues)
{
    /// <summary>
    /// The most publish channels a request round trip keeps open: the request and its reply can be awaiting their
    /// confirms at the same time.
    /// </summary>
    public const int MaxIdleChannels = 2;

    /// <summary>
    /// Gets whether this state has the same connections, consumers and consumer channels as <paramref name="expected"/>,
    /// and no more idle channels than concurrent publishes open.
    /// </summary>
    public bool Matches(BrokerState expected)
        => this with { IdleChannels = expected.IdleChannels } == expected && IdleChannels <= MaxIdleChannels;
}

internal static class Condition
{
    /// <summary>
    /// Polls <paramref name="condition"/> until it holds or <paramref name="timeout"/> elapses, and returns
    /// whether it held.
    /// </summary>
    public static async Task<bool> WaitAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it holds or <paramref name="timeout"/> elapses, and returns
    /// whether it held.
    /// </summary>
    public static async Task<bool> WaitAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (!await condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return true;
    }
}

internal sealed class AsyncDisposable(Func<Task> dispose) : IAsyncDisposable
{
    public async ValueTask DisposeAsync() => await dispose();
}

[CollectionDefinition("RabbitMQ")]
public class RabbitMQCollection : ICollectionFixture<RabbitMQFixture>;
