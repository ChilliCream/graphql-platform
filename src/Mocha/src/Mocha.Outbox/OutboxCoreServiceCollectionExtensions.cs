using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods to register outbox infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class OutboxCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core outbox services and dispatch middleware.
    /// </summary>
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
        builder.ConfigureMessageBus(b => b.UseDispatch(
            DispatchOutboxMiddleware.Create(), before: DispatchMiddlewares.Serialization.Key));

        return builder;
    }

    private sealed class OutboxCoreRegistration;
}
