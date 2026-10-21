using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxDbContextServiceCollectionExtensions
{
    public static bool AddOutboxDbContext(this IServiceCollection services, Type contextType)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(OutboxDbContextRegistration))
            {
                var registration = (OutboxDbContextRegistration)descriptor.ImplementationInstance!;

                if (registration.ContextType == contextType)
                {
                    return false;
                }
            }
        }

        services.AddSingleton(new OutboxDbContextRegistration(contextType));
        return true;
    }

    public static void AttachOutboxDbContext<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        var registration = services
            .Where(d => d.ServiceType == typeof(OutboxDbContextRegistration))
            .Select(d => (OutboxDbContextRegistration)d.ImplementationInstance!)
            .FirstOrDefault(r => r.ContextType == typeof(TContext));

        if (registration is null || registration.AttachmentRegistered)
        {
            return;
        }

        var descriptor = services.LastOrDefault(d => !d.IsKeyedService && d.ServiceType == typeof(TContext));
        if (descriptor?.Lifetime == ServiceLifetime.Singleton)
        {
            throw ThrowHelper.SingletonOutboxDbContext(typeof(TContext));
        }

        registration.AttachmentRegistered = true;
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
                    context.GetService<DbContextOutboxScope>().Attach(serviceProvider);
                    return context;
                }
                catch
                {
                    context.Dispose();
                    throw;
                }
            },
            descriptor?.Lifetime ?? ServiceLifetime.Scoped));
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

internal sealed class OutboxDbContextRegistration(Type contextType)
{
    public Type ContextType { get; } = contextType;

    public bool AttachmentRegistered { get; set; }
}

internal sealed record DefaultOutboxDbContextRegistration(Type ContextType);
