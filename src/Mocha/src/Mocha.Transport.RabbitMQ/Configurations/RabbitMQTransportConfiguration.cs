using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Configuration for a RabbitMQ messaging transport, extending the base transport configuration
/// with RabbitMQ-specific connection provider settings.
/// </summary>
public class RabbitMQTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is specified.
    /// </summary>
    public const string DefaultName = "rabbitmq";

    /// <summary>
    /// The default URI schema used for RabbitMQ transport addresses.
    /// </summary>
    public const string DefaultSchema = "rabbitmq";

    /// <summary>
    /// Creates a new configuration instance with the default name and schema.
    /// </summary>
    public RabbitMQTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets a factory delegate that resolves an <see cref="IRabbitMQConnectionProvider"/> from the service provider.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the transport falls back to resolving an <see cref="IConnectionFactory"/> from DI
    /// and wrapping it in a <see cref="ConnectionFactoryRabbitMQConnectionProvider"/>.
    /// </remarks>
    public Func<IServiceProvider, IRabbitMQConnectionProvider>? ConnectionProvider { get; set; }

    /// <summary>
    /// Gets or sets the explicitly declared exchanges for this transport.
    /// </summary>
    public List<RabbitMQExchangeConfiguration> Exchanges { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly declared queues for this transport.
    /// </summary>
    public List<RabbitMQQueueConfiguration> Queues { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly declared bindings for this transport.
    /// </summary>
    public List<RabbitMQBindingConfiguration> Bindings { get; set; } = [];
}

/// <summary>
/// Provides RabbitMQ connection details and the ability to create new connections.
/// </summary>
public interface IRabbitMQConnectionProvider
{
    /// <summary>
    /// Gets the hostname of the RabbitMQ broker.
    /// </summary>
    string Host { get; }

    /// <summary>
    /// Gets the virtual host on the RabbitMQ broker.
    /// </summary>
    string VirtualHost { get; }

    /// <summary>
    /// Gets the port number of the RabbitMQ broker.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Creates a new RabbitMQ connection asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the connection attempt.</param>
    /// <returns>A new open <see cref="IConnection"/>.</returns>
    ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Adapts a RabbitMQ <see cref="IConnectionFactory"/> into the <see cref="IRabbitMQConnectionProvider"/> abstraction.
/// </summary>
/// <param name="factory">The underlying RabbitMQ connection factory.</param>
public sealed class ConnectionFactoryRabbitMQConnectionProvider(IConnectionFactory factory)
    : IRabbitMQConnectionProvider
{
    /// <inheritdoc />
    public string Host => factory.Uri.Host;

    /// <inheritdoc />
    public string VirtualHost => factory.VirtualHost;

    /// <inheritdoc />
    public int Port => factory.Uri.Port;

    /// <inheritdoc />
    public async ValueTask<IConnection> CreateAsync(CancellationToken cancellationToken)
    {
        var connection = await factory.CreateConnectionAsync(cancellationToken);
        return connection;
    }
}
