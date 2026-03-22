using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// additional services into the DbContext internal service provider.
/// </summary>
public static class EntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers a callback that configures services within the DbContext internal service provider,
    /// allowing interceptors and other EF Core services to be injected alongside messaging infrastructure.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <param name="configure">
    /// A delegate receiving the application <see cref="IServiceProvider"/> and the DbContext
    /// <see cref="IServiceCollection"/> for registering additional services.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder ConfigureEntityFrameworkServices(
        this IEntityFrameworkCoreBuilder builder,
        Action<IServiceProvider, IServiceCollection> configure)
    {
        builder.Services.Configure<MessagingDbContextOptions>(
            builder.Name,
            (options) => options.ConfigureServices.Add(configure));
        return builder;
    }
}
