using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the core outbox services of a context.
/// </summary>
public static class OutboxEntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers the core outbox infrastructure of the context: its outbox signal, and the EF Core interceptors
    /// that raise the signal when a save or a transaction in which the context wrote outbox messages commits.
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
        builder.Services.TryAddKeyedSingleton<IOutboxSignal, MessageBusOutboxSignal>(builder.ContextType);
        builder.HostBuilder.UseOutboxCore();

        builder.ConfigureEntityFrameworkServices(static services =>
        {
            services.AddScoped<DbContextOutboxState>();
            services.AddScoped<IResettableService>(static s => s.GetRequiredService<DbContextOutboxState>());
            services.AddSingleton<IInterceptor, OutboxDbTransactionInterceptor>();
            services.AddSingleton<IInterceptor, OutboxSaveChangesInterceptor>();
        });

        return builder;
    }
}
