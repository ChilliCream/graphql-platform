using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CookieCrumble.Resources;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Helpers;

public sealed class RabbitMQFixture : IAsyncLifetime
{
#if RABBITMQ_3
    private readonly RabbitMQResource _resource = new() { Image = "rabbitmq:3.13" };
#else
    private readonly RabbitMQResource _resource = new();
#endif

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
        await _resource.InvokeCommandAsync(["rabbitmqctl", "close_all_connections", reason]);
    }

    internal async Task DeleteVhostAsync(string vhostName)
    {
        await _resource.InvokeCommandAsync(["rabbitmqctl", "delete_vhost", vhostName]);
    }

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
        ConnectionFactory = new ConnectionFactory { Uri = new Uri(fixture.ConnectionString), VirtualHost = vhostName };
    }

    public string VhostName { get; }

    public IConnectionFactory ConnectionFactory { get; }

    /// <summary>
    /// Force-closes the connection that consumes from the queue <paramref name="queueName"/>.
    /// </summary>
    public async Task CloseConsumerConnectionAsync(string queueName)
    {
        var consumers = await ListAsync("list_consumers", "-p", VhostName, "queue_name", "channel_pid");
        var channel = consumers.Single(row => row[0] == queueName)[1];
        var channels = await ListAsync("list_channels", "pid", "connection");
        var connection = channels.Single(row => row[0] == channel)[1];

        await _fixture.InvokeCommandAsync(["rabbitmqctl", "close_connection", connection, "test"]);
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

    public async ValueTask DisposeAsync() => await _fixture.DeleteVhostAsync(VhostName);

    private async Task<string[][]> ListAsync(params string[] command)
    {
        var output = await _fixture.InvokeCommandAsync(["rabbitmqctl", .. command, "--quiet", "--no-table-headers"]);

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split('\t'))
            .ToArray();
    }
}

[CollectionDefinition("RabbitMQ")]
public class RabbitMQCollection : ICollectionFixture<RabbitMQFixture>;
