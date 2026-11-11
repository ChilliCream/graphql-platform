using System.Net;
using System.Net.Http.Headers;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CookieCrumble.Resources;
using DotNet.Testcontainers.Containers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;
using ContainerWait = DotNet.Testcontainers.Builders.Wait;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQFixture : IAsyncLifetime
{
    /// <summary>
    /// The user the helpers observe the broker with. Its connections are not connections of a test.
    /// </summary>
    internal const string ManagementUser = "mocha-tests";

    private const string ManagementPassword = "mocha-tests";

#if RABBITMQ_3
    private readonly ManagementRabbitMQResource _resource = new() { Image = "rabbitmq:3.13" };
#else
    private readonly ManagementRabbitMQResource _resource = new();
#endif
    private readonly HttpClient _management = new();

    public RabbitMQFixture()
    {
        Faults = new BrokerFaults(this, _resource);
    }

    public async ValueTask InitializeAsync()
    {
        await _resource.InitializeAsync();
        _management.BaseAddress = new UriBuilder("http", _resource.Hostname, _resource.ManagementPort, "api/").Uri;
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String("guest:guest"u8));
        await PutAsync($"users/{ManagementUser}", new { password = ManagementPassword, tags = "administrator" });
        _management.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ManagementUser}:{ManagementPassword}")));

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

    /// <summary>
    /// Gets the faults that affect the whole broker. A fault of a single vhost is on <see cref="VhostContext.Faults"/>.
    /// </summary>
    public BrokerFaults Faults { get; }

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

    /// <summary>
    /// Runs a command inside the broker container and returns its output. Throws a <see cref="TimeoutException"/>
    /// when the command does not complete within <see cref="TestTimeouts.Command"/>.
    /// </summary>
    internal Task<string> InvokeCommandAsync(string[] command) => _resource.ExecAsync(command);

    internal Task<string> EvalAsync(string expression) => InvokeCommandAsync(["rabbitmqctl", "eval", expression]);

    /// <summary>
    /// Runs a <c>rabbitmqctl</c> list command and returns its rows, each keyed by column name.
    /// </summary>
    internal async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ListAsync(params string[] command)
    {
        // rabbitmqctl prints the columns of some lists in its own order, so the header row names them
        var output = await InvokeCommandAsync(["rabbitmqctl", .. command, "--quiet"]);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return [];
        }

        var columns = SplitRow(lines[0]);
        var rows = new List<IReadOnlyDictionary<string, string>>(lines.Length - 1);
        foreach (var line in lines.Skip(1))
        {
            var cells = SplitRow(line);
            var row = new Dictionary<string, string>(columns.Length);
            for (var i = 0; i < columns.Length; i++)
            {
                // a row can end before its trailing empty cells
                row[columns[i]] = i < cells.Length ? cells[i] : "";
            }

            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Gets the names of the connections the given users hold to a vhost. The broker lists a connection from
    /// the moment it is open.
    /// </summary>
    internal async Task<IReadOnlyList<string>> GetConnectionsAsync(
        string vhostName,
        IReadOnlyCollection<string> userNames)
    {
        if (Faults.IsNodeDrained)
        {
            // the management API does not answer while the node is in maintenance mode
            var connections = await ListAsync("list_connections", "name", "vhost", "user");

            return connections
                .Where(row => row["vhost"] == vhostName && userNames.Contains(row["user"]))
                .Select(row => row["name"])
                .ToArray();
        }

        var names = new List<string>();
        foreach (var userName in userNames)
        {
            using var response = await _management.GetAsync(
                $"connections/username/{Uri.EscapeDataString(userName)}",
                TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            await using var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
            using var document = await JsonDocument.ParseAsync(
                content,
                cancellationToken: TestContext.Current.CancellationToken);

            foreach (var connection in document.RootElement.EnumerateArray())
            {
                if (connection.GetProperty("vhost").GetString() == vhostName)
                {
                    names.Add(connection.GetProperty("name").GetString()!);
                }
            }
        }

        return names;
    }

    /// <summary>
    /// Force-closes a connection the way an operator would. A connection that is closed already is ignored.
    /// </summary>
    internal async Task CloseConnectionAsync(string connectionName)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"connections/{Uri.EscapeDataString(connectionName)}");
        request.Headers.Add("X-Reason", "test");
        using var response = await _management.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Gets the names of the exchanges that are bound to a destination of the given name.
    /// </summary>
    internal async Task<IReadOnlyList<string>> GetBindingSourcesAsync(string vhostName, string destinationName)
    {
        using var response = await _management.GetAsync(
            $"bindings/{Uri.EscapeDataString(vhostName)}",
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(
            content,
            cancellationToken: TestContext.Current.CancellationToken);

        // every queue has a binding to the default exchange, whose name is empty
        return document.RootElement.EnumerateArray()
            .Where(binding => binding.GetProperty("destination").GetString() == destinationName)
            .Select(binding => binding.GetProperty("source").GetString()!)
            .Where(source => source.Length > 0)
            .ToArray();
    }

    /// <summary>
    /// Reads the consumers and the ready messages of a queue with a passive declare on a connection of its own.
    /// Returns <see langword="null"/> when the node is in maintenance mode, or when the queue is exclusive to
    /// another connection.
    /// </summary>
    internal async Task<QueueProbe?> ProbeQueueAsync(string vhostName, string queueName)
    {
        if (Faults.IsNodeDrained)
        {
            return null;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(ConnectionString),
            VirtualHost = vhostName,
            UserName = ManagementUser,
            Password = ManagementPassword,
            AutomaticRecoveryEnabled = false,
            ClientProvidedName = $"{ManagementUser} queue probe"
        };

        await using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            var queue = await channel.QueueDeclarePassiveAsync(queueName, TestContext.Current.CancellationToken);

            return new QueueProbe(Exists: true, (int)queue.ConsumerCount, (int)queue.MessageCount);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == Constants.NotFound)
        {
            return new QueueProbe(Exists: false, Consumers: 0, Ready: 0);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == Constants.ResourceLocked)
        {
            return null;
        }
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

    private static string[] SplitRow(string line) => line.TrimEnd('\r').Split('\t');

    private static string GenerateVhostName(string testName, string filePath)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)))[..8];
        return $"{testName}_{hash}";
    }
}

