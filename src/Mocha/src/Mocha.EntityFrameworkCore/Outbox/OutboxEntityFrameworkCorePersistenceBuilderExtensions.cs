using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// core outbox interceptors that signal the outbox processor after save and transaction commit.
/// </summary>
public static class OutboxEntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers the core outbox infrastructure, including EF Core interceptors that signal the
    /// outbox processor when changes are saved or transactions are committed.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseOutboxCore(this IEntityFrameworkCoreBuilder builder)
    {
        if (!builder.Services.AddOutboxDbContext(builder.ContextType))
        {
            return builder;
        }

        builder.HostBuilder.UseOutboxCore();

        builder.ConfigureEntityFrameworkServices((sp, services) =>
        {
            var signal = sp.GetRequiredService<IOutboxSignal>();
            services.AddSingleton<IInterceptor>(new OutboxDbTransactionInterceptor(signal));
            services.AddSingleton<IInterceptor>(new OutboxSaveChangesInterceptor(signal));
            services.AddScoped(static s => new DbContextOutbox(s.GetRequiredService<ICurrentDbContext>().Context));
            services.AddScoped<IResettableService>(static s => s.GetRequiredService<DbContextOutbox>());
        });
        return builder;
    }
}
