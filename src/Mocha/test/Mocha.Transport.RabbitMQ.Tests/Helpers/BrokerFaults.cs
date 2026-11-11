using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

/// <summary>
/// Faults that affect the whole broker, and so every test of the collection.
/// </summary>
public sealed class BrokerFaults
{
    private readonly RabbitMQFixture _fixture;
    private readonly ManagementRabbitMQResource _resource;

    internal BrokerFaults(RabbitMQFixture fixture, ManagementRabbitMQResource resource)
    {
        _fixture = fixture;
        _resource = resource;
    }

    /// <summary>
    /// Gets whether the node is in maintenance mode, in which neither the management API nor a new connection
    /// reaches it.
    /// </summary>
    internal bool IsNodeDrained { get; private set; }

    /// <summary>
    /// Makes the broker close consumer channels that hold a delivery longer than <paramref name="timeout"/>,
    /// for channels opened until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> SetConsumerTimeoutAsync(TimeSpan timeout)
    {
        // RabbitMQ 3.11 has no consumer-timeout policy, and it checks the timeout on the channel tick
        await _fixture.EvalAsync(
            $"application:set_env(rabbit, consumer_timeout, {(int)timeout.TotalMilliseconds}), "
            + "application:set_env(rabbit, channel_tick_interval, 250).");

        return new AsyncDisposable(() => _fixture.EvalAsync(
            "application:set_env(rabbit, consumer_timeout, 1800000), "
            + "application:set_env(rabbit, channel_tick_interval, 60000)."));
    }

    /// <summary>
    /// Raises a memory alarm so the broker blocks every publishing connection until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> RaiseMemoryAlarmAsync()
    {
        await _fixture.InvokeCommandAsync(["rabbitmqctl", "set_vm_memory_high_watermark", "0"]);

        return new AsyncDisposable(() => _fixture.InvokeCommandAsync(
            ["rabbitmqctl", "set_vm_memory_high_watermark", "0.4"]));
    }

    /// <summary>
    /// Stops the broker application, which closes every connection and refuses new ones until the returned
    /// handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> StopBrokerAsync()
    {
        await _fixture.InvokeCommandAsync(["rabbitmqctl", "stop_app"]);

        // start_app returns once the broker has started
        return new AsyncDisposable(() => _fixture.InvokeCommandAsync(["rabbitmqctl", "start_app"]));
    }

    /// <summary>
    /// Kills the broker process, and starts it again when the returned handle is disposed.
    /// </summary>
    public Task<IAsyncDisposable> KillBrokerAsync() => _resource.KillAsync();