public sealed class VhostContext : IAsyncDisposable
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
        Broker = new BrokerView(fixture, vhostName);
        Faults = new VhostFaults(fixture, vhostName, Broker);
    }

    public string VhostName { get; }

    public IConnectionFactory ConnectionFactory => ConnectionSettings;

    /// <summary>
    /// Gets the connection factory as its concrete type, so a test can shorten heartbeats and timeouts.
    /// </summary>
    public ConnectionFactory ConnectionSettings { get; }

    /// <summary>
    /// Gets the state the broker holds for the vhost.
    /// </summary>
    public BrokerView Broker { get; }

    /// <summary>
    /// Gets the faults that affect only this vhost.
    /// </summary>
    public VhostFaults Faults { get; }

    public async ValueTask DisposeAsync() => await _fixture.DeleteVhostAsync(VhostName);
}

/// <summary>
/// What a passive declare tells about a queue.
/// </summary>
/// <param name="Exists">Whether the queue exists.</param>
/// <param name="Consumers">The number of consumers on the queue.</param>
/// <param name="Ready">The number of messages that are ready for delivery.</param>
internal sealed record QueueProbe(bool Exists, int Consumers, int Ready);

/// <summary>
/// The collections of the tests that need a broker. Every collection has a broker of its own and runs its tests
/// one after another, and <c>xunit.runner.json</c> lets all collections run at the same time, so the longest one
/// decides how long a run takes.
/// </summary>
public static class RabbitMQCollections
{
    public const string BrokerRecovery = "RabbitMQ Broker Recovery";
    public const string ConnectionRecovery = "RabbitMQ Connection Recovery";
    public const string PublisherRecovery = "RabbitMQ Publisher Recovery";
    public const string Dispatcher = "RabbitMQ Dispatcher";
    public const string ConsumerRecovery = "RabbitMQ Consumer Recovery";
    public const string ConsumerAttachment = "RabbitMQ Consumer Attachment";
    public const string Messaging = "RabbitMQ Messaging";
    public const string Topology = "RabbitMQ Topology";
}

[CollectionDefinition(RabbitMQCollections.BrokerRecovery)]
public sealed class RabbitMQBrokerRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.ConnectionRecovery)]
public sealed class RabbitMQConnectionRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.PublisherRecovery)]
public sealed class RabbitMQPublisherRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.Dispatcher)]
public sealed class RabbitMQDispatcherCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.ConsumerRecovery)]
public sealed class RabbitMQConsumerRecoveryCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.ConsumerAttachment)]
public sealed class RabbitMQConsumerAttachmentCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.Messaging)]
public sealed class RabbitMQMessagingCollection : ICollectionFixture<RabbitMQFixture>;

[CollectionDefinition(RabbitMQCollections.Topology)]
public sealed class RabbitMQTopologyCollection : ICollectionFixture<RabbitMQFixture>;

internal sealed class ManagementRabbitMQResource : RabbitMQResource
{
    public ushort ManagementPort => Container.GetMappedPublicPort(15672);

    public Task<string> ExecAsync(string[] command) => Container.ExecWithTimeoutAsync(command);

    public async Task<IAsyncDisposable> KillAsync()
    {
        await ExecAsync(["sh", "-c", "kill -KILL \"$(cat /tmp/rabbitmq.pid)\""]);
        await ExecAsync(["rm", "/tmp/rabbitmq.pid"]);
        return new AsyncDisposable(async () =>
        {
            await ExecAsync(["docker-entrypoint.sh", "rabbitmq-server", "-detached"]);
            await ExecAsync(["rabbitmqctl", "wait", "/tmp/rabbitmq.pid"]);
        });
    }

    protected override RabbitMqBuilder Configure(RabbitMqBuilder builder)
        => builder
            .WithEnvironment("RABBITMQ_PID_FILE", "/tmp/rabbitmq.pid")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand("docker-entrypoint.sh rabbitmq-server -detached && exec sleep infinity")
            .WithResourceMapping("[rabbitmq_management]."u8.ToArray(), "/etc/rabbitmq/enabled_plugins")
            .WithPortBinding(15672, true)
            .WithWaitStrategy(ContainerWait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .ForPort(15672)
                .ForPath("/api/overview")
                .WithBasicAuthentication("guest", "guest")));
}

internal static class ContainerCommands
{
    /// <summary>
    /// Runs a command inside the container and returns its output. Throws a <see cref="TimeoutException"/> when
    /// the command does not complete within <see cref="TestTimeouts.Command"/>, and an
    /// <see cref="InvalidOperationException"/> when it exits with an error.
    /// </summary>
    public static async Task<string> ExecWithTimeoutAsync(this IContainer container, string[] command)
    {
        var testAborted = TestContext.Current.CancellationToken;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(testAborted);
        deadline.CancelAfter(TestTimeouts.Command);

        ExecResult result;
        try
        {
            result = await container.ExecAsync(command, deadline.Token).WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!testAborted.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"'{string.Join(' ', command)}' did not complete within {TestTimeouts.Command.TotalSeconds:0} s.");
        }

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"'{string.Join(' ', command)}' failed with exit code {result.ExitCode}: {result.Stderr}");
        }

        return result.Stdout;
    }
}
