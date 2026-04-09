namespace Mocha.Transport.Kafka;

/// <summary>
/// Extension methods for registering the Kafka messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds a Kafka messaging transport to the message bus, applying the specified configuration delegate
    /// after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the Kafka transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddKafka(
        this IMessageBusHostBuilder busBuilder,
        Action<IKafkaMessagingTransportDescriptor> configure)
    {
        var transport = new KafkaMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        return busBuilder;
    }

    /// <summary>
    /// Adds a Kafka messaging transport to the message bus with default configuration and conventions.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddKafka(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddKafka(static _ => { });
    }
}