    /// <summary>
    /// Lowers the largest message the broker accepts until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> SetMaxMessageSizeAsync(int bytes)
    {
        // RabbitMQ 3.13 reads the limit from the application environment when a channel opens, so the bus must
        // start after this call. RabbitMQ 4.3 reads it from a persistent term that is set at boot.
        await _fixture.EvalAsync(
            $$"""
            persistent_term:put(
                mocha_max_message_size,
                {application:get_env(rabbit, max_message_size), persistent_term:get(max_message_size, undefined)}),
            application:set_env(rabbit, max_message_size, {{bytes}}),
            persistent_term:put(max_message_size, {{bytes}}).
            """);

        return new AsyncDisposable(() => _fixture.EvalAsync(
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
    public async Task<IAsyncDisposable> DrainNodeAsync()
    {
        IsNodeDrained = true;
        try
        {
            await _fixture.InvokeCommandAsync(["rabbitmq-upgrade", "drain"]);
        }
        catch (InvalidOperationException)
        {
            // The command fails when a connection exits while the node closes it, and the node is in maintenance
            // mode anyway, so the returned handle must still revive it.
        }

        return new AsyncDisposable(async () =>
        {
            await _fixture.InvokeCommandAsync(["rabbitmq-upgrade", "revive"]);
            IsNodeDrained = false;
        });
    }
}

/// <summary>
/// Faults that affect one vhost, and so only the test that owns it.
/// </summary>
public sealed class VhostFaults
{
    // The transport takes a consumer that is lost within a second of attaching for one that keeps failing, and
    // backs off before it attaches it again.
    private static readonly TimeSpan s_consumerSettleTime = TimeSpan.FromSeconds(1);

    private readonly RabbitMQFixture _fixture;
    private readonly string _vhostName;
    private readonly BrokerView _broker;

    internal VhostFaults(RabbitMQFixture fixture, string vhostName, BrokerView broker)
    {
        _fixture = fixture;
        _vhostName = vhostName;
        _broker = broker;
    }

    /// <summary>
    /// Force-closes every connection to the vhost, and returns the broker's identifiers of the closed connections
    /// once the broker no longer lists them. Throws when the vhost has no connection. The connections are closed
    /// one second after the call at the earliest.
    /// </summary>
    public async Task<IReadOnlyList<string>> CloseConnectionsAsync()
    {
        var requestedAt = Stopwatch.GetTimestamp();
        var connections = await _broker.GetConnectionsAsync();
        if (connections.Count == 0)
        {
            throw new InvalidOperationException($"Vhost '{_vhostName}' has no connection to close.");
        }

        await WaitForConsumersToSettleAsync(requestedAt);
        foreach (var connection in connections)
        {
            await _fixture.CloseConnectionAsync(connection);
        }

        await WaitForClosedAsync(connections);

        return connections;
    }

    /// <summary>
    /// Force-closes the connection that consumes <paramref name="queueName"/>, leaves the others open, and returns
    /// the broker's identifier of the closed connection once the broker no longer lists it. Throws when the queue
    /// does not have exactly one consumer. The connection is closed one second after the call at the earliest.
    /// </summary>
    public async Task<IReadOnlyList<string>> CloseConsumerConnectionAsync(string queueName)
    {
        var requestedAt = Stopwatch.GetTimestamp();
        var connection = await _broker.GetConsumerConnectionAsync(queueName);

        await WaitForConsumersToSettleAsync(requestedAt);
        await _fixture.CloseConnectionAsync(connection);
        await WaitForClosedAsync([connection]);

        return [connection];
    }

    /// <summary>
    /// Deletes a queue the way an operator would, together with its bindings and messages.
    /// </summary>
    public async Task DeleteQueueAsync(string queueName)
    {
        await _fixture.DeleteAsync($"queues/{Uri.EscapeDataString(_vhostName)}/{Uri.EscapeDataString(queueName)}");
    }

    /// <summary>
    /// Freezes the broker side of every connection to the vhost, so that the broker neither reads nor sends
    /// frames, including heartbeats, until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> FreezeConnectionsAsync()
    {
        // A suspension ends when the suspending process exits, so a process that waits for "resume" holds it.
        // Connections of other tests can close during the scan, so lookups on them are allowed to fail.
        var suspender = $"mocha_suspender_{_vhostName}";
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
                            Info <- [catch rabbit_reader:info(R, [vhost, user])],
                            is_list(Info), proplists:get_value(vhost, Info) =:= <<"{{_vhostName}}">>,
                            proplists:get_value(user, Info) =/= <<"{{RabbitMQFixture.ManagementUser}}">>],
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
        await _fixture.DeleteAsync($"permissions/{Uri.EscapeDataString(_vhostName)}/guest");

        return new AsyncDisposable(() => SetPermissionsAsync("guest", ".*", ".*", ".*"));
    }

    /// <summary>
    /// Limits the number of connections the vhost accepts, until the returned handle is disposed. The broker
    /// refuses a connection above the limit with 530 NOT_ALLOWED after the handshake.
    /// </summary>
    public async Task<IAsyncDisposable> LimitConnectionsAsync(int maxConnections)
    {
        var path = $"vhost-limits/{Uri.EscapeDataString(_vhostName)}/max-connections";
        await _fixture.PutAsync(path, new { value = maxConnections });

        return new AsyncDisposable(() => _fixture.DeleteAsync(path));
    }

    /// <summary>
    /// Removes the guest user's permission to declare or delete queues, exchanges and bindings in the vhost,
    /// until the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> DenyDeclaresAsync()
    {
        await SetPermissionsAsync("guest", "^$", ".*", ".*");

        return new AsyncDisposable(() => SetPermissionsAsync("guest", ".*", ".*", ".*"));
    }

    /// <summary>
    /// Creates a user with full permissions on the vhost, and deletes it when the returned handle is disposed.
    /// </summary>
    public async Task<IAsyncDisposable> AddUserAsync(string userName, string password)
    {
        await _fixture.PutAsync($"users/{Uri.EscapeDataString(userName)}", new { password, tags = "" });
        await SetPermissionsAsync(userName, ".*", ".*", ".*");
        _broker.IncludeUser(userName);

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
            $"permissions/{Uri.EscapeDataString(_vhostName)}/{Uri.EscapeDataString(userName)}",
            new { configure, write, read });

    public Task SetReadPermissionAsync(string pattern)
        => SetPermissionsAsync("guest", ".*", ".*", pattern);

    public async Task AllowReadingQueuesOnlyAsync()
    {
        var queues = await _broker.GetQueuesAsync();
        var pattern = $"^({string.Join("|", queues.Keys.Select(Regex.Escape))})$";
        await SetReadPermissionAsync(pattern);
    }

    // Completes once a consumer that was attached at the given time has been attached for the settle time.
    private static Task WaitForConsumersToSettleAsync(long attachedBefore)
    {
        var remaining = s_consumerSettleTime - Stopwatch.GetElapsedTime(attachedBefore);

        return remaining > TimeSpan.Zero
            ? Task.Delay(remaining, TestContext.Current.CancellationToken)
            : Task.CompletedTask;
    }

    private Task WaitForClosedAsync(IReadOnlyList<string> connections)
        => Wait.UntilAsync(
            async () => (await _broker.GetConnectionsAsync()).Intersect(connections).ToArray(),
            remaining => remaining.Length == 0,
            $"the broker to close the connections [{string.Join(", ", connections)}] to vhost '{_vhostName}'");
}

internal sealed class AsyncDisposable(Func<Task> dispose) : IAsyncDisposable
{
    public async ValueTask DisposeAsync() => await dispose();
}
