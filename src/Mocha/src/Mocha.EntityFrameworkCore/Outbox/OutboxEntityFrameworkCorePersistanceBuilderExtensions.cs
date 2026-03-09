using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// core outbox interceptors that signal the outbox processor after save and transaction commit.
/// </summary>
public static class OutboxEntityFrameworkCorePersistanceBuilderExtensions
{
    /// <summary>
    /// Registers the core outbox infrastructure, including EF Core interceptors that signal the
    /// outbox processor when changes are saved or transactions are committed.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder AddOutboxCore(this IEntityFrameworkCoreBuilder builder)
    {
        builder.HostBuilder.AddOutboxCore();

        builder.ConfigureEntityFrameworkServices(
            (sp, services) =>
            {
                var signal = sp.GetService<IOutboxSignal>();

                if (signal is not null)
                {
                    services.AddSingleton<IInterceptor>(new OutboxDbTransactionInterceptor(signal));
                    services.AddSingleton<IInterceptor>(new OutboxSaveChangesInterceptor(signal));
                }
            });
        return builder;
    }
}
