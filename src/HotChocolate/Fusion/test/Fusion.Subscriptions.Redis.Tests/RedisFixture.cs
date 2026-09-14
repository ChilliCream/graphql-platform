using CookieCrumble.Resources;
using StackExchange.Redis;

namespace HotChocolate.Fusion.Subscriptions.Redis;

/// <summary>
/// Provides a real Redis endpoint for integration tests.
/// </summary>
/// <remarks>
/// The default path starts Redis through Testcontainers. Set REDIS_CONNECTION_STRING to use an
/// existing Redis instance instead; tests skip when that instance cannot be reached.
/// </remarks>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisResource? _resource;
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

        _resource = new RedisResource();
    }

    public string ConnectionString { get; private set; } = null!;

    public string? SkipReason { get; private set; }

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
                SkipReason =
                    "REDIS_CONNECTION_STRING did not point to a usable Redis instance: "
                    + ex.Message;
            }

            return;
        }

        await _resource!.InitializeAsync();
        ConnectionString = _resource.ConnectionString;
        await GetPublisherAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_publisher is not null)
        {
            await _publisher.DisposeAsync().ConfigureAwait(false);
        }

        if (_resource is not null)
        {
            await _resource.DisposeAsync();
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

    /// <summary>
    /// Skips the calling test when REDIS_CONNECTION_STRING points at an instance that could not
    /// be reached.
    /// </summary>
    public void SkipWhenUnavailable()
    {
        if (SkipReason is not null)
        {
            Assert.Skip(SkipReason);
        }
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
}
