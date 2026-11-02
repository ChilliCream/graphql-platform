using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// core scheduling interceptors that signal the scheduler after save and transaction commit.
/// </summary>
public static class SchedulingEntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers the core scheduling infrastructure, including EF Core interceptors that signal the
    /// scheduler when changes are saved or transactions are committed.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseSchedulingCore(this IEntityFrameworkCoreBuilder builder)
    {
        builder.HostBuilder.UseSchedulerCore();

        builder.ConfigureEntityFrameworkServices((sp, services) =>
        {
            var signal = sp.GetService<ISchedulerSignal>();

            if (signal is not null)
            {
                var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
                services.AddSingleton<IInterceptor>(new SchedulingDbTransactionInterceptor(signal, timeProvider));
                services.AddSingleton<IInterceptor>(new SchedulingSaveChangesInterceptor(signal, timeProvider));
            }
        });
        return builder;
    }
}
