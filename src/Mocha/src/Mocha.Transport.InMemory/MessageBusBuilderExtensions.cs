namespace Mocha.Transport.InMemory;

/// <summary>
/// Extension methods for registering the in-memory messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class InMemoryMessageBusBuilderExtensions
{
    /// <summary>
    /// Adds an in-memory messaging transport to the message bus and configures it with the supplied delegate.
    /// </summary>
    /// <remarks>
    /// Default conventions (queue naming, topology discovery, dispatch topology) are automatically
    /// registered before the caller's configuration delegate runs.
    /// </remarks>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <param name="configure">A delegate to configure endpoints, topology, middleware, and conventions.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddInMemory(
        this IMessageBusHostBuilder busBuilder,
        Action<IInMemoryMessagingTransportDescriptor> configure)
    {
        var transport = new InMemoryMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        return busBuilder;
    }

    /// <summary>
    /// Adds an in-memory messaging transport to the message bus with default configuration.
    /// </summary>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddInMemory(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddInMemory(static _ => { });
    }
}
