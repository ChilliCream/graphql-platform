using CookieCrumble.Resources;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Testcontainers.Nats;

namespace Mocha.Transport.Nats.Tests.Fixtures;

/// <summary>
/// Shared JetStream-enabled NATS container for the integration tests.
/// </summary>
public sealed partial class JetStreamFixture : IAsyncLifetime
{
    /// <summary>
    /// The NATS server image: the lowest version with per-message TTL (2.11) and message schedules (2.12).
    /// </summary>
    public const string Image = "nats:2.12-alpine";

    private readonly JetStreamResource _resource = new();

    private NatsConnection? _connection;

    /// <summary>
    /// Gets the connection to the container.
    /// </summary>
    public INatsConnection Connection =>
        _connection ?? throw new InvalidOperationException("The fixture is not initialized.");

    /// <summary>
    /// Gets a JetStream context over the container connection.
    /// </summary>
    public INatsJSContext JetStream => new NatsJSContext(Connection);

    /// <summary>
    /// Gets the connection string of the container.
    /// </summary>
    public string ConnectionString => _resource.NatsConnectionString;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _resource.InitializeAsync();

        _connection = new NatsConnection(new NatsOpts
        {
            Url = _resource.NatsConnectionString,
            // Matches what the transport asks of a production connection: never drop a message
            // rather than applying back pressure.
            SubPendingChannelFullMode = System.Threading.Channels.BoundedChannelFullMode.Wait
        });

        await _connection.ConnectAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _resource.DisposeAsync();
    }

    private sealed class JetStreamResource : NatsResource
    {
        protected override NatsBuilder Configure(NatsBuilder builder) => builder.WithImage(Image);
    }
}

/// <summary>
/// Collection definition sharing one container across the integration tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class JetStreamCollection : ICollectionFixture<JetStreamFixture>
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "JetStream";
}
