using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Outbox;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IMessageBusHostBuilder"/> for integrating Entity Framework Core
/// persistence with the message bus infrastructure.
/// </summary>
public static class MessageBusHostBuilderExtensions
{
    /// <summary>
    /// Registers an Entity Framework Core <see cref="DbContext"/> with the message bus host and
    /// configures persistence features such as outbox and saga storage.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type used for messaging persistence.</typeparam>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <param name="configure">A delegate that configures Entity Framework Core features on the builder.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder AddEntityFramework<TContext>(
        this IMessageBusHostBuilder builder,
        Action<IEntityFrameworkCoreBuilder> configure)
        where TContext : DbContext
    {
        var efBuilder = new EntityFrameworkCoreBuilder
        {
            Services = builder.Services,
            HostBuilder = builder,
            ContextType = typeof(TContext),
            Name = typeof(TContext).FullName ?? typeof(TContext).Name
        };

        configure(efBuilder);

        builder.Services.AddOptions<MessagingDbContextOptions>(efBuilder.Name);

        builder.Services.TryAddSingleton(static services => new MessagingDbContextServices(
            services.GetService<IOutboxSignal>(),
            services.GetService<ISchedulerSignal>(),
            services.GetTimeProvider()));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IDbContextOptionsConfiguration<TContext>,
            MessagingDbContextOptionsConfiguration<TContext>
        >());

        return builder;
    }
}
