using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxDbContextServiceCollectionExtensions
{
    public static bool AddOutboxDbContext<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(OutboxDbContextRegistration)
                && service.ImplementationInstance is OutboxDbContextRegistration registration
                && registration.ContextType == typeof(TContext))
            {
                return false;
            }
        }

        var descriptor = services.LastOrDefault(d => !d.IsKeyedService && d.ServiceType == typeof(TContext));
        if (descriptor?.Lifetime == ServiceLifetime.Singleton)
        {
            throw ThrowHelper.SingletonOutboxDbContext(typeof(TContext));
        }

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.Add(new ServiceDescriptor(
            typeof(TContext),
            serviceProvider =>
            {
                var context = descriptor?.ImplementationFactory is { } implementationFactory
                    ? (TContext)implementationFactory(serviceProvider)
                    : descriptor?.ImplementationType is { } implementationType
                        ? (TContext)ActivatorUtilities.CreateInstance(serviceProvider, implementationType)
                        : CreateContext<TContext>(serviceProvider);

                try
                {
                    context.GetService<DbContextOutbox>().Attach(serviceProvider);
                    return context;
                }
                catch
                {
                    context.Dispose();
                    throw;
                }
            },
            descriptor?.Lifetime ?? ServiceLifetime.Scoped));

        services.AddSingleton(new OutboxDbContextRegistration(typeof(TContext)));
        return true;
    }

    private static TContext CreateContext<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
        IServiceProvider services)
        where TContext : DbContext
    {
#pragma warning disable EF1001
        // EF owns pooled leases and releases them with the application scope.
        if (services.GetService<IScopedDbContextLease<TContext>>() is { } lease)
        {
            return lease.Context;
        }
#pragma warning restore EF1001

        if (services.GetService<IDbContextFactory<TContext>>() is { } contextFactory)
        {
            return contextFactory.CreateDbContext();
        }

        if (services.GetService<DbContextOptions<TContext>>() is null)
        {
            throw ThrowHelper.MissingMessagingDbContext(typeof(TContext));
        }

        return ActivatorUtilities.CreateInstance<TContext>(services);
    }
}
