using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public static IMessageBusHostBuilder AddEntityFramework<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
        this IMessageBusHostBuilder builder,
        Action<IEntityFrameworkCoreBuilder> configure)
        where TContext : DbContext
    {
        var efBuilder = new EntityFrameworkCoreBuilder<TContext>(builder);

        configure(efBuilder);

        builder
            .Services.AddOptions<MessagingDbContextOptions>(efBuilder.Name)
            .Configure<IServiceProvider>((options, serviceProvider) => options.ServiceProvider = serviceProvider);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IDbContextOptionsConfiguration<TContext>,
            MessagingDbContextOptionsConfiguration<TContext>
        >());

        return builder;
    }
}
