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
        if (builder.Services.Any(descriptor =>
            descriptor.ServiceType == typeof(OutboxDbContextRegistration)
            && descriptor.ImplementationInstance is OutboxDbContextRegistration registration
            && registration.ContextType == builder.ContextType))
        {
            return builder;
        }

        builder.Services.AddSingleton(new OutboxDbContextRegistration(builder.ContextType));
        builder.HostBuilder.UseOutboxCore();

        builder.ConfigureEntityFrameworkServices((sp, services) =>
        {
            var signal = sp.GetRequiredService<IOutboxSignal>();
            services.AddSingleton<IInterceptor>(new OutboxDbTransactionInterceptor(signal));
            services.AddSingleton<IInterceptor>(new OutboxSaveChangesInterceptor(signal));
            services.AddScoped<DbContextOutboxState>();
            services.AddScoped<IResettableService>(static s => s.GetRequiredService<DbContextOutboxState>());
        });

        return builder;
    }
}
