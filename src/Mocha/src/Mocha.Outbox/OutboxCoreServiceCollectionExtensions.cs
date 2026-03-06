using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods to register outbox infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class OutboxCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core outbox services and inserts the outbox dispatch middleware into the message bus pipeline.
    /// </summary>
    /// <remarks>
    /// Adds <see cref="IOutboxSignal"/> as a singleton and configures the dispatch pipeline
    /// to persist outgoing messages (Publish, Send, Reply, Fault) through <see cref="IMessageOutbox"/>
    /// instead of dispatching them directly.
    /// </remarks>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder AddOutboxCore(this IMessageBusHostBuilder builder)
    {
        builder.Services.TryAddSingleton<IOutboxSignal, MessageBusOutboxSignal>();
        builder.ConfigureMessageBus(x => x.UseDispatch(DispatchOutboxMiddleware.Create()));

        return builder;
    }
}
