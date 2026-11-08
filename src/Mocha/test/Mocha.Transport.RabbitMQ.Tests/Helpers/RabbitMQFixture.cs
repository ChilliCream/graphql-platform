using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CookieCrumble.Resources;
using DotNet.Testcontainers.Builders;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQFixture : IAsyncLifetime
{
    private const string ManagementUser = "mocha-tests";
    private const string ManagementPassword = "mocha-tests";

#if RABBITMQ_3
    private readonly ManagementRabbitMQResource _resource = new() { Image = "rabbitmq:3.13" };
#else
    private readonly ManagementRabbitMQResource _resource = new();
#endif
    private readonly HttpClient _management = new();

    public async ValueTask InitializeAsync()
    {
        await _resource.InitializeAsync();
        _management.BaseAddress = new UriBuilder("http", _resource.Hostname, _resource.ManagementPort, "api/").Uri;
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String("guest:guest"u8));
        await PutAsync($"users/{ManagementUser}", new { password = ManagementPassword, tags = "administrator" });
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ManagementUser}:{ManagementPassword}")));

        // Quorum queues refresh the consumer and message counts that list_queues reports on each tick, so tests
        // that read those counts need a short tick.
        await EvalAsync("application:set_env(rabbit, quorum_tick_interval, 250).");
    }

    public async ValueTask DisposeAsync()
    {
        _management.Dispose();
        await _resource.DisposeAsync();
    }

    public string ConnectionString => _resource.ConnectionString;

    public async Task<VhostContext> CreateVhostAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var vhostName = GenerateVhostName(testName, filePath);
        await PutAsync($"vhosts/{Uri.EscapeDataString(vhostName)}", new { });
        await PutAsync(
            $"permissions/{Uri.EscapeDataString(vhostName)}/guest",
            new { configure = ".*", write = ".*", read = ".*" });
        await PutAsync(
            $"permissions/{Uri.EscapeDataString(vhostName)}/{ManagementUser}",
            new { configure = ".*", write = ".*", read = ".*" });
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
        await DeleteAsync($"vhosts/{Uri.EscapeDataString(vhostName)}");
    }

    internal async Task PutAsync(string path, object value)
    {
        using var response = await _management.PutAsJsonAsync(path, value, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    internal async Task DeleteAsync(string path)
    {
        using var response = await _management.DeleteAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
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

    internal Task<IAsyncDisposable> KillBrokerAsync() => _resource.KillAsync();

    /// <summary>
    /// Lowers the largest message the broker accepts until the returned handle is disposed.
    /// </summary>
    internal async Task<IAsyncDisposable> SetMaxMessageSizeAsync(int bytes)
    {
        // RabbitMQ 3.13 reads the limit from the application environment when a channel opens, so the bus must
        // start after this call. RabbitMQ 4.3 reads it from a persistent term that is set at boot.
        await EvalAsync(
            $$"""
            persistent_term:put(
                mocha_max_message_size,
                {application:get_env(rabbit, max_message_size), persistent_term:get(max_message_size, undefined)}),
            application:set_env(rabbit, max_message_size, {{bytes}}),
            persistent_term:put(max_message_size, {{bytes}}).
            """);

        return new AsyncDisposable(() => EvalAsync(
            """
            {Env, Term} = persistent_term:get(mocha_max_message_size),
            case Env of
                {ok, Value} -> application:set_env(rabbit, max_message_size, Value);
                undefined -> application:unset_env(rabbit, max_message_size)
            end,
            case Term of
                undefined -> persistent_term:erase(max_message_size);
                _ -> persistent_term:put(max_message_size, Term)
            end,
            persistent_term:erase(mocha_max_message_size).
            """));
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

    public async Task<int> GetUnacknowledgedMessageCountAsync(string queueName)
    {
        var queues = await ListAsync("list_queues", "-p", VhostName, "name", "messages_unacknowledged");
        return queues.Where(row => row[0] == queueName).Sum(row => int.Parse(row[1]));
    }

    /// <summary>
    /// Deletes a queue the way an operator would, together with its bindings and messages.
    /// </summary>
    public async Task DeleteQueueAsync(string queueName)
    {
        await _fixture.DeleteAsync($"queues/{Uri.EscapeDataString(VhostName)}/{Uri.EscapeDataString(queueName)}");
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

    public async Task WaitForBlockedPublisherAsync(TimeSpan timeout)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(timeout);

        while (!(await ListAsync("list_connections", "vhost", "state"))
            .Any(row => row[0] == VhostName && row[1] == "blocked"))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), deadline.Token);
        }
    }

    /// <summary>
    /// Gets the connections, channels and consumers the broker holds for the vhost.
    /// </summary>
    public async Task<BrokerState> GetStateAsync()
    {
        var output = await _fixture.EvalAsync(
            $$"""
            VHost = <<"{{VhostName}}">>,
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
    /// Polls the broker until its state for the vhost matches <paramref name="expected"/>, and returns the last
    /// observed state.
    /// </summary>
    public async Task<BrokerState> WaitForStateAsync(BrokerState expected, TimeSpan timeout)
    {
        var state = await GetStateAsync();
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (state != expected && DateTimeOffset.UtcNow < deadline)
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
        await _fixture.DeleteAsync($"permissions/{Uri.EscapeDataString(VhostName)}/guest");

        return new AsyncDisposable(() => SetPermissionsAsync("guest", ".*", ".*", ".*"));
    }

    /// <summary>
    /// Creates a user with full permissions on the vhost, and deletes it when the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> AddUserAsync(string userName, string password)
    {
        await _fixture.PutAsync($"users/{Uri.EscapeDataString(userName)}", new { password, tags = "" });
        await SetPermissionsAsync(userName, ".*", ".*", ".*");

        return new AsyncDisposable(async () =>
        {
            try
            {
                await DeleteUserAsync(userName);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // The test deleted the user already.
            }
        });
    }

    /// <summary>
    /// Deletes a user the way an operator would, which closes every connection of the user.
    /// </summary>
    public Task DeleteUserAsync(string userName)
        => _fixture.DeleteAsync($"users/{Uri.EscapeDataString(userName)}");

    /// <summary>
    /// Changes the password of a user. Open connections of the user stay open.
    /// </summary>
    public Task ChangePasswordAsync(string userName, string password)
        => _fixture.PutAsync($"users/{Uri.EscapeDataString(userName)}", new { password, tags = "" });

    /// <summary>
    /// Sets the configure, write and read permissions of a user on the vhost, each a regular expression of
    /// resource names.
    /// </summary>
    public Task SetPermissionsAsync(string userName, string configure, string write, string read)
        => _fixture.PutAsync(
            $"permissions/{Uri.EscapeDataString(VhostName)}/{Uri.EscapeDataString(userName)}",
            new { configure, write, read });

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

    /// <summary>
    /// Gets the number of consumers of the queue <paramref name="queueName"/>, or <see langword="null"/> when the
    /// queue does not exist.
    /// </summary>
    public async Task<int?> GetConsumerCountAsync(string queueName)
    {
        var queues = await ListAsync("list_queues", "-p", VhostName, "name", "consumers");
        var queue = queues.SingleOrDefault(row => row[0] == queueName);

        return queue is null ? null : int.Parse(queue[1], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> exists and has a consumer.
    /// </summary>
    public async Task WaitForConsumerAsync(string queueName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (await GetConsumerCountAsync(queueName) is null or 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
    }

    /// <summary>
    /// Waits until the queue <paramref name="queueName"/> has no consumer.
    /// </summary>
    public async Task WaitForNoConsumerAsync(string queueName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (await GetConsumerCountAsync(queueName) is > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
    }

    public Task SetConfigurePermissionAsync(string pattern)
        => SetPermissionsAsync("guest", pattern, ".*", ".*");

    public async Task WaitForQueueAsync(string queueName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (await GetConsumerCountAsync(queueName) is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
    }

    public Task SetReadPermissionAsync(string pattern)
        => SetPermissionsAsync("guest", ".*", ".*", pattern);

    public async Task AllowReadingQueuesOnlyAsync()
    {
        var queues = await ListAsync("list_queues", "-p", VhostName, "name");
        var pattern = $"^({string.Join("|", queues.Select(row => Regex.Escape(row[0])))})$";
        await SetReadPermissionAsync(pattern);
    }

    /// <summary>
    /// Gets the name of the reply queue of the bus.
    /// </summary>
    public async Task<string> GetReplyQueueNameAsync()
    {
        var queues = await ListAsync("list_queues", "-p", VhostName, "name");
        return queues.Single(row => row[0].StartsWith("response-", StringComparison.Ordinal))[0];
    }

    public async Task WaitForQueueDeletionAsync(string queueName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (await GetConsumerCountAsync(queueName) is not null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }
    }

    public async Task WaitForNoBindingsAsync(string queueName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.IsCancellationRequested)
        {
            var bindings = await ListAsync("list_bindings", "-p", VhostName, "source_name", "destination_name");
            if (bindings.All(row => row[0].Length == 0 || row[1] != queueName))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }

        cts.Token.ThrowIfCancellationRequested();
    }

    public async Task WaitForMessagesAsync(string queueName, int count, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!cts.IsCancellationRequested)
        {
            var queues = await ListAsync("list_queues", "-p", VhostName, "name", "messages_ready");
            var queue = queues.Single(row => row[0] == queueName);
            if (int.Parse(queue[1], CultureInfo.InvariantCulture) == count)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }

        cts.Token.ThrowIfCancellationRequested();
    }

    public async ValueTask DisposeAsync() => await _fixture.DeleteVhostAsync(VhostName);

    private async Task<string[][]> ListAsync(params string[] command)
    {
        var output = await _fixture.InvokeCommandAsync(["rabbitmqctl", .. command, "--quiet", "--no-table-headers"]);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r').Split('\t'))
            .ToArray();
    }

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
    int ConsumedQueues);

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

public static class RabbitMQCollections
{
    public const string ConnectionRecovery = "RabbitMQ Connection Recovery";
    public const string ConsumerRecovery = "RabbitMQ Consumer Recovery";
    public const string Messaging = "RabbitMQ Messaging";
    public const string Topology = "RabbitMQ Topology";
}

[CollectionDefinition(RabbitMQCollections.ConnectionRecovery)]
public sealed class RabbitMQConnectionRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.ConsumerRecovery)]
public sealed class RabbitMQConsumerRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.Messaging)]
public sealed class RabbitMQMessagingCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.Topology)]
public sealed class RabbitMQTopologyCollection : ICollectionFixture<RabbitMQFixture>;

internal sealed class ManagementRabbitMQResource : RabbitMQResource
{
    public ushort ManagementPort => Container.GetMappedPublicPort(15672);

    public async Task<IAsyncDisposable> KillAsync()
    {
        await InvokeCommandAsync(["sh", "-c", "kill -KILL \"$(cat /tmp/rabbitmq.pid)\""]);
        await InvokeCommandAsync(["rm", "/tmp/rabbitmq.pid"]);
        return new AsyncDisposable(async () =>
        {
            await InvokeCommandAsync(["docker-entrypoint.sh", "rabbitmq-server", "-detached"]);
            await InvokeCommandAsync(["rabbitmqctl", "wait", "/tmp/rabbitmq.pid"]);
        });
    }

    protected override RabbitMqBuilder Configure(RabbitMqBuilder builder)
        => builder
            .WithEnvironment("RABBITMQ_PID_FILE", "/tmp/rabbitmq.pid")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand("docker-entrypoint.sh rabbitmq-server -detached && exec sleep infinity")
            .WithResourceMapping("[rabbitmq_management]."u8.ToArray(), "/etc/rabbitmq/enabled_plugins")
            .WithPortBinding(15672, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(15672)
                .ForPath("/api/overview")
                .WithBasicAuthentication("guest", "guest")));
}
