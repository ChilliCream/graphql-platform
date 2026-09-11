using Microsoft.Extensions.DependencyInjection;
using Mocha.Scheduling;

namespace Mocha.Transport.Nats;

/// <summary>
/// Extension methods for registering the NATS JetStream messaging transport on an
/// <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class MessageBusBuilderExtensions
{
    /// <summary>
    /// Adds a NATS JetStream messaging transport to the message bus, applying the specified
    /// configuration delegate after default conventions and middleware have been registered.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <param name="configure">A delegate that configures the NATS transport descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddNats(
        this IMessageBusHostBuilder busBuilder,
        Action<INatsMessagingTransportDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var transport = new NatsMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        // JetStream holds a scheduled message itself, so the store publishes immediately with the
        // schedule headers rather than persisting anything of its own.
        busBuilder.Services.AddSingleton(
            new ScheduledMessageStoreRegistration(
                transport,
                NatsScheduledMessageStore.TokenPrefix,
                _ => new NatsScheduledMessageStore(transport)));

        return busBuilder;
    }

    /// <summary>
    /// Adds a NATS JetStream messaging transport to the message bus with default configuration.
    /// </summary>
    /// <param name="busBuilder">The message bus host builder to extend.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddNats(this IMessageBusHostBuilder busBuilder)
    {
        return busBuilder.AddNats(static _ => { });
    }
}
