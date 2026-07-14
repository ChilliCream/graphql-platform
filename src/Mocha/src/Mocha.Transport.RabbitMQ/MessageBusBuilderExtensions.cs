namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for registering the RabbitMQ messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds a RabbitMQ messaging transport to the message bus, applying the specified configuration delegate
    /// after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the RabbitMQ transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRabbitMQ(
        this IMessageBusHostBuilder busBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var transport = new RabbitMQMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        return busBuilder;
    }

    /// <summary>
    /// Adds a RabbitMQ messaging transport to the message bus with default configuration and conventions.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRabbitMQ(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddRabbitMQ(static _ => { });
    }
}
