using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class EntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers services in the DbContext internal service provider. That provider is shared across
    /// applications, so the delegate must not capture application services.
    /// </summary>
    public static IEntityFrameworkCoreBuilder ConfigureEntityFrameworkServices(
        this IEntityFrameworkCoreBuilder builder,
        Action<IServiceCollection> configure)
    {
        builder.Services.Configure<MessagingDbContextOptions>(
            builder.Name,
            options => options.ConfigureServices.Add(configure));

        return builder;
    }
}
