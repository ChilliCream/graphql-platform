using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Scheduling;

/// <summary>
/// Provides extension methods to register scheduling infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class SchedulerCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core scheduling services and inserts the scheduling dispatch middleware into
    /// the message bus pipeline.
    /// </summary>
    /// <remarks>
    /// Adds <see cref="ISchedulerSignal"/> as a singleton and configures the dispatch pipeline
    /// to persist outgoing messages with a <see cref="Middlewares.IDispatchContext.ScheduledTime"/>
    /// through <see cref="IScheduledMessageStore"/> instead of dispatching them directly.
    /// </remarks>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder UseSchedulerCore(this IMessageBusHostBuilder builder)
    {
        builder.Services.TryAddSingleton<ISchedulerSignal>(sp =>
            new MessageBusSchedulerSignal(sp.GetService<TimeProvider>() ?? TimeProvider.System));

        builder.ConfigureMessageBus(x => x.UseDispatch(DispatchSchedulingMiddleware.Create(), after: "Serialization"));

        return builder;
    }
}
