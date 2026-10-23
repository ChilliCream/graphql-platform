using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods to register outbox infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class OutboxCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core outbox services and persists outgoing messages before transport dispatch.
    /// </summary>
    /// <remarks>
    /// Adds <see cref="IOutboxSignal"/> as a singleton and configures the dispatch pipeline
    /// to persist outgoing messages (Publish, Send, Reply, Fault) through <see cref="IMessageOutbox"/>
    /// instead of dispatching them directly.
    /// </remarks>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder UseOutboxCore(this IMessageBusHostBuilder builder)
    {
        if (builder.Services.Any(d => d.ServiceType == typeof(OutboxCoreRegistration)))
        {
            return builder;
        }

        builder.Services.AddSingleton<OutboxCoreRegistration>();
        builder.Services.TryAddSingleton<IOutboxSignal, MessageBusOutboxSignal>();
        builder.ConfigureMessageBus(b => b.ConfigureFeature(features =>
            features.Set(new DispatchFinalizationFeature(static (_, next) =>
            {
                var middleware = new DispatchOutboxMiddleware();
                return context => middleware.InvokeAsync(context, next);
            }))));

        return builder;
    }

    private sealed class OutboxCoreRegistration;
}
