using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;

namespace HotChocolate.Fusion.Subscriptions.Redis;

/// <summary>
/// Provides a real Redis endpoint for integration tests.
/// </summary>
/// <remarks>
/// The default path starts Redis through Testcontainers. Set REDIS_CONNECTION_STRING to use an
/// existing Redis instance instead.
/// </remarks>
public sealed class RedisFixture : IAsyncLifetime
{
    private const int RedisPort = 6379;

    private readonly IContainer? _container;
    private readonly int _hostPort;
    private readonly bool _usesExistingInstance;
    private ConnectionMultiplexer? _publisher;

    public RedisFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _usesExistingInstance = true;
            ConnectionString = connectionString;
            return;
        }

        _hostPort = GetFreeTcpPort();
        ConnectionString = "localhost:" + _hostPort;
        _container = new ContainerBuilder("redis:7")
            .WithPortBinding(_hostPort, RedisPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
            .Build();
    }

    public string ConnectionString { get; private set; } = null!;

    public string NextChannel()
        => "events-" + Guid.NewGuid().ToString("N");

    public async ValueTask InitializeAsync()
    {
        if (_usesExistingInstance)
        {
            try
            {
                await GetPublisherAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.Skip(
                    "REDIS_CONNECTION_STRING did not point to a usable Redis instance: "
                    + ex.Message);
            }

            return;
        }

        await _container!.StartAsync();
        await GetPublisherAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_publisher is not null)
        {
            await _publisher.DisposeAsync().ConfigureAwait(false);
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task PublishAsync(
        string channel,
        string body,
        CancellationToken cancellationToken)
    {
        var multiplexer = await GetPublisherAsync().ConfigureAwait(false);
        var subscriber = multiplexer.GetSubscriber();

        await subscriber
            .PublishAsync(CreateRedisChannel(channel), body)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ConnectionMultiplexer> GetPublisherAsync()
    {
        if (_publisher is not null)
        {
            return _publisher;
        }

        _publisher = await ConnectionMultiplexer.ConnectAsync(ConnectionString).ConfigureAwait(false);
        return _publisher;
    }

    private static RedisChannel CreateRedisChannel(string channel)
        => new(channel, RedisChannel.PatternMode.Literal);

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
