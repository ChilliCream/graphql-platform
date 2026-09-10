using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Scheduling;

/// <summary>
/// Provides extension methods to register scheduling infrastructure on <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class SchedulerCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core scheduling services. The scheduling dispatch middleware is part of the
    /// default dispatch pipeline and does not need to be installed by this method.
    /// </summary>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder UseSchedulerCore(this IMessageBusHostBuilder builder)
    {
        builder.Services.TryAddSingleton<ISchedulerSignal>(sp =>
            new MessageBusSchedulerSignal(sp.GetService<TimeProvider>() ?? TimeProvider.System));

        return builder;
    }
}
