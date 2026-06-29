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
    /// Calling this method more than once is safe: the dispatch middleware is installed at most once.
    /// </remarks>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder UseSchedulerCore(this IMessageBusHostBuilder builder)
    {
        builder.Services.TryAddSingleton<ISchedulerSignal>(sp =>
            new MessageBusSchedulerSignal(sp.GetService<TimeProvider>() ?? TimeProvider.System));

        if (builder.Services.Any(d => d.ServiceType == typeof(SchedulerCoreMarker)))
        {
            return builder;
        }

        builder.Services.AddSingleton<SchedulerCoreMarker>();
        builder.ConfigureMessageBus(x => x.UseDispatch(DispatchSchedulingMiddleware.Create(), after: "Serialization"));

        return builder;
    }
}

/// <summary>
/// Marker service used to detect whether <see cref="SchedulerCoreServiceCollectionExtensions.UseSchedulerCore"/>
/// has already been called, preventing the scheduling dispatch middleware from being installed twice.
/// </summary>
internal sealed class SchedulerCoreMarker;
