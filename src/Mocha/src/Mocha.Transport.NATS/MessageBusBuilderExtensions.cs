namespace Mocha.Transport.NATS;

/// <summary>
/// Extension methods for registering the NATS JetStream messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds a NATS JetStream messaging transport to the message bus, applying the specified configuration delegate
    /// after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the NATS transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddNats(
        this IMessageBusHostBuilder busBuilder,
        Action<INatsMessagingTransportDescriptor> configure)
    {
        var transport = new NatsMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        return busBuilder;
    }

    /// <summary>
    /// Adds a NATS JetStream messaging transport to the message bus with default configuration and conventions.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddNats(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddNats(static _ => { });
    }
}
