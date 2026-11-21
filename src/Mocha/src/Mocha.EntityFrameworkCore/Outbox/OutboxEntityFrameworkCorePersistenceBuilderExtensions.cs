using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the core outbox services of a context.
/// </summary>
public static class OutboxEntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers the core outbox infrastructure of the context, including the EF Core interceptor that signals
    /// the outbox processor when a transaction in which the context wrote outbox messages commits.
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

        builder.ConfigureEntityFrameworkServices(static services =>
        {
            services.AddScoped<DbContextOutboxState>();
            services.AddSingleton<IInterceptor, OutboxDbTransactionInterceptor>();
        });

        return builder;
    }
}
