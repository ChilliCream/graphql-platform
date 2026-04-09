namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Extension methods for registering the Azure Event Hub messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Event Hub messaging transport to the message bus, applying the specified configuration delegate
    /// after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the Event Hub transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddEventHub(
        this IMessageBusHostBuilder busBuilder,
        Action<IEventHubMessagingTransportDescriptor> configure)
    {
        var transport = new EventHubMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        return busBuilder;
    }

    /// <summary>
    /// Adds an Azure Event Hub messaging transport to the message bus with default configuration and conventions.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddEventHub(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddEventHub(static _ => { });
    }
}
