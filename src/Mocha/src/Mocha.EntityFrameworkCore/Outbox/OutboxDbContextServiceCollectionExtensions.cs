using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxDbContextServiceCollectionExtensions
{
    public static void AddOutboxDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddScoped<DbContextOutboxScope>();

        for (var i = services.Count - 1; i >= 0; i--)
        {
            var descriptor = services[i];

            if (!descriptor.IsKeyedService && descriptor.ServiceType == typeof(TContext))
            {
                if (descriptor.Lifetime is not ServiceLifetime.Singleton)
                {
                    AddContext<TContext>(services, i, descriptor);
                }

                break;
            }
        }
    }

    private static void AddContext<TContext>(
        IServiceCollection services,
        int index,
        ServiceDescriptor descriptor)
        where TContext : DbContext
    {
        var originalFactory = descriptor.ImplementationFactory;

        services[index] = ServiceDescriptor.Describe(
            typeof(TContext),
            provider =>
            {
                var scope = provider.GetRequiredService<DbContextOutboxScope>();
                var context = (TContext)(originalFactory is not null
                    ? originalFactory(provider)
                    : ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType!));

                try
                {
                    context.Outbox.BindScope(scope);
                    return context;
                }
                catch
                {
                    context.Dispose();
                    throw;
                }
            },
            descriptor.Lifetime);
    }
}
